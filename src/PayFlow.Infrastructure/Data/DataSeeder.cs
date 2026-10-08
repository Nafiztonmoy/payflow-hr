using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Application.Payroll.Interfaces;
using PayFlow.Application.Payroll.Models;
using PayFlow.Domain.Entities;
using PayFlow.Domain.Enums;
using PayFlow.Domain.Models;

namespace PayFlow.Infrastructure.Data;

public class DataSeeder
{
    private readonly PayFlowDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPayrollCalculator _payrollCalculator;
    private readonly ICompensationResolver _compensationResolver;
    private readonly IAttendanceSummaryService _attendanceSummaryService;

    public DataSeeder(
        PayFlowDbContext context,
        IPasswordHasher passwordHasher,
        IPayrollCalculator payrollCalculator,
        ICompensationResolver compensationResolver,
        IAttendanceSummaryService attendanceSummaryService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _payrollCalculator = payrollCalculator;
        _compensationResolver = compensationResolver;
        _attendanceSummaryService = attendanceSummaryService;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        // Check if already seeded
        if (await _context.Organizations.AnyAsync(ct))
        {
            return;
        }

        // 1. Organization
        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Northstar Technologies",
            LegalName = "Northstar Technologies Enterprise Inc.",
            TaxIdentifier = "TAX-US-NT-2026-01",
            Timezone = "America/New_York",
            PayrollFrequency = PayrollFrequency.Monthly,
            Currency = "USD",
            WorkingDaysPerWeek = 5,
            StandardHoursPerDay = 8.0m,
            Address = "100 Tech Enterprise Blvd, Suite 800, Boston, MA",
            ContactEmail = "hr@northstar.local",
            Phone = "+1 (617) 555-0199",
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.Organizations.Add(org);

        // 2. Departments
        var deptEng = new Department { Id = Guid.NewGuid(), Name = "Engineering", Code = "ENG", Description = "Software and Platform Engineering" };
        var deptPrd = new Department { Id = Guid.NewGuid(), Name = "Product & Design", Code = "PRD", Description = "Product Strategy and User Experience" };
        var deptMkt = new Department { Id = Guid.NewGuid(), Name = "Sales & Marketing", Code = "MKT", Description = "Revenue and Growth Operations" };
        var deptHr = new Department { Id = Guid.NewGuid(), Name = "Human Resources", Code = "HR", Description = "People Operations and Talent" };
        var deptFin = new Department { Id = Guid.NewGuid(), Name = "Finance & Operations", Code = "FIN", Description = "Accounting, Payroll, and Treasury" };

        _context.Departments.AddRange(deptEng, deptPrd, deptMkt, deptHr, deptFin);

        // 3. Designations
        var desCeo = new Designation { Id = Guid.NewGuid(), Title = "Chief Executive Officer", Code = "CEO", Level = 10 };
        var desVpEng = new Designation { Id = Guid.NewGuid(), Title = "VP of Engineering", Code = "VP-ENG", Level = 8 };
        var desEngMgr = new Designation { Id = Guid.NewGuid(), Title = "Engineering Manager", Code = "ENG-MGR", Level = 6 };
        var desSrDev = new Designation { Id = Guid.NewGuid(), Title = "Senior Software Engineer", Code = "SR-DEV", Level = 5 };
        var desDev = new Designation { Id = Guid.NewGuid(), Title = "Software Engineer II", Code = "DEV-2", Level = 4 };
        var desJrDev = new Designation { Id = Guid.NewGuid(), Title = "Software Engineer I", Code = "DEV-1", Level = 3 };

        var desPrdLead = new Designation { Id = Guid.NewGuid(), Title = "Lead Product Manager", Code = "PRD-LD", Level = 6 };
        var desDesigner = new Designation { Id = Guid.NewGuid(), Title = "Senior UI/UX Designer", Code = "DES-SR", Level = 5 };

        var desVpSales = new Designation { Id = Guid.NewGuid(), Title = "VP of Sales", Code = "VP-SALES", Level = 8 };
        var desSalesExec = new Designation { Id = Guid.NewGuid(), Title = "Senior Account Executive", Code = "SALES-SR", Level = 5 };

        var desHrDir = new Designation { Id = Guid.NewGuid(), Title = "HR Director", Code = "HR-DIR", Level = 7 };
        var desHrSpec = new Designation { Id = Guid.NewGuid(), Title = "HR Operations Specialist", Code = "HR-SPEC", Level = 4 };

        var desCfo = new Designation { Id = Guid.NewGuid(), Title = "Chief Financial Officer", Code = "CFO", Level = 9 };
        var desAccountant = new Designation { Id = Guid.NewGuid(), Title = "Payroll Officer / Accountant", Code = "PAY-OFF", Level = 5 };
        var desFinAnalyst = new Designation { Id = Guid.NewGuid(), Title = "Senior Financial Analyst", Code = "FIN-SR", Level = 5 };

        _context.Designations.AddRange(
            desCeo, desVpEng, desEngMgr, desSrDev, desDev, desJrDev,
            desPrdLead, desDesigner, desVpSales, desSalesExec,
            desHrDir, desHrSpec, desCfo, desAccountant, desFinAnalyst
        );

        // 4. Leave Types
        var ltAnnual = new LeaveType { Id = Guid.NewGuid(), Name = "Annual Vacation Leave", Code = "AL", AnnualEntitlementDays = 18, MaxCarryForwardDays = 5, IsPaid = true };
        var ltSick = new LeaveType { Id = Guid.NewGuid(), Name = "Sick & Medical Leave", Code = "SL", AnnualEntitlementDays = 10, MaxCarryForwardDays = 2, IsPaid = true };
        var ltUnpaid = new LeaveType { Id = Guid.NewGuid(), Name = "Unpaid Personal Leave", Code = "UL", AnnualEntitlementDays = 30, MaxCarryForwardDays = 0, IsPaid = false };
        _context.LeaveTypes.AddRange(ltAnnual, ltSick, ltUnpaid);

        // 5. Salary Structures
        var structTech = new SalaryStructure
        {
            Id = Guid.NewGuid(),
            Name = "Engineering Technical Track",
            Code = "TECH-STD",
            Description = "Standard technical compensation structure with allowances and deductions",
            Version = 1,
            EffectiveFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true
        };

        structTech.Components = new List<PayComponent>
        {
            new() { Id = Guid.NewGuid(), SalaryStructureId = structTech.Id, Code = "BASIC", DisplayName = "Base Salary", Type = PayComponentType.Earning, CalculationMode = CalculationMode.FixedAmount, DefaultAmount = 0m, IsTaxable = true, DisplayOrder = 1 },
            new() { Id = Guid.NewGuid(), SalaryStructureId = structTech.Id, Code = "HRA", DisplayName = "Housing Allowance", Type = PayComponentType.Earning, CalculationMode = CalculationMode.PercentageOfBase, PercentageRate = 0.15m, IsTaxable = true, DisplayOrder = 2 },
            new() { Id = Guid.NewGuid(), SalaryStructureId = structTech.Id, Code = "TRANSPORT", DisplayName = "Commuter Allowance", Type = PayComponentType.Earning, CalculationMode = CalculationMode.FixedAmount, DefaultAmount = 250m, IsTaxable = true, DisplayOrder = 3 },
            new() { Id = Guid.NewGuid(), SalaryStructureId = structTech.Id, Code = "MEDICAL", DisplayName = "Health & Wellness Allowance", Type = PayComponentType.Earning, CalculationMode = CalculationMode.FixedAmount, DefaultAmount = 200m, IsTaxable = false, DisplayOrder = 4 },
            new() { Id = Guid.NewGuid(), SalaryStructureId = structTech.Id, Code = "PF", DisplayName = "Provident Fund Contribution", Type = PayComponentType.Deduction, CalculationMode = CalculationMode.PercentageOfBase, PercentageRate = 0.05m, IsTaxable = false, DisplayOrder = 5 }
        };

        var structExec = new SalaryStructure
        {
            Id = Guid.NewGuid(),
            Name = "Leadership & Executive Track",
            Code = "EXEC-LEAD",
            Description = "Executive management compensation structure",
            Version = 1,
            EffectiveFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true
        };

        structExec.Components = new List<PayComponent>
        {
            new() { Id = Guid.NewGuid(), SalaryStructureId = structExec.Id, Code = "BASIC", DisplayName = "Base Salary", Type = PayComponentType.Earning, CalculationMode = CalculationMode.FixedAmount, DefaultAmount = 0m, IsTaxable = true, DisplayOrder = 1 },
            new() { Id = Guid.NewGuid(), SalaryStructureId = structExec.Id, Code = "HRA", DisplayName = "Executive Housing Allowance", Type = PayComponentType.Earning, CalculationMode = CalculationMode.PercentageOfBase, PercentageRate = 0.20m, IsTaxable = true, DisplayOrder = 2 },
            new() { Id = Guid.NewGuid(), SalaryStructureId = structExec.Id, Code = "TRANSPORT", DisplayName = "Executive Car Allowance", Type = PayComponentType.Earning, CalculationMode = CalculationMode.FixedAmount, DefaultAmount = 500m, IsTaxable = true, DisplayOrder = 3 },
            new() { Id = Guid.NewGuid(), SalaryStructureId = structExec.Id, Code = "PF", DisplayName = "Provident Fund Contribution", Type = PayComponentType.Deduction, CalculationMode = CalculationMode.PercentageOfBase, PercentageRate = 0.06m, IsTaxable = false, DisplayOrder = 4 }
        };

        _context.SalaryStructures.AddRange(structTech, structExec);

        // 6. TaxRuleSet
        var demoTaxConfig = new TaxRuleConfig
        {
            StandardDeduction = 0m,
            Brackets = new List<TaxBracket>
            {
                new() { LowerBound = 0m, UpperBound = 3000m, Rate = 0.00m, Description = "Tier 1: $0 - $3,000 (0% Tax-Free Allowance)" },
                new() { LowerBound = 3000m, UpperBound = 6000m, Rate = 0.10m, Description = "Tier 2: $3,000 - $6,000 (10% Progressive Bracket)" },
                new() { LowerBound = 6000m, UpperBound = 10000m, Rate = 0.15m, Description = "Tier 3: $6,000 - $10,000 (15% Progressive Bracket)" },
                new() { LowerBound = 10000m, UpperBound = null, Rate = 0.20m, Description = "Tier 4: Over $10,000 (20% Top Bracket)" }
            }
        };

        var taxRuleSet = new TaxRuleSet
        {
            Id = Guid.NewGuid(),
            JurisdictionLabel = "DEMO-PROGRESSIVE-2026",
            Version = 1,
            Description = "Demo progressive tax brackets for showcase and testing purposes.",
            Disclaimer = "DEMO RULE SET ONLY - NOT AUTHORITATIVE TAX OR LEGAL ADVICE FOR ANY JURISDICTION.",
            EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true,
            RulesJson = JsonSerializer.Serialize(demoTaxConfig)
        };
        _context.TaxRuleSets.Add(taxRuleSet);

        // 7. Seed Employees
        // Level 1: Leadership
        var empCeo = CreateEmployee("EMP-EXEC-001", "Marcus", "Vance", "marcus.vance@northstar.local", deptFin.Id, desCeo.Id, null, 14000m, structExec.Id, new DateTime(2022, 1, 1));
        var empCfo = CreateEmployee("EMP-FIN-001", "Jonathan", "Crane", "jonathan.crane@northstar.local", deptFin.Id, desCfo.Id, empCeo.Id, 12000m, structExec.Id, new DateTime(2022, 3, 1));
        var empVpEng = CreateEmployee("EMP-ENG-000", "Sarah", "Lin", "sarah.lin@northstar.local", deptEng.Id, desVpEng.Id, empCeo.Id, 11500m, structExec.Id, new DateTime(2022, 6, 1));
        var empHrDir = CreateEmployee("EMP-HR-001", "Rachel", "Green", "hr@northstar.local", deptHr.Id, desHrDir.Id, empCeo.Id, 9500m, structExec.Id, new DateTime(2023, 1, 1));
        var empVpSales = CreateEmployee("EMP-MKT-001", "Michael", "Scott", "michael.scott@northstar.local", deptMkt.Id, desVpSales.Id, empCeo.Id, 10500m, structExec.Id, new DateTime(2022, 5, 1));

        // Level 2: Managers
        var empEngMgr = CreateEmployee("EMP-ENG-MGR", "David", "Miller", "manager@northstar.local", deptEng.Id, desEngMgr.Id, empVpEng.Id, 8500m, structTech.Id, new DateTime(2023, 2, 1));
        var empPrdLead = CreateEmployee("EMP-PRD-001", "Olivia", "Taylor", "olivia.taylor@northstar.local", deptPrd.Id, desPrdLead.Id, empCeo.Id, 8000m, structTech.Id, new DateTime(2023, 3, 1));

        // Level 3: Specialists & Team Members
        // Direct report of Manager David Miller: Alex Carter (Employee persona)
        var empAlex = CreateEmployee("EMP-ENG-001", "Alex", "Carter", "employee@northstar.local", deptEng.Id, desSrDev.Id, empEngMgr.Id, 6500m, structTech.Id, new DateTime(2024, 1, 15));
        var empEmily = CreateEmployee("EMP-ENG-002", "Emily", "Watson", "emily.watson@northstar.local", deptEng.Id, desDev.Id, empEngMgr.Id, 5500m, structTech.Id, new DateTime(2024, 4, 1));
        
        // Fixable blocking employee: James Liu has BankTransfer but missing account number
        var empJames = CreateEmployee("EMP-ENG-003", "James", "Liu", "james.liu@northstar.local", deptEng.Id, desJrDev.Id, empEngMgr.Id, 4200m, structTech.Id, new DateTime(2024, 8, 1));
        empJames.BankAccountNumber = null; // INTENTIONAL BLOCKING EXCEPTION! Missing bank account for BankTransfer
        empJames.BankRoutingNumber = null;

        var empSophie = CreateEmployee("EMP-ENG-004", "Sophie", "Turner", "sophie.turner@northstar.local", deptEng.Id, desSrDev.Id, empEngMgr.Id, 6200m, structTech.Id, new DateTime(2024, 2, 1));
        var empLucas = CreateEmployee("EMP-ENG-005", "Lucas", "Scott", "lucas.scott@northstar.local", deptEng.Id, desDev.Id, empEngMgr.Id, 5200m, structTech.Id, new DateTime(2024, 9, 1));

        // Design Team
        var empDesigner1 = CreateEmployee("EMP-PRD-002", "Liam", "Brown", "liam.brown@northstar.local", deptPrd.Id, desDesigner.Id, empPrdLead.Id, 5800m, structTech.Id, new DateTime(2023, 8, 1));
        var empDesigner2 = CreateEmployee("EMP-PRD-003", "Chloe", "Bennett", "chloe.bennett@northstar.local", deptPrd.Id, desDesigner.Id, empPrdLead.Id, 5600m, structTech.Id, new DateTime(2024, 3, 1));

        // Sales Team
        var empSales1 = CreateEmployee("EMP-MKT-002", "Jim", "Halpert", "jim.halpert@northstar.local", deptMkt.Id, desSalesExec.Id, empVpSales.Id, 6000m, structTech.Id, new DateTime(2023, 4, 1));
        var empSales2 = CreateEmployee("EMP-MKT-003", "Dwight", "Schrute", "dwight.schrute@northstar.local", deptMkt.Id, desSalesExec.Id, empVpSales.Id, 6100m, structTech.Id, new DateTime(2023, 5, 1));
        var empSales3 = CreateEmployee("EMP-MKT-004", "Pam", "Beesly", "pam.beesly@northstar.local", deptMkt.Id, desSalesExec.Id, empVpSales.Id, 5400m, structTech.Id, new DateTime(2023, 6, 1));

        // HR Team
        var empHrSpec = CreateEmployee("EMP-HR-002", "Noah", "Davis", "noah.davis@northstar.local", deptHr.Id, desHrSpec.Id, empHrDir.Id, 4800m, structTech.Id, new DateTime(2024, 5, 1));
        var empHrCoord = CreateEmployee("EMP-HR-003", "Mia", "Johnson", "mia.johnson@northstar.local", deptHr.Id, desHrSpec.Id, empHrDir.Id, 4600m, structTech.Id, new DateTime(2024, 6, 1));

        // Finance Team
        var empAccountant = CreateEmployee("EMP-FIN-002", "Angela", "Martin", "accountant@northstar.local", deptFin.Id, desAccountant.Id, empCfo.Id, 6200m, structTech.Id, new DateTime(2023, 3, 1));
        var empFinAnalyst = CreateEmployee("EMP-FIN-003", "Oscar", "Martinez", "oscar.martinez@northstar.local", deptFin.Id, desFinAnalyst.Id, empCfo.Id, 5900m, structTech.Id, new DateTime(2023, 7, 1));
        var empFinAssoc = CreateEmployee("EMP-FIN-004", "Kevin", "Malone", "kevin.malone@northstar.local", deptFin.Id, desFinAnalyst.Id, empAccountant.Id, 4500m, structTech.Id, new DateTime(2024, 2, 1));

        // Additional employees to reach 23-25 realistic employees
        var empDev6 = CreateEmployee("EMP-ENG-006", "Ethan", "Hunt", "ethan.hunt@northstar.local", deptEng.Id, desDev.Id, empEngMgr.Id, 5300m, structTech.Id, new DateTime(2024, 10, 1));
        var empDev7 = CreateEmployee("EMP-ENG-007", "Grace", "Hopper", "grace.hopper@northstar.local", deptEng.Id, desSrDev.Id, empEngMgr.Id, 7000m, structTech.Id, new DateTime(2023, 11, 1));
        var empSales4 = CreateEmployee("EMP-MKT-005", "Andy", "Bernard", "andy.bernard@northstar.local", deptMkt.Id, desSalesExec.Id, empVpSales.Id, 5100m, structTech.Id, new DateTime(2024, 4, 15));
        var empSales5 = CreateEmployee("EMP-MKT-006", "Stanley", "Hudson", "stanley.hudson@northstar.local", deptMkt.Id, desSalesExec.Id, empVpSales.Id, 5800m, structTech.Id, new DateTime(2023, 1, 10));

        var allEmployees = new List<Employee>
        {
            empCeo, empCfo, empVpEng, empHrDir, empVpSales,
            empEngMgr, empPrdLead,
            empAlex, empEmily, empJames, empSophie, empLucas, empDev6, empDev7,
            empDesigner1, empDesigner2,
            empSales1, empSales2, empSales3, empSales4, empSales5,
            empHrSpec, empHrCoord,
            empAccountant, empFinAnalyst, empFinAssoc
        };

        _context.Employees.AddRange(allEmployees);
        await _context.SaveChangesAsync(ct);

        // Update department managers now that employees and departments exist
        deptEng.ManagerEmployeeId = empVpEng.Id;
        deptPrd.ManagerEmployeeId = empPrdLead.Id;
        deptMkt.ManagerEmployeeId = empVpSales.Id;
        deptHr.ManagerEmployeeId = empHrDir.Id;
        deptFin.ManagerEmployeeId = empCfo.Id;
        await _context.SaveChangesAsync(ct);

        // 8. Seed 5 Login Personas
        var userAdmin = new User
        {
            Id = Guid.NewGuid(),
            UserName = "admin",
            Email = "admin@northstar.local",
            PasswordHash = _passwordHasher.HashPassword("Admin@PayFlow2026!"),
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var userHr = new User
        {
            Id = Guid.NewGuid(),
            UserName = "hr",
            Email = "hr@northstar.local",
            PasswordHash = _passwordHasher.HashPassword("Hr@PayFlow2026!"),
            Role = UserRole.HR,
            EmployeeId = empHrDir.Id,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var userManager = new User
        {
            Id = Guid.NewGuid(),
            UserName = "manager",
            Email = "manager@northstar.local",
            PasswordHash = _passwordHasher.HashPassword("Manager@PayFlow2026!"),
            Role = UserRole.Manager,
            EmployeeId = empEngMgr.Id, // Manager of Alex, Emily, James, Sophie, Lucas
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var userAccountant = new User
        {
            Id = Guid.NewGuid(),
            UserName = "accountant",
            Email = "accountant@northstar.local",
            PasswordHash = _passwordHasher.HashPassword("Accountant@PayFlow2026!"),
            Role = UserRole.Accountant,
            EmployeeId = empAccountant.Id,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var userEmployee = new User
        {
            Id = Guid.NewGuid(),
            UserName = "employee",
            Email = "employee@northstar.local",
            PasswordHash = _passwordHasher.HashPassword("Employee@PayFlow2026!"),
            Role = UserRole.Employee,
            EmployeeId = empAlex.Id, // Alex Carter, reports to David Miller
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Users.AddRange(userAdmin, userHr, userManager, userAccountant, userEmployee);

        // 9. Leave Balances for 2026
        foreach (var emp in allEmployees)
        {
            _context.LeaveBalances.Add(new LeaveBalance
            {
                Id = Guid.NewGuid(),
                EmployeeId = emp.Id,
                LeaveTypeId = ltAnnual.Id,
                Year = 2026,
                OpeningBalance = 18m,
                AccruedDays = 0m,
                UsedDays = emp == empAlex ? 2m : 0m,
                AdjustedDays = 0m
            });

            _context.LeaveBalances.Add(new LeaveBalance
            {
                Id = Guid.NewGuid(),
                EmployeeId = emp.Id,
                LeaveTypeId = ltSick.Id,
                Year = 2026,
                OpeningBalance = 10m,
                AccruedDays = 0m,
                UsedDays = 0m,
                AdjustedDays = 0m
            });

            _context.LeaveBalances.Add(new LeaveBalance
            {
                Id = Guid.NewGuid(),
                EmployeeId = emp.Id,
                LeaveTypeId = ltUnpaid.Id,
                Year = 2026,
                OpeningBalance = 30m,
                AccruedDays = 0m,
                UsedDays = emp == empEmily ? 2m : 0m,
                AdjustedDays = 0m
            });
        }

        // 10. Leave Requests
        // Pending request for Alex Carter -> David Miller will see it in Manager Inbox!
        var reqAlex = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = empAlex.Id,
            LeaveTypeId = ltAnnual.Id,
            StartDate = new DateOnly(2026, 3, 23),
            EndDate = new DateOnly(2026, 3, 24),
            DayCount = 2,
            Reason = "Family personal commitment.",
            Status = LeaveRequestStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
        };

        // Emily Watson had approved unpaid leave in March (triggers unpaid leave deduction warning!)
        var reqEmily = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = empEmily.Id,
            LeaveTypeId = ltUnpaid.Id,
            StartDate = new DateOnly(2026, 3, 10),
            EndDate = new DateOnly(2026, 3, 11),
            DayCount = 2,
            Reason = "Personal urgent travel.",
            Status = LeaveRequestStatus.Approved,
            ReviewedByUserId = userManager.Id,
            ReviewedAtUtc = DateTime.UtcNow.AddDays(-15),
            ReviewRemarks = "Approved by manager."
        };

        // Another employee approved vacation
        var reqSophie = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = empSophie.Id,
            LeaveTypeId = ltAnnual.Id,
            StartDate = new DateOnly(2026, 3, 5),
            EndDate = new DateOnly(2026, 3, 6),
            DayCount = 2,
            Reason = "Vacation day off.",
            Status = LeaveRequestStatus.Approved,
            ReviewedByUserId = userManager.Id,
            ReviewedAtUtc = DateTime.UtcNow.AddDays(-20),
            ReviewRemarks = "Approved."
        };

        _context.LeaveRequests.AddRange(reqAlex, reqEmily, reqSophie);

        // 11. Attendance Records for March 2026
        var marchStart = new DateOnly(2026, 3, 1);
        var marchEnd = new DateOnly(2026, 3, 31);

        for (var date = marchStart; date <= marchEnd; date = date.AddDays(1))
        {
            bool isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;

            foreach (var emp in allEmployees)
            {
                if (isWeekend)
                {
                    _context.AttendanceRecords.Add(new AttendanceRecord
                    {
                        Id = Guid.NewGuid(),
                        EmployeeId = emp.Id,
                        Date = date,
                        Status = AttendanceStatus.Weekend,
                        WorkedMinutes = 0,
                        OvertimeMinutes = 0,
                        Source = AttendanceSource.SelfService
                    });
                }
                else
                {
                    // Check if on leave
                    if (emp == empEmily && date >= new DateOnly(2026, 3, 10) && date <= new DateOnly(2026, 3, 11))
                    {
                        _context.AttendanceRecords.Add(new AttendanceRecord
                        {
                            Id = Guid.NewGuid(),
                            EmployeeId = emp.Id,
                            Date = date,
                            Status = AttendanceStatus.OnLeave,
                            WorkedMinutes = 0,
                            OvertimeMinutes = 0,
                            Source = AttendanceSource.SelfService
                        });
                        continue;
                    }

                    int workedMins = 480; // 8 hours
                    int otMins = 0;

                    // Overtime for Alex (e.g. 120 mins on March 18)
                    if (emp == empAlex && date == new DateOnly(2026, 3, 18))
                    {
                        otMins = 120;
                    }

                    // High overtime for Sophie to trigger warning (> 40h overtime total across month)
                    if (emp == empSophie && date.Day <= 10)
                    {
                        otMins = 250; // Total > 2500 mins
                    }

                    _context.AttendanceRecords.Add(new AttendanceRecord
                    {
                        Id = Guid.NewGuid(),
                        EmployeeId = emp.Id,
                        Date = date,
                        Status = AttendanceStatus.Present,
                        CheckInTimeUtc = new DateTime(date.Year, date.Month, date.Day, 9, 0, 0, DateTimeKind.Utc),
                        CheckOutTimeUtc = new DateTime(date.Year, date.Month, date.Day, 17, 0, 0, DateTimeKind.Utc).AddMinutes(otMins),
                        WorkedMinutes = workedMins,
                        OvertimeMinutes = otMins,
                        Source = AttendanceSource.Biometric
                    });
                }
            }
        }

        await _context.SaveChangesAsync(ct);

        // 12. Seed Historical Paid Payroll Run (February 2026)
        var febStart = new DateOnly(2026, 2, 1);
        var febEnd = new DateOnly(2026, 2, 28);
        var febRun = new PayrollRun
        {
            Id = Guid.NewGuid(),
            PeriodYear = 2026,
            PeriodMonth = 2,
            PeriodStart = febStart,
            PeriodEnd = febEnd,
            RunNumber = "PR-2026-02-01",
            Status = PayrollRunStatus.Paid,
            CalculatedAtUtc = new DateTime(2026, 2, 26, 10, 0, 0, DateTimeKind.Utc),
            ReviewedAtUtc = new DateTime(2026, 2, 27, 9, 0, 0, DateTimeKind.Utc),
            ApprovedAtUtc = new DateTime(2026, 2, 27, 14, 0, 0, DateTimeKind.Utc),
            PaidAtUtc = new DateTime(2026, 2, 28, 16, 0, 0, DateTimeKind.Utc),
            PaymentReference = "ACH-FEDWIRE-20260228-NT9988",
            CreatedByUserId = userAccountant.Id,
            ReviewedByUserId = userAccountant.Id,
            ApprovedByUserId = userAdmin.Id,
            PaidByUserId = userAccountant.Id,
            TotalGross = 168450.00m,
            TotalTax = 14220.00m,
            TotalDeductions = 23980.00m,
            TotalNet = 144470.00m,
            TotalEmployees = allEmployees.Count,
            ConcurrencyToken = Guid.NewGuid()
        };
        _context.PayrollRuns.Add(febRun);

        // Add items & payslips for February
        foreach (var emp in allEmployees)
        {
            var comp = emp.Compensations.First();
            decimal baseSal = comp.BaseSalary;
            decimal hra = RoundingPolicy.RoundCurrency(baseSal * 0.15m);
            decimal gross = baseSal + hra + 450m;
            decimal pf = RoundingPolicy.RoundCurrency(baseSal * 0.05m);
            decimal tax = RoundingPolicy.RoundCurrency(gross > 3000 ? (gross - 3000) * 0.10m : 0m);
            decimal deductions = pf + tax;
            decimal net = gross - deductions;

            var item = new PayrollItem
            {
                Id = Guid.NewGuid(),
                PayrollRunId = febRun.Id,
                EmployeeId = emp.Id,
                BaseSalary = baseSal,
                GrossPay = gross,
                TaxableAmount = gross,
                IncomeTax = tax,
                TotalDeductions = deductions,
                NetPay = net,
                Status = PayrollItemStatus.Ok,
                CalculationSnapshotJson = JsonSerializer.Serialize(new { Employee = emp.FullName, Net = net }),
                CalculationHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(emp.Id.ToString() + gross.ToString()))),
                CreatedAtUtc = new DateTime(2026, 2, 26, 10, 0, 0, DateTimeKind.Utc)
            };

            item.Components.Add(new PayrollItemComponent { Id = Guid.NewGuid(), PayrollItemId = item.Id, ComponentCode = "BASIC", ComponentName = "Base Salary", ComponentType = PayComponentType.Earning, Amount = baseSal, SourceRule = "Contract", Explanation = "Base" });
            item.Components.Add(new PayrollItemComponent { Id = Guid.NewGuid(), PayrollItemId = item.Id, ComponentCode = "HRA", ComponentName = "Housing Allowance", ComponentType = PayComponentType.Earning, Amount = hra, SourceRule = "Structure", Explanation = "15% of Base" });
            item.Components.Add(new PayrollItemComponent { Id = Guid.NewGuid(), PayrollItemId = item.Id, ComponentCode = "PF", ComponentName = "Provident Fund", ComponentType = PayComponentType.Deduction, Amount = pf, SourceRule = "Structure", Explanation = "5% of Base" });
            item.Components.Add(new PayrollItemComponent { Id = Guid.NewGuid(), PayrollItemId = item.Id, ComponentCode = "INCOME_TAX", ComponentName = "Income Tax", ComponentType = PayComponentType.Deduction, Amount = tax, SourceRule = "Demo Brackets", Explanation = "Demo progressive" });

            _context.PayrollItems.Add(item);

            // Generated Payslip for February
            _context.Payslips.Add(new Payslip
            {
                Id = Guid.NewGuid(),
                PayrollItemId = item.Id,
                EmployeeId = emp.Id,
                PayslipNumber = $"PS-2026-02-{item.Id.ToString()[..8].ToUpper()}",
                IssueDateUtc = new DateTime(2026, 2, 28, 16, 0, 0, DateTimeKind.Utc),
                SnapshotJson = item.CalculationSnapshotJson
            });
        }

        // 13. Seed Current Active Payroll Run (March 2026) in Calculated status
        // Create run and trigger calculation so it has real items and seeded exceptions
        var marchRun = new PayrollRun
        {
            Id = Guid.NewGuid(),
            PeriodYear = 2026,
            PeriodMonth = 3,
            PeriodStart = marchStart,
            PeriodEnd = marchEnd,
            RunNumber = "PR-2026-03-01",
            Status = PayrollRunStatus.Draft,
            CreatedByUserId = userAccountant.Id,
            CreatedAtUtc = DateTime.UtcNow,
            ConcurrencyToken = Guid.NewGuid()
        };
        _context.PayrollRuns.Add(marchRun);
        await _context.SaveChangesAsync(ct);

        // Now calculate March run using the real calculation engine!
        // This will naturally detect:
        // - James Liu missing bank account (BLOCKING EXCEPTION)
        // - Emily Watson unpaid leave deduction (WARNING EXCEPTION)
        // - Sophie Turner high overtime (WARNING EXCEPTION)
        var period = new PayrollPeriod(2026, 3, marchStart, marchEnd, 22);

        decimal mGross = 0;
        decimal mTax = 0;
        decimal mDeductions = 0;
        decimal mNet = 0;

        foreach (var emp in allEmployees)
        {
            var comp = emp.Compensations.First();
            var structure = emp.Compensations.First().SalaryStructure;
            var summary = _attendanceSummaryService.CalculateSummary(emp, emp.AttendanceRecords, emp.LeaveRequests, period);

            var ctx = new EmployeePayrollContext(
                Employee: emp,
                ActiveCompensation: comp,
                SalaryStructure: structure,
                Components: structure?.Components.ToList() ?? new List<PayComponent>(),
                AttendanceSummary: summary,
                TaxConfig: demoTaxConfig,
                Period: period
            );

            var calc = _payrollCalculator.CalculateEmployeePayroll(ctx);

            var item = new PayrollItem
            {
                Id = Guid.NewGuid(),
                PayrollRunId = marchRun.Id,
                EmployeeId = emp.Id,
                BaseSalary = calc.BaseSalary,
                GrossPay = calc.GrossPay,
                TaxableAmount = calc.TaxableAmount,
                IncomeTax = calc.IncomeTax,
                TotalDeductions = calc.TotalDeductions,
                NetPay = calc.NetPay,
                Status = calc.Status,
                CalculationSnapshotJson = calc.SnapshotJson,
                CalculationHash = calc.CalculationHash,
                CreatedAtUtc = DateTime.UtcNow
            };

            foreach (var c in calc.Components)
            {
                item.Components.Add(new PayrollItemComponent
                {
                    Id = Guid.NewGuid(),
                    PayrollItemId = item.Id,
                    ComponentCode = c.Code,
                    ComponentName = c.DisplayName,
                    ComponentType = c.Type,
                    Amount = c.Amount,
                    SourceRule = c.SourceRule,
                    Explanation = c.Explanation
                });
            }

            foreach (var ex in calc.Exceptions)
            {
                _context.PayrollExceptions.Add(new PayrollException
                {
                    Id = Guid.NewGuid(),
                    PayrollRunId = marchRun.Id,
                    PayrollItemId = item.Id,
                    EmployeeId = emp.Id,
                    Severity = ex.Severity,
                    ExceptionType = ex.ExceptionType,
                    Message = ex.Message,
                    IsResolved = false
                });
            }

            _context.PayrollItems.Add(item);

            mGross += calc.GrossPay;
            mTax += calc.IncomeTax;
            mDeductions += calc.TotalDeductions;
            mNet += calc.NetPay;
        }

        marchRun.TotalGross = RoundingPolicy.RoundCurrency(mGross);
        marchRun.TotalTax = RoundingPolicy.RoundCurrency(mTax);
        marchRun.TotalDeductions = RoundingPolicy.RoundCurrency(mDeductions);
        marchRun.TotalNet = RoundingPolicy.RoundCurrency(mNet);
        marchRun.TotalEmployees = allEmployees.Count;
        marchRun.Status = PayrollRunStatus.Calculated;
        marchRun.CalculatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
    }

