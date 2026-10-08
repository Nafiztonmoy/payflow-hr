using PayFlow.Domain.Common;
using PayFlow.Domain.Enums;

namespace PayFlow.Domain.Entities;

public class AttendanceRecord : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public DateOnly Date { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
    public DateTime? CheckInTimeUtc { get; set; }
    public DateTime? CheckOutTimeUtc { get; set; }
    public int WorkedMinutes { get; set; } = 480; // 8 hours default
    public int OvertimeMinutes { get; set; } = 0;
    public AttendanceSource Source { get; set; } = AttendanceSource.SelfService;
    public AttendanceCorrectionStatus CorrectionStatus { get; set; } = AttendanceCorrectionStatus.None;
    public string? CorrectionReason { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }

    // Navigation
    public virtual Employee? Employee { get; set; }
}

public class LeaveType : BaseEntity
{
    public string Name { get; set; } = string.Empty; // e.g. Annual Leave, Sick Leave, Unpaid Leave
    public string Code { get; set; } = string.Empty; // e.g. AL, SL, UL
    public int AnnualEntitlementDays { get; set; } = 15;
    public int MaxCarryForwardDays { get; set; } = 5;
    public bool IsPaid { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }

    // Navigation
    public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    public virtual ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();
}

public class LeaveBalance : BaseEntity, IConcurrencyAware
{
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public int Year { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal AccruedDays { get; set; }
    public decimal UsedDays { get; set; }
    public decimal AdjustedDays { get; set; }
    public decimal RemainingDays => OpeningBalance + AccruedDays + AdjustedDays - UsedDays;
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();

    // Navigation
    public virtual Employee? Employee { get; set; }
    public virtual LeaveType? LeaveType { get; set; }
}

public class LeaveRequest : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal DayCount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Pending;
    public Guid? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewRemarks { get; set; }

    // Navigation
    public virtual Employee? Employee { get; set; }
    public virtual LeaveType? LeaveType { get; set; }
    public virtual User? ReviewedByUser { get; set; }
}
