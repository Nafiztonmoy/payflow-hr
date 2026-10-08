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
[Route("api/v1")]
[Authorize]
public class LeaveController : ControllerBase
{
    private readonly PayFlowDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public LeaveController(
        PayFlowDbContext context,
        ICurrentUserService currentUserService,
        IAuditService auditService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public record LeaveRequestDto(
        Guid Id,
        Guid EmployeeId,
        string EmployeeNo,
        string EmployeeName,
        string DepartmentName,
        Guid LeaveTypeId,
        string LeaveTypeName,
        DateOnly StartDate,
        DateOnly EndDate,
        decimal DayCount,
        string Reason,
        string Status,
        string? ReviewRemarks,
        DateTime? ReviewedAtUtc
    );

    public record CreateLeaveRequest(
        Guid LeaveTypeId,
        DateOnly StartDate,
        DateOnly EndDate,
        string Reason
    );

    public record ReviewLeaveRequest(string? Remarks);

    [HttpGet("leave-requests")]
    public async Task<IActionResult> GetLeaveRequests([FromQuery] LeaveRequestStatus? status, CancellationToken ct)
    {
        var role = _currentUserService.Role;
        var myEmpId = _currentUserService.EmployeeId;

        var query = _context.LeaveRequests
            .Include(l => l.Employee)
                .ThenInclude(e => e!.Department)
            .Include(l => l.LeaveType)
            .AsNoTracking();

        if (role == UserRole.Employee)
        {
            if (!myEmpId.HasValue) return Forbid();
            query = query.Where(l => l.EmployeeId == myEmpId.Value);
        }
        else if (role == UserRole.Manager)
        {
            var directReportIds = await _context.Employees
                .Where(e => e.ManagerId == myEmpId || e.Id == myEmpId)
                .Select(e => e.Id)
                .ToListAsync(ct);
            query = query.Where(l => directReportIds.Contains(l.EmployeeId));
        }

        if (status.HasValue)
        {
            query = query.Where(l => l.Status == status.Value);
        }

        var items = await query
            .OrderByDescending(l => l.CreatedAtUtc)
            .Select(l => new LeaveRequestDto(
                l.Id,
                l.EmployeeId,
                l.Employee != null ? l.Employee.EmployeeNo : string.Empty,
                l.Employee != null ? l.Employee.FullName : string.Empty,
                l.Employee != null && l.Employee.Department != null ? l.Employee.Department.Name : "N/A",
                l.LeaveTypeId,
                l.LeaveType != null ? l.LeaveType.Name : string.Empty,
                l.StartDate,
                l.EndDate,
                l.DayCount,
                l.Reason,
                l.Status.ToString(),
                l.ReviewRemarks,
                l.ReviewedAtUtc
            ))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost("leave-requests")]
    public async Task<IActionResult> SubmitLeaveRequest([FromBody] CreateLeaveRequest req, CancellationToken ct)
    {
        var empId = _currentUserService.EmployeeId;
        if (!empId.HasValue)
            return BadRequest(new { message = "Current user is not associated with an Employee profile." });

        if (req.StartDate > req.EndDate)
            return BadRequest(new { message = "Start date must be before or equal to End date." });

        var leaveType = await _context.LeaveTypes.FindAsync(new object[] { req.LeaveTypeId }, ct);
        if (leaveType == null)
            return BadRequest(new { message = "Invalid leave type." });

        // Calculate days (excluding weekends)
        decimal days = 0;
        for (var date = req.StartDate; date <= req.EndDate; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
            {
                days += 1.0m;
            }
        }

        if (days <= 0)
            return BadRequest(new { message = "Leave request covers zero working days." });

        // Check for overlap
        bool hasOverlap = await _context.LeaveRequests
            .AnyAsync(l => l.EmployeeId == empId.Value &&
                           l.Status != LeaveRequestStatus.Rejected &&
                           l.Status != LeaveRequestStatus.Cancelled &&
                           l.StartDate <= req.EndDate &&
                           l.EndDate >= req.StartDate, ct);

        if (hasOverlap)
            return BadRequest(new { message = "An existing leave request overlaps with the requested date range." });

        // Check leave balance
        int currentYear = req.StartDate.Year;
        var balance = await _context.LeaveBalances
            .FirstOrDefaultAsync(b => b.EmployeeId == empId.Value && b.LeaveTypeId == req.LeaveTypeId && b.Year == currentYear, ct);

        if (balance != null && balance.RemainingDays < days)
        {
            return BadRequest(new { message = $"Insufficient leave balance. Remaining: {balance.RemainingDays} days, Requested: {days} days." });
        }

        var leaveRequest = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = empId.Value,
            LeaveTypeId = req.LeaveTypeId,
            StartDate = req.StartDate,
            EndDate = req.EndDate,
            DayCount = days,
            Reason = req.Reason,
            Status = LeaveRequestStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.LeaveRequests.Add(leaveRequest);
        await _context.SaveChangesAsync(ct);

        await _auditService.LogAsync(
            "SUBMIT_LEAVE_REQUEST",
            nameof(LeaveRequest),
            leaveRequest.Id.ToString(),
            $"Submitted leave request for {days} days ({req.StartDate:yyyy-MM-dd} to {req.EndDate:yyyy-MM-dd}). Reason: {req.Reason}",
            null,
            leaveRequest,
            ct
        );

        return Ok(leaveRequest);
    }

    [HttpPost("leave-requests/{id}/approve")]
    public async Task<IActionResult> ApproveLeaveRequest(Guid id, [FromBody] ReviewLeaveRequest? req, CancellationToken ct)
    {
        var leave = await _context.LeaveRequests
            .Include(l => l.Employee)
            .Include(l => l.LeaveType)
            .FirstOrDefaultAsync(l => l.Id == id, ct)
            ?? throw new EntityNotFoundException(nameof(LeaveRequest), id);

        var role = _currentUserService.Role;
        bool isPrivileged = role == UserRole.Admin || role == UserRole.HR;
        bool isManager = role == UserRole.Manager && leave.Employee?.ManagerId == _currentUserService.EmployeeId;

        if (!isPrivileged && !isManager)
            return Forbid();

        if (leave.Status != LeaveRequestStatus.Pending)
            return BadRequest(new { message = $"Leave request is already in '{leave.Status}' status." });

        leave.Status = LeaveRequestStatus.Approved;
        leave.ReviewedByUserId = _currentUserService.UserId;
        leave.ReviewedAtUtc = DateTime.UtcNow;
        leave.ReviewRemarks = req?.Remarks ?? "Approved";

        // Update balance
        int year = leave.StartDate.Year;
        var balance = await _context.LeaveBalances
            .FirstOrDefaultAsync(b => b.EmployeeId == leave.EmployeeId && b.LeaveTypeId == leave.LeaveTypeId && b.Year == year, ct);

        if (balance != null)
        {
            balance.UsedDays += leave.DayCount;
        }

        await _context.SaveChangesAsync(ct);

        await _auditService.LogAsync(
            "APPROVE_LEAVE_REQUEST",
            nameof(LeaveRequest),
            leave.Id.ToString(),
            $"Approved leave request of {leave.Employee?.FullName} for {leave.DayCount} days.",
            null,
            new { leave.Id, leave.Status, leave.ReviewRemarks },
            ct
        );

        return Ok(new { message = "Leave request approved.", leave.Id });
    }

    [HttpPost("leave-requests/{id}/reject")]
    public async Task<IActionResult> RejectLeaveRequest(Guid id, [FromBody] ReviewLeaveRequest req, CancellationToken ct)
    {
        var leave = await _context.LeaveRequests
            .Include(l => l.Employee)
            .FirstOrDefaultAsync(l => l.Id == id, ct)
            ?? throw new EntityNotFoundException(nameof(LeaveRequest), id);

        var role = _currentUserService.Role;
        bool isPrivileged = role == UserRole.Admin || role == UserRole.HR;
        bool isManager = role == UserRole.Manager && leave.Employee?.ManagerId == _currentUserService.EmployeeId;

        if (!isPrivileged && !isManager)
            return Forbid();

        if (leave.Status != LeaveRequestStatus.Pending)
            return BadRequest(new { message = $"Leave request is already in '{leave.Status}' status." });

        leave.Status = LeaveRequestStatus.Rejected;
        leave.ReviewedByUserId = _currentUserService.UserId;
        leave.ReviewedAtUtc = DateTime.UtcNow;
        leave.ReviewRemarks = req.Remarks ?? "Rejected";

        await _context.SaveChangesAsync(ct);

        await _auditService.LogAsync(
            "REJECT_LEAVE_REQUEST",
            nameof(LeaveRequest),
            leave.Id.ToString(),
            $"Rejected leave request of {leave.Employee?.FullName}. Remarks: {leave.ReviewRemarks}",
            null,
            new { leave.Id, leave.Status, leave.ReviewRemarks },
            ct
        );

        return Ok(new { message = "Leave request rejected.", leave.Id });
    }

    [HttpGet("leave-balances/me")]
    public async Task<IActionResult> GetMyLeaveBalances(CancellationToken ct)
    {
        var empId = _currentUserService.EmployeeId;
        if (!empId.HasValue)
            return Ok(new List<object>());

        int year = DateTime.UtcNow.Year;
        var balances = await _context.LeaveBalances
            .Include(b => b.LeaveType)
            .Where(b => b.EmployeeId == empId.Value && b.Year == year)
            .Select(b => new
            {
                b.Id,
                b.LeaveTypeId,
                LeaveTypeName = b.LeaveType != null ? b.LeaveType.Name : "N/A",
                LeaveTypeCode = b.LeaveType != null ? b.LeaveType.Code : "N/A",
                IsPaid = b.LeaveType != null && b.LeaveType.IsPaid,
                b.Year,
                b.OpeningBalance,
                b.AccruedDays,
                b.UsedDays,
                b.AdjustedDays,
                b.RemainingDays
            })
            .ToListAsync(ct);

        return Ok(balances);
    }
}
