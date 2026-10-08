using PayFlow.Domain.Common;
using PayFlow.Domain.Enums;

namespace PayFlow.Domain.Entities;

public class TaxRuleSet : BaseEntity
{
    public string JurisdictionLabel { get; set; } = "DEMO-PROGRESSIVE-2026";
    public int Version { get; set; } = 1;
    public string Description { get; set; } = "Configurable progressive tax brackets for demo purposes.";
    public string Disclaimer { get; set; } = "DEMO RULE SET ONLY - NOT AUTHORITATIVE TAX OR LEGAL ADVICE FOR ANY JURISDICTION.";
    public DateTime EffectiveFrom { get; set; } = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public string RulesJson { get; set; } = string.Empty; // Serialized TaxBracketConfig
}

public class PayrollRun : BaseEntity, IConcurrencyAware
{
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public string RunNumber { get; set; } = string.Empty; // e.g. "PR-2026-03-01"
    public PayrollRunStatus Status { get; set; } = PayrollRunStatus.Draft;
    
    public decimal TotalGross { get; set; } = 0m;
    public decimal TotalTax { get; set; } = 0m;
    public decimal TotalDeductions { get; set; } = 0m;
    public decimal TotalNet { get; set; } = 0m;
    public int TotalEmployees { get; set; } = 0;

    public DateTime? CalculatedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public Guid? PaidByUserId { get; set; }

    public string? PaymentReference { get; set; }
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();

    // Navigations
    public virtual ICollection<PayrollItem> Items { get; set; } = new List<PayrollItem>();
    public virtual ICollection<PayrollException> Exceptions { get; set; } = new List<PayrollException>();
}

public class PayrollItem : BaseEntity
{
    public Guid PayrollRunId { get; set; }
    public Guid EmployeeId { get; set; }

    public decimal BaseSalary { get; set; }
    public decimal GrossPay { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal IncomeTax { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }

    public PayrollItemStatus Status { get; set; } = PayrollItemStatus.Ok;
    public string CalculationSnapshotJson { get; set; } = "{}";
    public string CalculationHash { get; set; } = string.Empty;

    // Navigations
    public virtual PayrollRun? PayrollRun { get; set; }
    public virtual Employee? Employee { get; set; }
    public virtual ICollection<PayrollItemComponent> Components { get; set; } = new List<PayrollItemComponent>();
    public virtual ICollection<PayrollException> Exceptions { get; set; } = new List<PayrollException>();
    public virtual Payslip? Payslip { get; set; }
}

public class PayrollItemComponent : BaseEntity
{
    public Guid PayrollItemId { get; set; }
    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public PayComponentType ComponentType { get; set; }
    public decimal Amount { get; set; }
    public string SourceRule { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;

    // Navigation
    public virtual PayrollItem? PayrollItem { get; set; }
}

public class PayrollException : BaseEntity
{
    public Guid PayrollRunId { get; set; }
    public Guid? PayrollItemId { get; set; }
    public Guid EmployeeId { get; set; }

    public ExceptionSeverity Severity { get; set; } = ExceptionSeverity.Warning;
    public string ExceptionType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsResolved { get; set; } = false;
    public DateTime? ResolvedAtUtc { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public string? ResolutionNotes { get; set; }

    // Navigations
    public virtual PayrollRun? PayrollRun { get; set; }
    public virtual PayrollItem? PayrollItem { get; set; }
    public virtual Employee? Employee { get; set; }
}

public class Payslip : BaseEntity
{
    public Guid PayrollItemId { get; set; }
    public Guid EmployeeId { get; set; }
    public string PayslipNumber { get; set; } = string.Empty;
    public DateTime IssueDateUtc { get; set; } = DateTime.UtcNow;
    public string SnapshotJson { get; set; } = "{}";

    // Navigations
    public virtual PayrollItem? PayrollItem { get; set; }
    public virtual Employee? Employee { get; set; }
}

public class AuditLog : BaseEntity
{
    public Guid? ActorId { get; set; }
    public string ActorEmail { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string Summary { get; set; } = string.Empty;
    public string? BeforeValuesJson { get; set; }
    public string? AfterValuesJson { get; set; }
}
