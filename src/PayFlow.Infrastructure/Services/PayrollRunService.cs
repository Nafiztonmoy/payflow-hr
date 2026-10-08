using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Application.Payroll.Interfaces;
using PayFlow.Application.Payroll.Models;
using PayFlow.Domain.Entities;
using PayFlow.Domain.Enums;
using PayFlow.Domain.Exceptions;
using PayFlow.Domain.Models;
using PayFlow.Infrastructure.Data;

namespace PayFlow.Infrastructure.Services;

public class PayrollRunService : IPayrollRunService
{
    private readonly PayFlowDbContext _context;
    private readonly IPayrollCalculator _payrollCalculator;
    private readonly ICompensationResolver _compensationResolver;
    private readonly IAttendanceSummaryService _attendanceSummaryService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public PayrollRunService(
        PayFlowDbContext context,
        IPayrollCalculator payrollCalculator,
        ICompensationResolver compensationResolver,
        IAttendanceSummaryService attendanceSummaryService,
        ICurrentUserService currentUserService,
        IAuditService auditService)
    {
        _context = context;
        _payrollCalculator = payrollCalculator;
        _compensationResolver = compensationResolver;
        _attendanceSummaryService = attendanceSummaryService;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async Task<PayrollRunSummaryDto> CreateRunAsync(CreatePayrollRunCommand command, CancellationToken ct = default)
    {
        if (command.Month < 1 || command.Month > 12)
            throw new PayrollValidationException("Invalid month specified.");

        bool exists = await _context.PayrollRuns
            .AnyAsync(r => r.PeriodYear == command.Year && r.PeriodMonth == command.Month, ct);

        if (exists)
            throw new PayrollValidationException($"A payroll run for {command.Year}-{command.Month:D2} already exists.");

        var startDate = new DateOnly(command.Year, command.Month, 1);
        var endDate = new DateOnly(command.Year, command.Month, DateTime.DaysInMonth(command.Year, command.Month));

        var run = new PayrollRun
        {
            PeriodYear = command.Year,
            PeriodMonth = command.Month,
            PeriodStart = startDate,
            PeriodEnd = endDate,
            RunNumber = $"PR-{command.Year}-{command.Month:D2}",
            Status = PayrollRunStatus.Draft,
            CreatedByUserId = _currentUserService.UserId,
            CreatedAtUtc = DateTime.UtcNow,
            ConcurrencyToken = Guid.NewGuid()
        };

        _context.PayrollRuns.Add(run);
        await _context.SaveChangesAsync(ct);

        await _auditService.LogAsync(
            "CREATE_PAYROLL_RUN",
            nameof(PayrollRun),
            run.Id.ToString(),
            $"Created draft payroll run {run.RunNumber} for {command.Year}-{command.Month:D2}.",
            null,
            new { run.Id, run.RunNumber, run.Status },
            ct
        );

        return MapToSummary(run, 0, 0);
    }

    public async Task<PayrollRunSummaryDto> CalculateRunAsync(Guid runId, CancellationToken ct = default)
    {
        var run = await _context.PayrollRuns
            .Include(r => r.Items)
            .Include(r => r.Exceptions)
            .FirstOrDefaultAsync(r => r.Id == runId, ct)
            ?? throw new EntityNotFoundException(nameof(PayrollRun), runId);

        if (run.Status == PayrollRunStatus.Approved || run.Status == PayrollRunStatus.Paid)
        {
            throw new PayrollValidationException($"Cannot recalculate payroll run in '{run.Status}' status. Approved and Paid payroll runs are immutable.");
        }

        run.Status = PayrollRunStatus.Calculating;
        await _context.SaveChangesAsync(ct);

        // Transaction boundary for idempotent calculation (only on relational databases)
        var isRelational = _context.Database.IsRelational();
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = isRelational
            ? await _context.Database.BeginTransactionAsync(ct)
            : null;

        try
        {
            // Clear existing unapproved items, components, and exceptions
            var existingItems = await _context.PayrollItems
                .Where(i => i.PayrollRunId == runId)
                .ToListAsync(ct);
            _context.PayrollItems.RemoveRange(existingItems);

            var existingExceptions = await _context.PayrollExceptions
                .Where(e => e.PayrollRunId == runId)
                .ToListAsync(ct);
            _context.PayrollExceptions.RemoveRange(existingExceptions);

            await _context.SaveChangesAsync(ct);

            // Fetch active tax ruleset
            var taxRuleSet = await _context.TaxRuleSets
                .Where(t => t.IsActive)
                .OrderByDescending(t => t.EffectiveFrom)
                .FirstOrDefaultAsync(ct);

            var taxConfig = !string.IsNullOrEmpty(taxRuleSet?.RulesJson)
                ? JsonSerializer.Deserialize<TaxRuleConfig>(taxRuleSet.RulesJson) ?? GetDefaultTaxConfig()
                : GetDefaultTaxConfig();

            // Calculate working days in month
            int workingDays = CalculateWorkingDays(run.PeriodStart, run.PeriodEnd);
            var period = new PayrollPeriod(run.PeriodYear, run.PeriodMonth, run.PeriodStart, run.PeriodEnd, workingDays);

            // Fetch employees
            var employees = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .Include(e => e.Compensations)
                .Include(e => e.AttendanceRecords.Where(a => a.Date >= run.PeriodStart && a.Date <= run.PeriodEnd))
                .Include(e => e.LeaveRequests.Where(l => l.StartDate <= run.PeriodEnd && l.EndDate >= run.PeriodStart))
                    .ThenInclude(l => l.LeaveType)
                .Where(e => e.Status == EmploymentStatus.Active || (e.TerminationDate.HasValue && e.TerminationDate.Value >= run.PeriodStart.ToDateTime(TimeOnly.MinValue)))
                .ToListAsync(ct);

            decimal totalGross = 0m;
            decimal totalTax = 0m;
            decimal totalDeductions = 0m;
            decimal totalNet = 0m;

            var newItems = new List<PayrollItem>();
            var newExceptions = new List<PayrollException>();

            foreach (var employee in employees)
            {
                var effectiveCompensation = _compensationResolver.ResolveEffectiveCompensation(employee, period);
                
                SalaryStructure? salaryStructure = null;
                List<PayComponent>? components = null;

                if (effectiveCompensation != null)
                {
                    salaryStructure = await _context.SalaryStructures
                        .Include(s => s.Components)
                        .FirstOrDefaultAsync(s => s.Id == effectiveCompensation.SalaryStructureId, ct);
                    components = salaryStructure?.Components.ToList();
                }

                var attendanceSummary = _attendanceSummaryService.CalculateSummary(
                    employee,
                    employee.AttendanceRecords,
                    employee.LeaveRequests,
                    period
                );

                var context = new EmployeePayrollContext(
                    Employee: employee,
                    ActiveCompensation: effectiveCompensation,
                    SalaryStructure: salaryStructure,
                    Components: components ?? new List<PayComponent>(),
                    AttendanceSummary: attendanceSummary,
                    TaxConfig: taxConfig,
                    Period: period
                );

                var calcResult = _payrollCalculator.CalculateEmployeePayroll(context);

                var payrollItem = new PayrollItem
                {
                    Id = Guid.NewGuid(),
                    PayrollRunId = run.Id,
                    EmployeeId = employee.Id,
                    BaseSalary = calcResult.BaseSalary,
                    GrossPay = calcResult.GrossPay,
                    TaxableAmount = calcResult.TaxableAmount,
                    IncomeTax = calcResult.IncomeTax,
                    TotalDeductions = calcResult.TotalDeductions,
                    NetPay = calcResult.NetPay,
                    Status = calcResult.Status,
                    CalculationSnapshotJson = calcResult.SnapshotJson,
                    CalculationHash = calcResult.CalculationHash,
                    CreatedAtUtc = DateTime.UtcNow
                };

                foreach (var c in calcResult.Components)
                {
                    payrollItem.Components.Add(new PayrollItemComponent
                    {
                        Id = Guid.NewGuid(),
                        PayrollItemId = payrollItem.Id,
                        ComponentCode = c.Code,
                        ComponentName = c.DisplayName,
                        ComponentType = c.Type,
                        Amount = c.Amount,
                        SourceRule = c.SourceRule,
                        Explanation = c.Explanation
                    });
                }

                foreach (var ex in calcResult.Exceptions)
                {
                    var exceptionEntity = new PayrollException
                    {
                        Id = Guid.NewGuid(),
                        PayrollRunId = run.Id,
                        PayrollItemId = payrollItem.Id,
                        EmployeeId = employee.Id,
                        Severity = ex.Severity,
                        ExceptionType = ex.ExceptionType,
                        Message = ex.Message,
                        IsResolved = false
                    };
                    newExceptions.Add(exceptionEntity);
                }

                newItems.Add(payrollItem);

                totalGross += calcResult.GrossPay;
                totalTax += calcResult.IncomeTax;
                totalDeductions += calcResult.TotalDeductions;
                totalNet += calcResult.NetPay;
            }

            _context.PayrollItems.AddRange(newItems);
            _context.PayrollExceptions.AddRange(newExceptions);

            run.TotalGross = RoundingPolicy.RoundCurrency(totalGross);
            run.TotalTax = RoundingPolicy.RoundCurrency(totalTax);
            run.TotalDeductions = RoundingPolicy.RoundCurrency(totalDeductions);
            run.TotalNet = RoundingPolicy.RoundCurrency(totalNet);
            run.TotalEmployees = newItems.Count;
            run.CalculatedAtUtc = DateTime.UtcNow;
            run.Status = PayrollRunStatus.Calculated;
            run.ConcurrencyToken = Guid.NewGuid();

            await _context.SaveChangesAsync(ct);
            if (transaction != null)
            {
                await transaction.CommitAsync(ct);
            }

            await _auditService.LogAsync(
                "CALCULATE_PAYROLL_RUN",
                nameof(PayrollRun),
                run.Id.ToString(),
                $"Calculated payroll run {run.RunNumber}: {newItems.Count} employees, Gross ${run.TotalGross:N2}, Net ${run.TotalNet:N2}, {newExceptions.Count} exceptions.",
                null,
                new { run.Id, run.TotalGross, run.TotalNet, EmployeeCount = newItems.Count, ExceptionCount = newExceptions.Count },
                ct
            );

            int warnings = newExceptions.Count(e => e.Severity == ExceptionSeverity.Warning);
            int blockings = newExceptions.Count(e => e.Severity == ExceptionSeverity.Blocking);

            return MapToSummary(run, warnings, blockings);
        }
        catch (Exception)
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(ct);
            }
            run.Status = PayrollRunStatus.Failed;
            await _context.SaveChangesAsync(ct);
            throw;
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    public async Task<PayrollRunSummaryDto> SubmitForReviewAsync(SubmitForReviewCommand command, CancellationToken ct = default)
    {
        var run = await _context.PayrollRuns
            .FirstOrDefaultAsync(r => r.Id == command.RunId, ct)
            ?? throw new EntityNotFoundException(nameof(PayrollRun), command.RunId);

        if (run.ConcurrencyToken != command.ConcurrencyToken)
            throw new ConcurrencyConflictException("The payroll run has been modified by another user. Please refresh and try again.");

        if (run.Status != PayrollRunStatus.Calculated)
            throw new PayrollValidationException($"Only runs in 'Calculated' status can be submitted for review. Current status: '{run.Status}'.");

        run.Status = PayrollRunStatus.InReview;
        run.ReviewedAtUtc = DateTime.UtcNow;
        run.ReviewedByUserId = _currentUserService.UserId;
        run.ConcurrencyToken = Guid.NewGuid();

        await _context.SaveChangesAsync(ct);

        await _auditService.LogAsync(
            "SUBMIT_PAYROLL_REVIEW",
            nameof(PayrollRun),
            run.Id.ToString(),
            $"Submitted payroll run {run.RunNumber} for review.",
            new { PreviousStatus = PayrollRunStatus.Calculated },
            new { Status = run.Status },
            ct
        );

        var (w, b) = await GetExceptionCountsAsync(run.Id, ct);
        return MapToSummary(run, w, b);
    }

    public async Task<PayrollRunSummaryDto> ApproveRunAsync(ApprovePayrollRunCommand command, CancellationToken ct = default)
    {
        var run = await _context.PayrollRuns
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == command.RunId, ct)
            ?? throw new EntityNotFoundException(nameof(PayrollRun), command.RunId);

        if (run.ConcurrencyToken != command.ConcurrencyToken)
            throw new ConcurrencyConflictException("The payroll run has been modified by another user. Please refresh and try again.");

        if (run.Status != PayrollRunStatus.InReview && run.Status != PayrollRunStatus.Calculated)
            throw new PayrollValidationException($"Run must be in 'InReview' or 'Calculated' status to approve. Current status: '{run.Status}'.");

        // Verify zero blocking exceptions
        bool hasBlocking = await _context.PayrollExceptions
            .AnyAsync(e => e.PayrollRunId == command.RunId && !e.IsResolved && e.Severity == ExceptionSeverity.Blocking, ct);

        if (hasBlocking)
        {
            throw new PayrollValidationException("Cannot approve payroll run with unresolved blocking exceptions. Resolve all blocking exceptions first.");
        }

        run.Status = PayrollRunStatus.Approved;
        run.ApprovedAtUtc = DateTime.UtcNow;
        run.ApprovedByUserId = _currentUserService.UserId;
        run.ConcurrencyToken = Guid.NewGuid();

        // Generate payslips for all items
        foreach (var item in run.Items)
        {
            var existingPayslip = await _context.Payslips.FirstOrDefaultAsync(p => p.PayrollItemId == item.Id, ct);
            if (existingPayslip == null)
            {
                _context.Payslips.Add(new Payslip
                {
                    Id = Guid.NewGuid(),
                    PayrollItemId = item.Id,
                    EmployeeId = item.EmployeeId,
                    PayslipNumber = $"PS-{run.PeriodYear}-{run.PeriodMonth:D2}-{item.Id.ToString()[..8].ToUpper()}",
                    IssueDateUtc = DateTime.UtcNow,
                    SnapshotJson = item.CalculationSnapshotJson
                });
            }
        }

        await _context.SaveChangesAsync(ct);

        await _auditService.LogAsync(
            "APPROVE_PAYROLL_RUN",
            nameof(PayrollRun),
            run.Id.ToString(),
            $"Approved payroll run {run.RunNumber}. Generated payslips for {run.Items.Count} employees.",
            null,
            new { run.Id, run.Status, ApprovedAt = run.ApprovedAtUtc },
            ct
        );

        var (w, b) = await GetExceptionCountsAsync(run.Id, ct);
        return MapToSummary(run, w, b);
    }

