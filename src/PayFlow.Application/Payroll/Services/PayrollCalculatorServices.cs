using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PayFlow.Application.Payroll.Interfaces;
using PayFlow.Application.Payroll.Models;
using PayFlow.Domain.Entities;
using PayFlow.Domain.Enums;
using PayFlow.Domain.Models;

namespace PayFlow.Application.Payroll.Services;

public class CompensationResolver : ICompensationResolver
{
    public EmployeeCompensation? ResolveEffectiveCompensation(Employee employee, PayrollPeriod period)
    {
        if (employee.Compensations == null || !employee.Compensations.Any())
            return null;

        var periodStart = period.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var periodEnd = period.EndDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        return employee.Compensations
            .Where(c => c.IsActive &&
                        c.EffectiveFrom <= periodEnd &&
                        (c.EffectiveTo == null || c.EffectiveTo >= periodStart))
            .OrderByDescending(c => c.EffectiveFrom)
            .FirstOrDefault();
    }
}

public class LeaveImpactCalculator : ILeaveImpactCalculator
{
    public (decimal DeductionAmount, string Explanation) CalculateUnpaidLeaveImpact(
        decimal baseSalary, int totalWorkingDays, int unpaidLeaveDays)
    {
        if (unpaidLeaveDays <= 0 || totalWorkingDays <= 0 || baseSalary <= 0)
        {
            return (0m, "No unpaid leave deductions.");
        }

        decimal dailyRate = RoundingPolicy.RoundCurrency(baseSalary / totalWorkingDays);
        decimal deductionAmount = RoundingPolicy.RoundCurrency(dailyRate * unpaidLeaveDays);

        string explanation = $"Unpaid Leave: {unpaidLeaveDays} day(s) @ ${dailyRate:N2}/day (Base ${baseSalary:N2} / {totalWorkingDays} days) = -${deductionAmount:N2}";
        return (deductionAmount, explanation);
    }
}

public class OvertimeCalculator : IOvertimeCalculator
{
    public (decimal OvertimePay, decimal HourlyRate, string Explanation) CalculateOvertimePay(
        decimal baseSalary, int totalWorkingDays, decimal standardDailyHours, int overtimeMinutes, decimal overtimeMultiplier = 1.5m)
    {
        if (overtimeMinutes <= 0 || totalWorkingDays <= 0 || baseSalary <= 0 || standardDailyHours <= 0)
        {
            return (0m, 0m, "No overtime worked.");
        }

        decimal standardHoursPerMonth = totalWorkingDays * standardDailyHours;
        decimal hourlyRate = RoundingPolicy.RoundCurrency(baseSalary / standardHoursPerMonth);
        decimal overtimeHours = Math.Round(overtimeMinutes / 60.0m, 2, MidpointRounding.AwayFromZero);
        decimal overtimePay = RoundingPolicy.RoundCurrency(hourlyRate * overtimeHours * overtimeMultiplier);

        string explanation = $"Overtime: {overtimeHours:N2} hrs @ ${hourlyRate:N2}/hr x {overtimeMultiplier:N2} multiplier = ${overtimePay:N2}";
        return (overtimePay, hourlyRate, explanation);
    }
}

public class TaxCalculator : ITaxCalculator
{
    public (decimal TaxAmount, string Explanation) CalculateTax(decimal taxableGross, TaxRuleConfig taxConfig)
    {
        if (taxConfig == null || taxableGross <= 0)
        {
            return (0m, "Tax: $0.00 (Zero or negative taxable gross)");
        }

        decimal netTaxable = Math.Max(0m, taxableGross - taxConfig.StandardDeduction);
        if (netTaxable <= 0)
        {
            return (0m, $"Tax: $0.00 (Taxable ${taxableGross:N2} fully offset by standard deduction ${taxConfig.StandardDeduction:N2})");
        }

        decimal totalTax = 0m;
        var breakdownList = new List<string>();

        if (taxConfig.StandardDeduction > 0)
        {
            breakdownList.Add($"Std Deduction: -${taxConfig.StandardDeduction:N2}");
        }

        var orderedBrackets = taxConfig.Brackets.OrderBy(b => b.LowerBound).ToList();
        foreach (var bracket in orderedBrackets)
        {
            if (netTaxable <= bracket.LowerBound)
                continue;

            decimal bracketCeiling = bracket.UpperBound ?? netTaxable;
            decimal taxableInBracket = Math.Min(netTaxable, bracketCeiling) - bracket.LowerBound;

            if (taxableInBracket > 0)
            {
                decimal taxForBracket = RoundingPolicy.RoundCurrency(taxableInBracket * bracket.Rate);
                totalTax += taxForBracket;

                string upperStr = bracket.UpperBound.HasValue ? $"${bracket.UpperBound.Value:N0}" : "above";
                breakdownList.Add($"[${bracket.LowerBound:N0}-{upperStr} @ {bracket.Rate * 100:N0}%: ${taxForBracket:N2}]");
            }
        }

        totalTax = RoundingPolicy.RoundCurrency(totalTax);
        string explanation = $"Demo Tax: ${totalTax:N2} on net taxable ${netTaxable:N2} -> " + string.Join(" + ", breakdownList);
        return (totalTax, explanation);
    }
}

