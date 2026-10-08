using PayFlow.Domain.Common;
using PayFlow.Domain.Enums;

namespace PayFlow.Domain.Entities;

public class Organization : BaseEntity
{
    public string Name { get; set; } = "Northstar Technologies";
    public string LegalName { get; set; } = "Northstar Technologies Inc.";
    public string TaxIdentifier { get; set; } = "TAX-998811-NT";
    public string Timezone { get; set; } = "UTC";
    public PayrollFrequency PayrollFrequency { get; set; } = PayrollFrequency.Monthly;
    public string Currency { get; set; } = "USD";
    public int WorkingDaysPerWeek { get; set; } = 5;
    public decimal StandardHoursPerDay { get; set; } = 8.0m;
    public string Address { get; set; } = "100 Tech Park, Suite 400";
    public string ContactEmail { get; set; } = "hr@northstar.local";
    public string Phone { get; set; } = "+1 (555) 019-2834";
}

public class Department : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ManagerEmployeeId { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual Employee? Manager { get; set; }
    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

public class Designation : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

public class User : BaseEntity
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Guid? EmployeeId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTimeUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }

    // Navigation
    public virtual Employee? Employee { get; set; }
}