    public async Task<PayrollRunSummaryDto> MarkPaidAsync(MarkPaidCommand command, CancellationToken ct = default)
    {
        var run = await _context.PayrollRuns
            .FirstOrDefaultAsync(r => r.Id == command.RunId, ct)
            ?? throw new EntityNotFoundException(nameof(PayrollRun), command.RunId);

        if (run.ConcurrencyToken != command.ConcurrencyToken)
            throw new ConcurrencyConflictException("The payroll run has been modified by another user. Please refresh and try again.");

        if (run.Status != PayrollRunStatus.Approved)
            throw new PayrollValidationException($"Only Approved payroll runs can be marked as Paid. Current status: '{run.Status}'.");

        if (string.IsNullOrWhiteSpace(command.PaymentReference))
            throw new PayrollValidationException("Payment reference is required to mark payroll as paid.");

        run.Status = PayrollRunStatus.Paid;
        run.PaidAtUtc = command.PaidAtUtc;
        run.PaymentReference = command.PaymentReference;
        run.PaidByUserId = _currentUserService.UserId;
        run.ConcurrencyToken = Guid.NewGuid();

        await _context.SaveChangesAsync(ct);

        await _auditService.LogAsync(
            "MARK_PAYROLL_PAID",
            nameof(PayrollRun),
            run.Id.ToString(),
            $"Marked payroll run {run.RunNumber} as paid. Payment Reference: {run.PaymentReference}.",
            null,
            new { run.Id, run.Status, run.PaymentReference, run.PaidAtUtc },
            ct
        );

        var (w, b) = await GetExceptionCountsAsync(run.Id, ct);
        return MapToSummary(run, w, b);
    }

