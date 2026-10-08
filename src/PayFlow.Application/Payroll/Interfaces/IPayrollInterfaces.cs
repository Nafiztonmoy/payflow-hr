using PayFlow.Application.Payroll.Models;
using PayFlow.Domain.Entities;
using PayFlow.Domain.Models;

namespace PayFlow.Application.Payroll.Interfaces;

public interface ICompensationResolver
{
    EmployeeCompensation? ResolveEffectiveCompensation(Employee employee, PayrollPeriod period);
}

public interface IAttendanceSummaryService
{
    AttendanceSummary CalculateSummary(Employee employee, IEnumerable<AttendanceRecord> attendanceRecords, IEnumerable<LeaveRequest> approvedLeaves, PayrollPeriod period);
}

public interface ILeaveImpactCalculator
{
    (decimal DeductionAmount, string Explanation) CalculateUnpaidLeaveImpact(decimal baseSalary, int totalWorkingDays, int unpaidLeaveDays);
}

public interface IOvertimeCalculator
{
    (decimal OvertimePay, decimal HourlyRate, string Explanation) CalculateOvertimePay(decimal baseSalary, int totalWorkingDays, decimal standardDailyHours, int overtimeMinutes, decimal overtimeMultiplier = 1.5m);
}

public interface ITaxCalculator
{
    (decimal TaxAmount, string Explanation) CalculateTax(decimal taxableGross, TaxRuleConfig taxConfig);
}

public interface IPayrollCalculator
{
    CalculatedPayrollItemResult CalculateEmployeePayroll(EmployeePayrollContext context);
}
