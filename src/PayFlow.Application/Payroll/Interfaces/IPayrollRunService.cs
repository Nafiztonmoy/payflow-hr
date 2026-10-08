using PayFlow.Application.Payroll.Models;
using PayFlow.Domain.Entities;
using PayFlow.Domain.Enums;

namespace PayFlow.Application.Payroll.Interfaces;

public record CreatePayrollRunCommand(int Year, int Month);
public record SubmitForReviewCommand(Guid RunId, Guid ConcurrencyToken);
public record ApprovePayrollRunCommand(Guid RunId, Guid ConcurrencyToken);
public record MarkPaidCommand(Guid RunId, string PaymentReference, DateTime PaidAtUtc, Guid ConcurrencyToken);
public record ResolveExceptionCommand(Guid ExceptionId, string ResolutionNotes);

public record PayrollRunSummaryDto(
    Guid Id,
    int Year,
    int Month,
    string RunNumber,
    PayrollRunStatus Status,
    decimal TotalGross,
    decimal TotalTax,
    decimal TotalDeductions,
    decimal TotalNet,
    int TotalEmployees,
    int WarningCount,
    int BlockingCount,
    DateTime? CalculatedAtUtc,
    DateTime? ReviewedAtUtc,
    DateTime? ApprovedAtUtc,
    DateTime? PaidAtUtc,
    string? PaymentReference,
    Guid ConcurrencyToken
);

public record PayrollItemDetailDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeNo,
    string EmployeeName,
    string DepartmentName,
    string DesignationTitle,
    decimal BaseSalary,
    decimal GrossPay,
    decimal TaxableAmount,
    decimal IncomeTax,
    decimal TotalDeductions,
    decimal NetPay,
    PayrollItemStatus Status,
    string CalculationHash,
    List<PayrollItemComponentDto> Components,
    List<PayrollExceptionDto> Exceptions
);

public record PayrollItemComponentDto(
    string ComponentCode,
    string ComponentName,
    PayComponentType ComponentType,
    decimal Amount,
    string SourceRule,
    string Explanation
);

public record PayrollExceptionDto(
    Guid Id,
    Guid? PayrollItemId,
    Guid EmployeeId,
    string EmployeeName,
    ExceptionSeverity Severity,
    string ExceptionType,
    string Message,
    bool IsResolved,
    DateTime? ResolvedAtUtc,
    string? ResolutionNotes
);

public record PayslipDto(
    Guid PayslipId,
    Guid PayrollItemId,
    string PayslipNumber,
    string OrganizationName,
    string Currency,
    int PeriodYear,
    int PeriodMonth,
    string EmployeeNo,
    string EmployeeName,
    string Department,
    string Designation,
    string PaymentMethod,
    string? MaskedBankAccount,
    decimal BaseSalary,
    decimal GrossPay,
    decimal TaxableAmount,
    decimal IncomeTax,
    decimal TotalDeductions,
    decimal NetPay,
    List<PayrollItemComponentDto> Earnings,
    List<PayrollItemComponentDto> Deductions,
    DateTime IssueDateUtc,
    string CalculationHash
);

public interface IPayrollRunService
{
    Task<PayrollRunSummaryDto> CreateRunAsync(CreatePayrollRunCommand command, CancellationToken ct = default);
    Task<PayrollRunSummaryDto> CalculateRunAsync(Guid runId, CancellationToken ct = default);
    Task<PayrollRunSummaryDto> SubmitForReviewAsync(SubmitForReviewCommand command, CancellationToken ct = default);
    Task<PayrollRunSummaryDto> ApproveRunAsync(ApprovePayrollRunCommand command, CancellationToken ct = default);
    Task<PayrollRunSummaryDto> MarkPaidAsync(MarkPaidCommand command, CancellationToken ct = default);
    Task<PayrollExceptionDto> ResolveExceptionAsync(ResolveExceptionCommand command, CancellationToken ct = default);
    Task<PayrollRunSummaryDto> GetRunByIdAsync(Guid runId, CancellationToken ct = default);
    Task<List<PayrollRunSummaryDto>> GetRunsAsync(CancellationToken ct = default);
    Task<List<PayrollItemDetailDto>> GetRunItemsAsync(Guid runId, CancellationToken ct = default);
    Task<List<PayrollExceptionDto>> GetRunExceptionsAsync(Guid runId, CancellationToken ct = default);
    Task<byte[]> ExportPayrollCsvAsync(Guid runId, CancellationToken ct = default);
    Task<PayslipDto> GetPayslipByItemIdAsync(Guid payrollItemId, CancellationToken ct = default);
}