    public async Task<PayrollExceptionDto> ResolveExceptionAsync(ResolveExceptionCommand command, CancellationToken ct = default)
    {
        var ex = await _context.PayrollExceptions
            .Include(e => e.Employee)
            .FirstOrDefaultAsync(e => e.Id == command.ExceptionId, ct)
            ?? throw new EntityNotFoundException(nameof(PayrollException), command.ExceptionId);

        ex.IsResolved = true;
        ex.ResolvedAtUtc = DateTime.UtcNow;
        ex.ResolvedByUserId = _currentUserService.UserId;
        ex.ResolutionNotes = command.ResolutionNotes;

        // If payroll item had blocking exception, re-evaluate item status
        if (ex.PayrollItemId.HasValue)
        {
            var otherExceptions = await _context.PayrollExceptions
                .Where(e => e.PayrollItemId == ex.PayrollItemId.Value && e.Id != ex.Id && !e.IsResolved)
                .ToListAsync(ct);

            var item = await _context.PayrollItems.FindAsync(new object[] { ex.PayrollItemId.Value }, ct);
            if (item != null)
            {
                if (otherExceptions.Any(e => e.Severity == ExceptionSeverity.Blocking))
                {
                    item.Status = PayrollItemStatus.HasBlockingExceptions;
                }
                else if (otherExceptions.Any(e => e.Severity == ExceptionSeverity.Warning))
                {
                    item.Status = PayrollItemStatus.HasWarnings;
                }
                else
                {
                    item.Status = PayrollItemStatus.Ok;
                }
            }
        }

        await _context.SaveChangesAsync(ct);

        await _auditService.LogAsync(
            "RESOLVE_PAYROLL_EXCEPTION",
            nameof(PayrollException),
            ex.Id.ToString(),
            $"Resolved exception '{ex.ExceptionType}' for employee {ex.Employee?.FullName ?? ex.EmployeeId.ToString()}: {command.ResolutionNotes}",
            null,
            new { ex.Id, ex.IsResolved, ex.ResolutionNotes },
            ct
        );

        return new PayrollExceptionDto(
            ex.Id,
            ex.PayrollItemId,
            ex.EmployeeId,
            ex.Employee?.FullName ?? "Unknown",
            ex.Severity,
            ex.ExceptionType,
            ex.Message,
            ex.IsResolved,
            ex.ResolvedAtUtc,
            ex.ResolutionNotes
        );
    }