public class AttendanceSummaryService : IAttendanceSummaryService
{
    public AttendanceSummary CalculateSummary(
        Employee employee, 
        IEnumerable<AttendanceRecord> attendanceRecords, 
        IEnumerable<LeaveRequest> approvedLeaves, 
        PayrollPeriod period)
    {
        var records = attendanceRecords?.ToList() ?? new List<AttendanceRecord>();
        var leaves = approvedLeaves?.ToList() ?? new List<LeaveRequest>();

        int daysWorked = records.Count(r => r.Status == AttendanceStatus.Present || r.Status == AttendanceStatus.HalfDay);
        int overtimeMinutes = records.Sum(r => r.OvertimeMinutes);

        int paidLeaveDays = 0;
        int unpaidLeaveDays = 0;

        foreach (var leave in leaves.Where(l => l.Status == LeaveRequestStatus.Approved))
        {
            if (leave.LeaveType?.IsPaid == false)
            {
                unpaidLeaveDays += (int)Math.Ceiling(leave.DayCount);
            }
            else
            {
                paidLeaveDays += (int)Math.Ceiling(leave.DayCount);
            }
        }

        int absentDays = records.Count(r => r.Status == AttendanceStatus.Absent);

        // If records are empty or few, determine if missing attendance
        bool hasMissingAttendance = !records.Any() && employee.Status == EmploymentStatus.Active;

        return new AttendanceSummary(
            TotalWorkingDays: period.TotalWorkingDays,
            DaysWorked: daysWorked,
            PaidLeaveDays: paidLeaveDays,
            UnpaidLeaveDays: unpaidLeaveDays,
            AbsentDays: absentDays,
            OvertimeMinutes: overtimeMinutes,
            HasMissingAttendance: hasMissingAttendance
        );
    }
}

public class PayrollCalculator : IPayrollCalculator
{
    private readonly ILeaveImpactCalculator _leaveImpactCalculator;
    private readonly IOvertimeCalculator _overtimeCalculator;
    private readonly ITaxCalculator _taxCalculator;

    public PayrollCalculator(
        ILeaveImpactCalculator leaveImpactCalculator,
        IOvertimeCalculator overtimeCalculator,
        ITaxCalculator taxCalculator)
    {
        _leaveImpactCalculator = leaveImpactCalculator;
        _overtimeCalculator = overtimeCalculator;
        _taxCalculator = taxCalculator;
    }

