namespace PayFlow.Domain.Enums;

public enum UserRole
{
    Admin,
    HR,
    Manager,
    Accountant,
    Employee
}

public enum EmploymentStatus
{
    Active,
    OnLeave,
    Terminated
}

public enum PaymentMethod
{
    BankTransfer,
    Cheque,
    Cash
}

public enum PayComponentType
{
    Earning,
    Deduction
}

public enum CalculationMode
{
    FixedAmount,
    PercentageOfBase
}

public enum AttendanceStatus
{
    Present,
    Absent,
    HalfDay,
    OnLeave,
    Holiday,
    Weekend
}

public enum AttendanceCorrectionStatus
{
    None,
    Requested,
    Approved,
    Rejected
}

public enum AttendanceSource
{
    Biometric,
    Manual,
    SelfService
}

public enum LeaveRequestStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled
}

public enum PayrollRunStatus
{
    Draft,
    Calculating,
    Calculated,
    InReview,
    Approved,
    Paid,
    Failed
}

public enum PayrollItemStatus
{
    Ok,
    HasWarnings,
    HasBlockingExceptions
}

public enum ExceptionSeverity
{
    Warning,
    Blocking
}

public enum PayrollFrequency
{
    Monthly,
    BiWeekly
}
