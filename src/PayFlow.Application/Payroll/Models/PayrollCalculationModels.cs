using PayFlow.Domain.Entities;
using PayFlow.Domain.Enums;
using PayFlow.Domain.Models;

namespace PayFlow.Application.Payroll.Models;

public record PayrollPeriod(int Year, int Month, DateOnly StartDate, DateOnly EndDate, int TotalWorkingDays, decimal StandardDailyHours = 8.0m);

public record EmployeePayrollContext(
    Employee Employee,
    EmployeeCompensation? ActiveCompensation,
    SalaryStructure? SalaryStructure,
    List<PayComponent> Components,
    AttendanceSummary AttendanceSummary,
    TaxRuleConfig TaxConfig,
    PayrollPeriod Period
);

public record AttendanceSummary(
    int TotalWorkingDays,
    int DaysWorked,
    int PaidLeaveDays,
    int UnpaidLeaveDays,
    int AbsentDays,
    int OvertimeMinutes,
    bool HasMissingAttendance
);

public record CalculatedComponentResult(
    string Code,
    string DisplayName,
    PayComponentType Type,
    decimal Amount,
    string SourceRule,
    string Explanation,
    bool IsTaxable
);

public record CalculatedPayrollItemResult(
    Guid EmployeeId,
    decimal BaseSalary,
    decimal GrossPay,
    decimal TaxableAmount,
    decimal IncomeTax,
    decimal TotalDeductions,
    decimal NetPay,
    PayrollItemStatus Status,
    List<CalculatedComponentResult> Components,
    List<PayrollExceptionResult> Exceptions,
    string SnapshotJson,
    string CalculationHash
);

public record PayrollExceptionResult(
    ExceptionSeverity Severity,
    string ExceptionType,
    string Message
);