    public CalculatedPayrollItemResult CalculateEmployeePayroll(EmployeePayrollContext context)
    {
        var employee = context.Employee;
        var period = context.Period;
        var exceptions = new List<PayrollExceptionResult>();
        var components = new List<CalculatedComponentResult>();

        // 1. Validation checks
        if (context.ActiveCompensation == null)
        {
            exceptions.Add(new PayrollExceptionResult(
                ExceptionSeverity.Blocking,
                "MISSING_COMPENSATION",
                $"Employee {employee.FullName} ({employee.EmployeeNo}) has no active salary compensation assigned for period {period.Year}-{period.Month:D2}."
            ));
        }

        if (employee.PaymentMethod == PaymentMethod.BankTransfer &&
            (string.IsNullOrWhiteSpace(employee.BankAccountNumber) || string.IsNullOrWhiteSpace(employee.BankRoutingNumber)))
        {
            exceptions.Add(new PayrollExceptionResult(
                ExceptionSeverity.Blocking,
                "MISSING_BANK_ACCOUNT",
                $"Employee {employee.FullName} has BankTransfer payment method but is missing Bank Account Number or Routing Number."
            ));
        }

        decimal baseSalary = context.ActiveCompensation?.BaseSalary ?? 0m;

        // 2. Mid-period join or termination proration
        decimal prorationFactor = 1.0m;
        var joinDateOnly = DateOnly.FromDateTime(employee.JoinDate);
        DateOnly? termDateOnly = employee.TerminationDate.HasValue ? DateOnly.FromDateTime(employee.TerminationDate.Value) : null;

        bool isMidPeriodJoin = joinDateOnly > period.StartDate && joinDateOnly <= period.EndDate;
        bool isMidPeriodTerm = termDateOnly.HasValue && termDateOnly.Value >= period.StartDate && termDateOnly.Value < period.EndDate;

        string baseExplanation = $"Base Salary: ${baseSalary:N2} (Standard monthly rate)";

        if (isMidPeriodJoin || isMidPeriodTerm)
        {
            var effectiveStart = isMidPeriodJoin ? joinDateOnly : period.StartDate;
            var effectiveEnd = isMidPeriodTerm ? termDateOnly!.Value : period.EndDate;
            int activeDays = Math.Max(1, effectiveEnd.Day - effectiveStart.Day + 1);
            int totalDaysInMonth = period.EndDate.Day;
            
            prorationFactor = Math.Min(1.0m, Math.Max(0.0m, (decimal)activeDays / totalDaysInMonth));
            decimal proratedBase = RoundingPolicy.RoundCurrency(baseSalary * prorationFactor);
            baseExplanation = $"Prorated Base: ${proratedBase:N2} ({activeDays}/{totalDaysInMonth} calendar days active in period @ ${baseSalary:N2} full base)";
            baseSalary = proratedBase;
        }

        components.Add(new CalculatedComponentResult(
            Code: "BASIC",
            DisplayName: "Basic Salary",
            Type: PayComponentType.Earning,
            Amount: baseSalary,
            SourceRule: "Contract Base Rate",
            Explanation: baseExplanation,
            IsTaxable: true
        ));

        // 3. Process structure pay components
        var configuredComponents = context.Components ?? context.SalaryStructure?.Components?.ToList() ?? new List<PayComponent>();

        foreach (var comp in configuredComponents.Where(c => c.IsActive && c.Code != "BASIC").OrderBy(c => c.DisplayOrder))
        {
            decimal compAmount = 0m;
            string compExplanation = string.Empty;

            if (comp.CalculationMode == CalculationMode.PercentageOfBase)
            {
                compAmount = RoundingPolicy.RoundCurrency(baseSalary * comp.PercentageRate);
                compExplanation = $"{comp.DisplayName}: {comp.PercentageRate * 100:N1}% of Basic (${baseSalary:N2}) = ${compAmount:N2}";
            }
            else
            {
                compAmount = comp.DefaultAmount;
                compExplanation = $"{comp.DisplayName}: Fixed amount ${compAmount:N2}";
            }

            components.Add(new CalculatedComponentResult(
                Code: comp.Code,
                DisplayName: comp.DisplayName,
                Type: comp.Type,
                Amount: compAmount,
                SourceRule: $"Salary Structure '{context.SalaryStructure?.Name ?? "Standard"}'",
                Explanation: compExplanation,
                IsTaxable: comp.IsTaxable
            ));
        }

        // 4. Overtime calculation
        if (context.AttendanceSummary.OvertimeMinutes > 0)
        {
            var (otPay, otRate, otExpl) = _overtimeCalculator.CalculateOvertimePay(
                baseSalary,
                period.TotalWorkingDays,
                period.StandardDailyHours,
                context.AttendanceSummary.OvertimeMinutes
            );

            if (otPay > 0)
            {
                components.Add(new CalculatedComponentResult(
                    Code: "OVERTIME",
                    DisplayName: "Overtime Pay",
                    Type: PayComponentType.Earning,
                    Amount: otPay,
                    SourceRule: "Approved Attendance Timesheet",
                    Explanation: otExpl,
                    IsTaxable: true
                ));
            }

            if (context.AttendanceSummary.OvertimeMinutes > 2400) // > 40 hours
            {
                exceptions.Add(new PayrollExceptionResult(
                    ExceptionSeverity.Warning,
                    "HIGH_OVERTIME",
                    $"Employee recorded {Math.Round(context.AttendanceSummary.OvertimeMinutes / 60.0m, 1)} hours of overtime in period."
                ));
            }
        }

        // 5. Unpaid leave deduction
        if (context.AttendanceSummary.UnpaidLeaveDays > 0)
        {
            var (unpaidDeduction, unpaidExpl) = _leaveImpactCalculator.CalculateUnpaidLeaveImpact(
                baseSalary,
                period.TotalWorkingDays,
                context.AttendanceSummary.UnpaidLeaveDays
            );

            if (unpaidDeduction > 0)
            {
                components.Add(new CalculatedComponentResult(
                    Code: "UNPAID_LEAVE",
                    DisplayName: "Unpaid Leave Deduction",
                    Type: PayComponentType.Deduction,
                    Amount: unpaidDeduction,
                    SourceRule: "Approved Unpaid Leave Policy",
                    Explanation: unpaidExpl,
                    IsTaxable: false
                ));

                exceptions.Add(new PayrollExceptionResult(
                    ExceptionSeverity.Warning,
                    "UNPAID_ABSENCE",
                    $"Employee had {context.AttendanceSummary.UnpaidLeaveDays} day(s) of unpaid leave deducted."
                ));
            }
        }

        // 6. Calculate Gross Pay
        decimal totalGross = components
            .Where(c => c.Type == PayComponentType.Earning)
            .Sum(c => c.Amount);
        totalGross = RoundingPolicy.RoundCurrency(totalGross);

        // 7. Calculate Taxable Amount and Income Tax
        decimal taxableAmount = components
            .Where(c => c.Type == PayComponentType.Earning && c.IsTaxable)
            .Sum(c => c.Amount);
        taxableAmount = RoundingPolicy.RoundCurrency(taxableAmount);

        var (taxAmount, taxExpl) = _taxCalculator.CalculateTax(taxableAmount, context.TaxConfig);
        if (taxAmount > 0)
        {
            components.Add(new CalculatedComponentResult(
                Code: "INCOME_TAX",
                DisplayName: "Income Tax (Withholding)",
                Type: PayComponentType.Deduction,
                Amount: taxAmount,
                SourceRule: "Progressive Demo Tax Brackets",
                Explanation: taxExpl,
                IsTaxable: false
            ));
        }

        // 8. Total Deductions
        decimal totalDeductions = components
            .Where(c => c.Type == PayComponentType.Deduction)
            .Sum(c => c.Amount);
        totalDeductions = RoundingPolicy.RoundCurrency(totalDeductions);

        // 9. Net Pay
        decimal netPay = RoundingPolicy.RoundCurrency(totalGross - totalDeductions);

        if (netPay < 0)
        {
            exceptions.Add(new PayrollExceptionResult(
                ExceptionSeverity.Blocking,
                "NEGATIVE_NET",
                $"Calculated net pay is negative (${netPay:N2}). Total deductions (${totalDeductions:N2}) exceed gross pay (${totalGross:N2})."
            ));
        }

        // 10. Status
        var status = PayrollItemStatus.Ok;
        if (exceptions.Any(e => e.Severity == ExceptionSeverity.Blocking))
        {
            status = PayrollItemStatus.HasBlockingExceptions;
        }
        else if (exceptions.Any(e => e.Severity == ExceptionSeverity.Warning))
        {
            status = PayrollItemStatus.HasWarnings;
        }

        // 11. Snapshot and Hash
        var snapshotObj = new
        {
            EmployeeId = employee.Id,
            EmployeeNo = employee.EmployeeNo,
            FullName = employee.FullName,
            PeriodYear = period.Year,
            PeriodMonth = period.Month,
            BaseSalary = baseSalary,
            GrossPay = totalGross,
            TaxableAmount = taxableAmount,
            IncomeTax = taxAmount,
            TotalDeductions = totalDeductions,
            NetPay = netPay,
            Components = components,
            Exceptions = exceptions,
            CalculatedAtUtc = DateTime.UtcNow
        };

        string snapshotJson = JsonSerializer.Serialize(snapshotObj);
        string calculationHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshotJson)));

        return new CalculatedPayrollItemResult(
            EmployeeId: employee.Id,
            BaseSalary: baseSalary,
            GrossPay: totalGross,
            TaxableAmount: taxableAmount,
            IncomeTax: taxAmount,
            TotalDeductions: totalDeductions,
            NetPay: netPay,
            Status: status,
            Components: components,
            Exceptions: exceptions,
            SnapshotJson: snapshotJson,
            CalculationHash: calculationHash
        );
    }
}
