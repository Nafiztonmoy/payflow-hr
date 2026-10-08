using PayFlow.Domain.Common;
using PayFlow.Domain.Enums;

namespace PayFlow.Domain.Entities;

public class Employee : BaseEntity
{
    public string EmployeeNo { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string WorkEmail { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime DateOfBirth { get; set; }
    public DateTime JoinDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public EmploymentStatus Status { get; set; } = EmploymentStatus.Active;

    public Guid DepartmentId { get; set; }
    public Guid DesignationId { get; set; }
    public Guid? ManagerId { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.BankTransfer;
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankRoutingNumber { get; set; }

    // Navigations
    public virtual Department? Department { get; set; }
    public virtual Designation? Designation { get; set; }
    public virtual Employee? Manager { get; set; }
    public virtual ICollection<Employee> DirectReports { get; set; } = new List<Employee>();
    public virtual ICollection<EmployeeCompensation> Compensations { get; set; } = new List<EmployeeCompensation>();
    public virtual ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    public virtual ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();
    public virtual User? User { get; set; }
}