    private static Employee CreateEmployee(
        string no, string first, string last, string email,
        Guid deptId, Guid desId, Guid? mgrId, decimal baseSalary,
        Guid structId, DateTime joinDate)
    {
        var emp = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNo = no,
            FirstName = first,
            LastName = last,
            WorkEmail = email,
            Phone = $"+1 (555) {Random.Shared.Next(100, 999)}-{Random.Shared.Next(1000, 9999)}",
            DateOfBirth = new DateTime(1990, 5, 15),
            JoinDate = joinDate,
            Status = EmploymentStatus.Active,
            DepartmentId = deptId,
            DesignationId = desId,
            ManagerId = mgrId,
            PaymentMethod = PaymentMethod.BankTransfer,
            BankName = "JPMorgan Chase Bank",
            BankAccountNumber = $"88{Random.Shared.Next(10000000, 99999999)}",
            BankRoutingNumber = "021000021",
            CreatedAtUtc = DateTime.UtcNow
        };

        var comp = new EmployeeCompensation
        {
            Id = Guid.NewGuid(),
            EmployeeId = emp.Id,
            SalaryStructureId = structId,
            BaseSalary = baseSalary,
            EffectiveFrom = joinDate,
            EffectiveTo = null,
            IsActive = true,
            Remarks = "Standard base compensation assignment",
            CreatedAtUtc = DateTime.UtcNow
        };

        emp.Compensations.Add(comp);
        return emp;
    }
}
