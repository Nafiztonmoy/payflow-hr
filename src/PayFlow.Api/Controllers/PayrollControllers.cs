using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Application.Payroll.Interfaces;
using PayFlow.Domain.Enums;
using PayFlow.Infrastructure.Data;

namespace PayFlow.Api.Controllers;

[ApiController]
[Route("api/v1/payroll-runs")]
[Authorize(Roles = "Admin,Accountant")]
public class PayrollRunsController : ControllerBase
{
    private readonly IPayrollRunService _payrollRunService;

    public PayrollRunsController(IPayrollRunService payrollRunService)
    {
        _payrollRunService = payrollRunService;
    }

    public record CreateRunRequest(int Year, int Month);
    public record SubmitReviewRequest(Guid ConcurrencyToken);
    public record ApproveRunRequest(Guid ConcurrencyToken);
    public record MarkPaidRequest(string PaymentReference, DateTime PaidAtUtc, Guid ConcurrencyToken);
    public record ResolveExceptionRequest(string ResolutionNotes);

    [HttpGet]
    public async Task<IActionResult> GetRuns(CancellationToken ct)
    {
        var runs = await _payrollRunService.GetRunsAsync(ct);
        return Ok(runs);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRunById(Guid id, CancellationToken ct)
    {
        var run = await _payrollRunService.GetRunByIdAsync(id, ct);
        return Ok(run);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRun([FromBody] CreateRunRequest req, CancellationToken ct)
    {
        var result = await _payrollRunService.CreateRunAsync(new CreatePayrollRunCommand(req.Year, req.Month), ct);
        return CreatedAtAction(nameof(GetRunById), new { id = result.Id }, result);
    }

    [HttpPost("{id}/calculate")]
    public async Task<IActionResult> CalculateRun(Guid id, CancellationToken ct)
    {
        var result = await _payrollRunService.CalculateRunAsync(id, ct);
        return Ok(result);
    }

    [HttpPost("{id}/submit-review")]
    public async Task<IActionResult> SubmitForReview(Guid id, [FromBody] SubmitReviewRequest req, CancellationToken ct)
    {
        var result = await _payrollRunService.SubmitForReviewAsync(new SubmitForReviewCommand(id, req.ConcurrencyToken), ct);
        return Ok(result);
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> ApproveRun(Guid id, [FromBody] ApproveRunRequest req, CancellationToken ct)
    {
        var result = await _payrollRunService.ApproveRunAsync(new ApprovePayrollRunCommand(id, req.ConcurrencyToken), ct);
        return Ok(result);
    }

    [HttpPost("{id}/mark-paid")]
    public async Task<IActionResult> MarkPaid(Guid id, [FromBody] MarkPaidRequest req, CancellationToken ct)
    {
        var result = await _payrollRunService.MarkPaidAsync(new MarkPaidCommand(id, req.PaymentReference, req.PaidAtUtc, req.ConcurrencyToken), ct);
        return Ok(result);
    }

    [HttpGet("{id}/items")]
    public async Task<IActionResult> GetRunItems(Guid id, CancellationToken ct)
    {
        var items = await _payrollRunService.GetRunItemsAsync(id, ct);
        return Ok(items);
    }

    [HttpGet("{id}/exceptions")]
    public async Task<IActionResult> GetRunExceptions(Guid id, CancellationToken ct)
    {
        var exceptions = await _payrollRunService.GetRunExceptionsAsync(id, ct);
        return Ok(exceptions);
    }

    [HttpPost("exceptions/{id}/resolve")]
    [Authorize(Roles = "Admin,Accountant,HR")]
    public async Task<IActionResult> ResolveException(Guid id, [FromBody] ResolveExceptionRequest req, CancellationToken ct)
    {
        var result = await _payrollRunService.ResolveExceptionAsync(new ResolveExceptionCommand(id, req.ResolutionNotes), ct);
        return Ok(result);
    }

    [HttpGet("{id}/export-csv")]
    public async Task<IActionResult> ExportCsv(Guid id, CancellationToken ct)
    {
        var bytes = await _payrollRunService.ExportPayrollCsvAsync(id, ct);
        var run = await _payrollRunService.GetRunByIdAsync(id, ct);
        return File(bytes, "text/csv", $"payroll-run-{run.RunNumber}.csv");
    }
}

[ApiController]
[Route("api/v1/payroll-items")]
[Authorize]
public class PayrollItemsController : ControllerBase
{
    private readonly IPayrollRunService _payrollRunService;
    private readonly PayFlowDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public PayrollItemsController(
        IPayrollRunService payrollRunService,
        PayFlowDbContext context,
        ICurrentUserService currentUserService)
    {
        _payrollRunService = payrollRunService;
        _context = context;
        _currentUserService = currentUserService;
    }

    [HttpGet("{id}/payslip")]
    public async Task<IActionResult> GetPayslip(Guid id, CancellationToken ct)
    {
        var item = await _context.PayrollItems
            .Include(i => i.PayrollRun)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (item == null)
            return NotFound(new { message = "Payroll item not found." });

        var role = _currentUserService.Role;
        var myEmpId = _currentUserService.EmployeeId;

        // CRITICAL AUTHORIZATION BOUNDARY:
        // Ordinary Employee or Manager can only view their own payslip!
        // An Employee attempting to access another employee's payslip MUST be rejected!
        if (role == UserRole.Employee || role == UserRole.Manager)
        {
            if (!myEmpId.HasValue || item.EmployeeId != myEmpId.Value)
            {
                return Forbid();
            }
        }

        var payslip = await _payrollRunService.GetPayslipByItemIdAsync(id, ct);
        return Ok(payslip);
    }

    [HttpGet("my-payslips")]
    public async Task<IActionResult> GetMyPayslips(CancellationToken ct)
    {
        var myEmpId = _currentUserService.EmployeeId;
        if (!myEmpId.HasValue)
            return Ok(new List<object>());

        var payslips = await _context.Payslips
            .Include(p => p.PayrollItem)
                .ThenInclude(i => i!.PayrollRun)
            .Where(p => p.EmployeeId == myEmpId.Value && p.PayrollItem!.PayrollRun!.Status == PayrollRunStatus.Paid)
            .OrderByDescending(p => p.PayrollItem!.PayrollRun!.PeriodYear)
            .ThenByDescending(p => p.PayrollItem!.PayrollRun!.PeriodMonth)
            .Select(p => new
            {
                p.Id,
                PayrollItemId = p.PayrollItemId,
                p.PayslipNumber,
                p.IssueDateUtc,
                Year = p.PayrollItem!.PayrollRun!.PeriodYear,
                Month = p.PayrollItem!.PayrollRun!.PeriodMonth,
                GrossPay = p.PayrollItem.GrossPay,
                TotalDeductions = p.PayrollItem.TotalDeductions,
                NetPay = p.PayrollItem.NetPay
            })
            .ToListAsync(ct);

        return Ok(payslips);
    }
}