    public async Task<PayrollRunSummaryDto> GetRunByIdAsync(Guid runId, CancellationToken ct = default)
    {
        var run = await _context.PayrollRuns
            .FirstOrDefaultAsync(r => r.Id == runId, ct)
            ?? throw new EntityNotFoundException(nameof(PayrollRun), runId);

        var (w, b) = await GetExceptionCountsAsync(run.Id, ct);
        return MapToSummary(run, w, b);
    }

    public async Task<List<PayrollRunSummaryDto>> GetRunsAsync(CancellationToken ct = default)
    {
        var runs = await _context.PayrollRuns
            .OrderByDescending(r => r.PeriodYear)
            .ThenByDescending(r => r.PeriodMonth)
            .ToListAsync(ct);

        var results = new List<PayrollRunSummaryDto>();
        foreach (var run in runs)
        {
            var (w, b) = await GetExceptionCountsAsync(run.Id, ct);
            results.Add(MapToSummary(run, w, b));
        }

        return results;
    }

    public async Task<List<PayrollItemDetailDto>> GetRunItemsAsync(Guid runId, CancellationToken ct = default)
    {
        var items = await _context.PayrollItems
            .Include(i => i.Employee)
                .ThenInclude(e => e.Department)
            .Include(i => i.Employee)
                .ThenInclude(e => e.Designation)
            .Include(i => i.Components)
            .Include(i => i.Exceptions)
            .Where(i => i.PayrollRunId == runId)
            .OrderBy(i => i.Employee!.EmployeeNo)
            .ToListAsync(ct);

        return items.Select(i => new PayrollItemDetailDto(
            i.Id,
            i.EmployeeId,
            i.Employee?.EmployeeNo ?? string.Empty,
            i.Employee?.FullName ?? string.Empty,
            i.Employee?.Department?.Name ?? "Unassigned",
            i.Employee?.Designation?.Title ?? "Unassigned",
            i.BaseSalary,
            i.GrossPay,
            i.TaxableAmount,
            i.IncomeTax,
            i.TotalDeductions,
            i.NetPay,
            i.Status,
            i.CalculationHash,
            i.Components.Select(c => new PayrollItemComponentDto(
                c.ComponentCode,
                c.ComponentName,
                c.ComponentType,
                c.Amount,
                c.SourceRule,
                c.Explanation
            )).ToList(),
            i.Exceptions.Select(e => new PayrollExceptionDto(
                e.Id,
                e.PayrollItemId,
                e.EmployeeId,
                i.Employee?.FullName ?? string.Empty,
                e.Severity,
                e.ExceptionType,
                e.Message,
                e.IsResolved,
                e.ResolvedAtUtc,
                e.ResolutionNotes
            )).ToList()
        )).ToList();
    }

