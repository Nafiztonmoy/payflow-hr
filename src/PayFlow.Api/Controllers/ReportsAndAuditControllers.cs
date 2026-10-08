using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Domain.Entities;
using PayFlow.Domain.Enums;
using PayFlow.Infrastructure.Data;

namespace PayFlow.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly PayFlowDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ReportsController(PayFlowDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    [HttpGet("payroll-summary")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<IActionResult> GetPayrollSummaryReport(CancellationToken ct)
    {
        var runs = await _context.PayrollRuns
            .Include(r => r.Exceptions)
            .OrderBy(r => r.PeriodYear)
            .ThenBy(r => r.PeriodMonth)
            .Select(r => new
            {
                r.Id,
                r.RunNumber,
                Period = $"{r.PeriodYear}-{r.PeriodMonth:D2}",
                r.Status,
                r.TotalEmployees,
                r.TotalGross,
                r.TotalTax,
                r.TotalDeductions,
                r.TotalNet,
                TotalExceptions = r.Exceptions.Count,
                UnresolvedExceptions = r.Exceptions.Count(e => !e.IsResolved),
                r.CalculatedAtUtc,
                r.ApprovedAtUtc,
                r.PaidAtUtc
            })
            .ToListAsync(ct);

        return Ok(runs);
    }

    [HttpGet("labor-cost")]
    [Authorize(Roles = "Admin,Accountant,HR")]
    public async Task<IActionResult> GetLaborCostReport([FromQuery] int? year, CancellationToken ct)
    {
        int targetYear = year ?? DateTime.UtcNow.Year;

        var items = await _context.PayrollItems
            .Include(i => i.PayrollRun)
            .Include(i => i.Employee)
                .ThenInclude(e => e!.Department)
            .Where(i => i.PayrollRun!.PeriodYear == targetYear &&
                        (i.PayrollRun.Status == PayrollRunStatus.Paid || i.PayrollRun.Status == PayrollRunStatus.Approved || i.PayrollRun.Status == PayrollRunStatus.Calculated))
            .ToListAsync(ct);

        var byDepartment = items
            .GroupBy(i => i.Employee?.Department?.Name ?? "Unassigned")
            .Select(g => new
            {
                Department = g.Key,
                TotalGross = g.Sum(x => x.GrossPay),
                TotalBase = g.Sum(x => x.BaseSalary),
                TotalTax = g.Sum(x => x.IncomeTax),
                TotalNet = g.Sum(x => x.NetPay),
                EmployeeCount = g.Select(x => x.EmployeeId).Distinct().Count()
            })
            .OrderByDescending(x => x.TotalGross)
            .ToList();

        return Ok(new { Year = targetYear, Departments = byDepartment });
    }

    [HttpGet("leave")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> GetLeaveReport(CancellationToken ct)
    {
        int year = DateTime.UtcNow.Year;
        var balances = await _context.LeaveBalances
            .Include(b => b.LeaveType)
            .Include(b => b.Employee)
                .ThenInclude(e => e!.Department)
            .Where(b => b.Year == year)
            .ToListAsync(ct);

        var report = balances
            .GroupBy(b => new { Dept = b.Employee?.Department?.Name ?? "Unassigned", LeaveType = b.LeaveType?.Name ?? "Unknown" })
            .Select(g => new
            {
                Department = g.Key.Dept,
                LeaveType = g.Key.LeaveType,
                TotalOpening = g.Sum(x => x.OpeningBalance),
                TotalUsed = g.Sum(x => x.UsedDays),
                TotalRemaining = g.Sum(x => x.RemainingDays)
            })
            .OrderBy(x => x.Department)
            .ThenBy(x => x.LeaveType)
            .ToList();

        return Ok(new { Year = year, Utilization = report });
    }

    [HttpGet("attendance")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> GetAttendanceReport([FromQuery] int? year, [FromQuery] int? month, CancellationToken ct)
    {
        int targetYear = year ?? DateTime.UtcNow.Year;
        int targetMonth = month ?? DateTime.UtcNow.Month;
        var startDate = new DateOnly(targetYear, targetMonth, 1);
        var endDate = new DateOnly(targetYear, targetMonth, DateTime.DaysInMonth(targetYear, targetMonth));

        var records = await _context.AttendanceRecords
            .Include(a => a.Employee)
                .ThenInclude(e => e!.Department)
            .Where(a => a.Date >= startDate && a.Date <= endDate)
            .ToListAsync(ct);

        var stats = records
            .GroupBy(a => a.Employee?.Department?.Name ?? "Unassigned")
            .Select(g => new
            {
                Department = g.Key,
                TotalRecords = g.Count(),
                PresentCount = g.Count(x => x.Status == AttendanceStatus.Present),
                AbsentCount = g.Count(x => x.Status == AttendanceStatus.Absent),
                OnLeaveCount = g.Count(x => x.Status == AttendanceStatus.OnLeave),
                TotalOvertimeHours = Math.Round(g.Sum(x => x.OvertimeMinutes) / 60.0m, 1),
                TotalWorkedHours = Math.Round(g.Sum(x => x.WorkedMinutes) / 60.0m, 1)
            })
            .OrderBy(x => x.Department)
            .ToList();

        return Ok(new { Year = targetYear, Month = targetMonth, DepartmentStats = stats });
    }
}

[ApiController]
[Route("api/v1/audit-logs")]
[Authorize(Roles = "Admin")]
public class AuditLogsController : ControllerBase
{
    private readonly PayFlowDbContext _context;

    public AuditLogsController(PayFlowDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? action,
        [FromQuery] string? entityType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = _context.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action.ToLower().Contains(action.ToLower()));

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(a => a.EntityType.ToLower().Contains(entityType.ToLower()));

        var total = await query.CountAsync(ct);
        var logs = await query
            .OrderByDescending(a => a.TimestampUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Ok(new { total, page, pageSize, items = logs });
    }
}

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly PayFlowDbContext _context;

    public HealthController(PayFlowDbContext context)
    {
        _context = context;
    }

    [HttpGet("live")]
    [AllowAnonymous]
    public IActionResult Live()
    {
        return Ok(new { status = "Healthy", timestamp = DateTime.UtcNow });
    }

    [HttpGet("ready")]
    [AllowAnonymous]
    public async Task<IActionResult> Ready(CancellationToken ct)
    {
        try
        {
            bool canConnect = await _context.Database.CanConnectAsync(ct);
            if (!canConnect)
            {
                return StatusCode(503, new { status = "Unhealthy", error = "Cannot connect to database" });
            }
            return Ok(new { status = "Healthy", database = "Connected", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { status = "Unhealthy", error = ex.Message });
        }
    }
}
