using PayFlow.Domain.Common;
using PayFlow.Domain.Enums;

namespace PayFlow.Domain.Entities;

public class SalaryStructure : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Version { get; set; } = 1;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;

    // Components configured in this structure
    public virtual ICollection<PayComponent> Components { get; set; } = new List<PayComponent>();
    public virtual ICollection<EmployeeCompensation> Compensations { get; set; } = new List<EmployeeCompensation>();
}

public class PayComponent : BaseEntity
{
    public Guid SalaryStructureId { get; set; }
    public string Code { get; set; } = string.Empty; // e.g., BASIC, HRA, TRANSPORT, MEDICAL, PF, TAX
    public string DisplayName { get; set; } = string.Empty;
    public PayComponentType Type { get; set; } // Earning or Deduction
    public CalculationMode CalculationMode { get; set; } // FixedAmount or PercentageOfBase
    public decimal DefaultAmount { get; set; } = 0m;
    public decimal PercentageRate { get; set; } = 0m; // e.g., 0.10 for 10%
    public bool IsTaxable { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;

    // Navigation
    public virtual SalaryStructure? SalaryStructure { get; set; }
}

public class EmployeeCompensation : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Guid SalaryStructureId { get; set; }
    public decimal BaseSalary { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Remarks { get; set; }

    // Navigation
    public virtual Employee? Employee { get; set; }
    public virtual SalaryStructure? SalaryStructure { get; set; }
}
