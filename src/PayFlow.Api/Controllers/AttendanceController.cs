using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Domain.Entities;
using PayFlow.Domain.Enums;
using PayFlow.Domain.Exceptions;
using PayFlow.Infrastructure.Data;

namespace PayFlow.Api.Controllers;

[ApiController]
[Route("api/v1/attendance")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly PayFlowDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public AttendanceController(
        PayFlowDbContext context,
        ICurrentUserService currentUserService,
        IAuditService auditService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public record AttendanceDto(
        Guid Id,
        Guid EmployeeId,
        string EmployeeNo,
        string EmployeeName,
        DateOnly Date,
        string Status,
        DateTime? CheckInTimeUtc,
        DateTime? CheckOutTimeUtc,
        int WorkedMinutes,
        int OvertimeMinutes,
        string Source,
        string CorrectionStatus,
        string? CorrectionReason
    );

    public record CorrectAttendanceRequest(string Reason, int WorkedMinutes, int OvertimeMinutes, AttendanceStatus Status);

    [HttpGet]
    public async Task<IActionResult> GetAttendance(
        [FromQuery] Guid? employeeId,
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken ct = default)
    {
        var role = _currentUserService.Role;
        var myEmpId = _currentUserService.EmployeeId;

        var query = _context.AttendanceRecords
            .Include(a => a.Employee)
            .AsNoTracking();

        // Security boundaries
        if (role == UserRole.Employee)
        {
            if (!myEmpId.HasValue) return Forbid();
            query = query.Where(a => a.EmployeeId == myEmpId.Value);
        }
        else if (role == UserRole.Manager)
        {
            // Manager can see their own records OR direct reports
            if (employeeId.HasValue)
            {
                bool isReport = await _context.Employees.AnyAsync(e => e.Id == employeeId.Value && (e.ManagerId == myEmpId || e.Id == myEmpId), ct);
                if (!isReport) return Forbid();
                query = query.Where(a => a.EmployeeId == employeeId.Value);
            }
            else
            {
                var directReportIds = await _context.Employees
                    .Where(e => e.ManagerId == myEmpId || e.Id == myEmpId)
                    .Select(e => e.Id)
                    .ToListAsync(ct);
                query = query.Where(a => directReportIds.Contains(a.EmployeeId));
            }
        }
        else
        {
            if (employeeId.HasValue)
            {
                query = query.Where(a => a.EmployeeId == employeeId.Value);
            }
        }

        int targetYear = year ?? DateTime.UtcNow.Year;
        int targetMonth = month ?? DateTime.UtcNow.Month;
        var startDate = new DateOnly(targetYear, targetMonth, 1);
        var endDate = new DateOnly(targetYear, targetMonth, DateTime.DaysInMonth(targetYear, targetMonth));

        query = query.Where(a => a.Date >= startDate && a.Date <= endDate);

        var items = await query
            .OrderBy(a => a.Date)
            .ThenBy(a => a.Employee!.EmployeeNo)
            .Select(a => new AttendanceDto(
                a.Id,
                a.EmployeeId,
                a.Employee != null ? a.Employee.EmployeeNo : string.Empty,
                a.Employee != null ? a.Employee.FullName : string.Empty,
                a.Date,
                a.Status.ToString(),
                a.CheckInTimeUtc,
                a.CheckOutTimeUtc,
                a.WorkedMinutes,
                a.OvertimeMinutes,
                a.Source.ToString(),
                a.CorrectionStatus.ToString(),
                a.CorrectionReason
            ))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost("{id}/correct")]
    public async Task<IActionResult> CorrectAttendance(Guid id, [FromBody] CorrectAttendanceRequest req, CancellationToken ct)
    {
        var record = await _context.AttendanceRecords
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new EntityNotFoundException(nameof(AttendanceRecord), id);

        var role = _currentUserService.Role;
        bool isPrivileged = role == UserRole.Admin || role == UserRole.HR;
        bool isManager = role == UserRole.Manager && record.Employee?.ManagerId == _currentUserService.EmployeeId;
        bool isSelf = record.EmployeeId == _currentUserService.EmployeeId;

        if (!isPrivileged && !isManager && !isSelf)
        {
            return Forbid();
        }

        var before = new { record.WorkedMinutes, record.OvertimeMinutes, record.Status, record.CorrectionStatus };

        record.WorkedMinutes = req.WorkedMinutes;
        record.OvertimeMinutes = req.OvertimeMinutes;
        record.Status = req.Status;
        record.CorrectionReason = req.Reason;
        record.CorrectionStatus = isPrivileged || isManager ? AttendanceCorrectionStatus.Approved : AttendanceCorrectionStatus.Requested;
        record.ReviewedBy = _currentUserService.UserEmail;
        record.ReviewedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        await _auditService.LogAsync(
            "CORRECT_ATTENDANCE",
            nameof(AttendanceRecord),
            record.Id.ToString(),
            $"Attendance correction applied for {record.Employee?.FullName} on {record.Date:yyyy-MM-dd}: {req.Reason}",
            before,
            new { record.WorkedMinutes, record.OvertimeMinutes, record.Status, record.CorrectionStatus },
            ct
        );

        return Ok(new { message = "Attendance corrected successfully.", record.Id });
    }
}