    public async Task<List<PayrollExceptionDto>> GetRunExceptionsAsync(Guid runId, CancellationToken ct = default)
    {
        var exceptions = await _context.PayrollExceptions
            .Include(e => e.Employee)
            .Where(e => e.PayrollRunId == runId)
            .OrderBy(e => e.Severity)
            .ThenBy(e => e.IsResolved)
            .ToListAsync(ct);

        return exceptions.Select(e => new PayrollExceptionDto(
            e.Id,
            e.PayrollItemId,
            e.EmployeeId,
            e.Employee?.FullName ?? "Unknown",
            e.Severity,
            e.ExceptionType,
            e.Message,
            e.IsResolved,
            e.ResolvedAtUtc,
            e.ResolutionNotes
        )).ToList();
    }

    public async Task<byte[]> ExportPayrollCsvAsync(Guid runId, CancellationToken ct = default)
    {
        var items = await GetRunItemsAsync(runId, ct);
        var sb = new StringBuilder();
        sb.AppendLine("Employee No,Employee Name,Department,Designation,Base Salary,Gross Pay,Taxable Amount,Income Tax,Total Deductions,Net Pay,Status");

        foreach (var i in items)
        {
            sb.AppendLine($"\"{i.EmployeeNo}\",\"{i.EmployeeName}\",\"{i.DepartmentName}\",\"{i.DesignationTitle}\",{i.BaseSalary:F2},{i.GrossPay:F2},{i.TaxableAmount:F2},{i.IncomeTax:F2},{i.TotalDeductions:F2},{i.NetPay:F2},\"{i.Status}\"");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<PayslipDto> GetPayslipByItemIdAsync(Guid payrollItemId, CancellationToken ct = default)
    {
        var item = await _context.PayrollItems
            .Include(i => i.PayrollRun)
            .Include(i => i.Employee)
                .ThenInclude(e => e.Department)
            .Include(i => i.Employee)
                .ThenInclude(e => e.Designation)
            .Include(i => i.Components)
            .Include(i => i.Payslip)
            .FirstOrDefaultAsync(i => i.Id == payrollItemId, ct)
            ?? throw new EntityNotFoundException(nameof(PayrollItem), payrollItemId);

        var org = await _context.Organizations.FirstOrDefaultAsync(ct) ?? new Organization();

        string maskedBank = !string.IsNullOrEmpty(item.Employee?.BankAccountNumber)
            ? $"****{item.Employee.BankAccountNumber[^Math.Min(4, item.Employee.BankAccountNumber.Length)..]}"
            : "N/A";

        var earnings = item.Components
            .Where(c => c.ComponentType == PayComponentType.Earning)
            .Select(c => new PayrollItemComponentDto(c.ComponentCode, c.ComponentName, c.ComponentType, c.Amount, c.SourceRule, c.Explanation))
            .ToList();

        var deductions = item.Components
            .Where(c => c.ComponentType == PayComponentType.Deduction)
            .Select(c => new PayrollItemComponentDto(c.ComponentCode, c.ComponentName, c.ComponentType, c.Amount, c.SourceRule, c.Explanation))
            .ToList();

        return new PayslipDto(
            item.Payslip?.Id ?? Guid.Empty,
            item.Id,
            item.Payslip?.PayslipNumber ?? $"PS-{item.PayrollRun?.PeriodYear}-{item.PayrollRun?.PeriodMonth:D2}",
            org.Name,
            org.Currency,
            item.PayrollRun?.PeriodYear ?? DateTime.UtcNow.Year,
            item.PayrollRun?.PeriodMonth ?? DateTime.UtcNow.Month,
            item.Employee?.EmployeeNo ?? string.Empty,
            item.Employee?.FullName ?? string.Empty,
            item.Employee?.Department?.Name ?? "Unassigned",
            item.Employee?.Designation?.Title ?? "Unassigned",
            item.Employee?.PaymentMethod.ToString() ?? "BankTransfer",
            maskedBank,
            item.BaseSalary,
            item.GrossPay,
            item.TaxableAmount,
            item.IncomeTax,
            item.TotalDeductions,
            item.NetPay,
            earnings,
            deductions,
            item.Payslip?.IssueDateUtc ?? DateTime.UtcNow,
            item.CalculationHash
        );
    }

    private static PayrollRunSummaryDto MapToSummary(PayrollRun r, int warnings, int blockings)
    {
        return new PayrollRunSummaryDto(
            r.Id,
            r.PeriodYear,
            r.PeriodMonth,
            r.RunNumber,
            r.Status,
            r.TotalGross,
            r.TotalTax,
            r.TotalDeductions,
            r.TotalNet,
            r.TotalEmployees,
            warnings,
            blockings,
            r.CalculatedAtUtc,
            r.ReviewedAtUtc,
            r.ApprovedAtUtc,
            r.PaidAtUtc,
            r.PaymentReference,
            r.ConcurrencyToken
        );
    }

    private async Task<(int Warnings, int Blockings)> GetExceptionCountsAsync(Guid runId, CancellationToken ct)
    {
        var exceptions = await _context.PayrollExceptions
            .Where(e => e.PayrollRunId == runId && !e.IsResolved)
            .ToListAsync(ct);

        return (
            exceptions.Count(e => e.Severity == ExceptionSeverity.Warning),
            exceptions.Count(e => e.Severity == ExceptionSeverity.Blocking)
        );
    }

    private static int CalculateWorkingDays(DateOnly start, DateOnly end)
    {
        int count = 0;
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                count++;
        }
        return Math.Max(1, count);
    }

    private static TaxRuleConfig GetDefaultTaxConfig()
    {
        return new TaxRuleConfig
        {
            StandardDeduction = 0m,
            Brackets = new List<TaxBracket>
            {
                new() { LowerBound = 0, UpperBound = 3000, Rate = 0.00m, Description = "0% up to $3k" },
                new() { LowerBound = 3000, UpperBound = 6000, Rate = 0.10m, Description = "10% from $3k to $6k" },
                new() { LowerBound = 6000, UpperBound = 10000, Rate = 0.15m, Description = "15% from $6k to $10k" },
                new() { LowerBound = 10000, UpperBound = null, Rate = 0.20m, Description = "20% above $10k" }
            }
        };
    }
}
