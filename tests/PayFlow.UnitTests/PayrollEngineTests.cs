using FluentAssertions;
using PayFlow.Application.Payroll.Models;
using PayFlow.Application.Payroll.Services;
using PayFlow.Domain.Entities;
using PayFlow.Domain.Enums;
using PayFlow.Domain.Models;
using Xunit;

namespace PayFlow.UnitTests;

public class PayrollEngineTests
{
    private readonly LeaveImpactCalculator _leaveImpactCalculator = new();
    private readonly OvertimeCalculator _overtimeCalculator = new();
    private readonly TaxCalculator _taxCalculator = new();
    private readonly PayrollCalculator _payrollCalculator;

    public PayrollEngineTests()
    {
        _payrollCalculator = new PayrollCalculator(_leaveImpactCalculator, _overtimeCalculator, _taxCalculator);
    }

    [Fact]
    public void LeaveImpact_WithZeroUnpaidLeave_ReturnsZeroDeduction()
    {
        var (deduction, explanation) = _leaveImpactCalculator.CalculateUnpaidLeaveImpact(5000m, 22, 0);

        deduction.Should().Be(0m);
        explanation.Should().Contain("No unpaid leave");
    }

    [Theory]
    [InlineData(5000, 20, 2, 500)]      // 5000 / 20 = 250/day * 2 = 500
    [InlineData(3300, 22, 1, 150)]      // 3300 / 22 = 150/day * 1 = 150
    [InlineData(4500, 21, 3, 642.87)]   // 4500 / 21 = 214.2857 -> 214.29 * 3 = 642.87
    public void LeaveImpact_CalculatesDailyRateAndRoundsCorrectly(decimal baseSalary, int workingDays, int unpaidDays, decimal expectedDeduction)
    {
        var (deduction, _) = _leaveImpactCalculator.CalculateUnpaidLeaveImpact(baseSalary, workingDays, unpaidDays);

        deduction.Should().Be(expectedDeduction);
    }

    [Fact]
    public void Overtime_WithZeroMinutes_ReturnsZeroPay()
    {
        var (otPay, hourlyRate, _) = _overtimeCalculator.CalculateOvertimePay(5000m, 22, 8.0m, 0);

        otPay.Should().Be(0m);
        hourlyRate.Should().Be(0m);
    }

    [Fact]
    public void Overtime_WithApprovedMinutes_CalculatesCorrectPayWithMultiplier()
    {
        // 22 days * 8h = 176h.
        // Base 5280 -> 5280 / 176 = 30.00/hr.
        // 120 minutes = 2 hours.
        // Multiplier 1.5 -> 2 * 30.00 * 1.5 = 90.00
        var (otPay, hourlyRate, explanation) = _overtimeCalculator.CalculateOvertimePay(5280m, 22, 8.0m, 120, 1.5m);

        hourlyRate.Should().Be(30.00m);
        otPay.Should().Be(90.00m);
        explanation.Should().Contain("Overtime: 2.00 hrs");
    }

    [Fact]
    public void TaxCalculator_WithZeroOrNegativeTaxableGross_ReturnsZeroTax()
    {
        var config = CreateDemoTaxConfig();
        var (tax, _) = _taxCalculator.CalculateTax(0m, config);
        tax.Should().Be(0m);

        var (negTax, _) = _taxCalculator.CalculateTax(-500m, config);
        negTax.Should().Be(0m);
    }

    [Fact]
    public void TaxCalculator_StandardDeductionOffsetsTaxableAmount()
    {
        var config = CreateDemoTaxConfig(standardDeduction: 1000m);
        // Taxable 1000 with 1000 standard deduction leaves 0 net taxable
        var (tax, _) = _taxCalculator.CalculateTax(1000m, config);
        tax.Should().Be(0m);
    }

    [Theory]
    [InlineData(2000, 0)]          // Bracket 1: 0 - 3,000 @ 0% -> 0
    [InlineData(3000, 0)]          // Exact boundary of bracket 1 -> 0
    [InlineData(4000, 100)]        // 3000 @ 0% + 1000 @ 10% -> 100
    [InlineData(6000, 300)]        // 3000 @ 0% + 3000 @ 10% -> 300
    [InlineData(8000, 600)]        // 300 + (2000 @ 15%) = 300 + 300 = 600
    [InlineData(12000, 1300)]      // 300 + 600 (4000@15%) + 400 (2000@20%) = 1300
    public void TaxCalculator_ProgressiveBrackets_CalculateAccurately(decimal taxableGross, decimal expectedTax)
    {
        var config = CreateDemoTaxConfig(standardDeduction: 0m);
        var (tax, _) = _taxCalculator.CalculateTax(taxableGross, config);
        tax.Should().Be(expectedTax);
    }

    [Fact]
    public void PayrollCalculator_MissingCompensation_ProducesBlockingException()
    {
        var employee = CreateEmployee("EMP001", "John", "Doe");
        var context = new EmployeePayrollContext(
            Employee: employee,
            ActiveCompensation: null,
            SalaryStructure: null,
            Components: new List<PayComponent>(),
            AttendanceSummary: new AttendanceSummary(22, 22, 0, 0, 0, 0, false),
            TaxConfig: CreateDemoTaxConfig(),
            Period: new PayrollPeriod(2026, 3, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), 22)
        );

        var result = _payrollCalculator.CalculateEmployeePayroll(context);

        result.Status.Should().Be(PayrollItemStatus.HasBlockingExceptions);
        result.Exceptions.Should().Contain(e => e.Severity == ExceptionSeverity.Blocking && e.ExceptionType == "MISSING_COMPENSATION");
    }

    [Fact]
    public void PayrollCalculator_MissingBankDetailsForBankTransfer_ProducesBlockingException()
    {
        var employee = CreateEmployee("EMP002", "Alice", "Smith");
        employee.PaymentMethod = PaymentMethod.BankTransfer;
        employee.BankAccountNumber = null; // Missing

        var comp = new EmployeeCompensation { BaseSalary = 6000m, EffectiveFrom = new DateTime(2026, 1, 1) };
        var context = new EmployeePayrollContext(
            Employee: employee,
            ActiveCompensation: comp,
            SalaryStructure: null,
            Components: new List<PayComponent>(),
            AttendanceSummary: new AttendanceSummary(22, 22, 0, 0, 0, 0, false),
            TaxConfig: CreateDemoTaxConfig(),
            Period: new PayrollPeriod(2026, 3, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), 22)
        );

        var result = _payrollCalculator.CalculateEmployeePayroll(context);

        result.Status.Should().Be(PayrollItemStatus.HasBlockingExceptions);
        result.Exceptions.Should().Contain(e => e.Severity == ExceptionSeverity.Blocking && e.ExceptionType == "MISSING_BANK_ACCOUNT");
    }

    [Fact]
    public void PayrollCalculator_ValidEmployee_ComputesEarningsDeductionsAndNet()
    {
        var employee = CreateEmployee("EMP003", "Bob", "Taylor");
        employee.BankAccountNumber = "123456789";
        employee.BankRoutingNumber = "987654321";

        var comp = new EmployeeCompensation { BaseSalary = 5000m, EffectiveFrom = new DateTime(2026, 1, 1) };

        // 10% HRA Earning, 5% Provident Fund Deduction
        var components = new List<PayComponent>
        {
            new() { Code = "HRA", DisplayName = "House Rent Allowance", Type = PayComponentType.Earning, CalculationMode = CalculationMode.PercentageOfBase, PercentageRate = 0.10m, IsTaxable = true, IsActive = true },
            new() { Code = "PF", DisplayName = "Provident Fund", Type = PayComponentType.Deduction, CalculationMode = CalculationMode.PercentageOfBase, PercentageRate = 0.05m, IsTaxable = false, IsActive = true }
        };

        var context = new EmployeePayrollContext(
            Employee: employee,
            ActiveCompensation: comp,
            SalaryStructure: new SalaryStructure { Name = "Standard" },
            Components: components,
            AttendanceSummary: new AttendanceSummary(22, 22, 0, 0, 0, 120, false), // 2 hrs OT
            TaxConfig: CreateDemoTaxConfig(standardDeduction: 0m),
            Period: new PayrollPeriod(2026, 3, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), 22)
        );

        var result = _payrollCalculator.CalculateEmployeePayroll(context);

        // Basic: 5000
        // HRA: 500
        // OT: 2h @ (5000/176 = 28.41/h) * 1.5 = 85.23
        // Gross: 5000 + 500 + 85.23 = 5585.23
        // Taxable: 5585.23
        // Tax: 3000 @ 0% + 2585.23 @ 10% = 258.52
        // PF: 5000 * 0.05 = 250.00
        // Total Deductions: 258.52 + 250 = 508.52
        // Net: 5585.23 - 508.52 = 5076.71
        result.Status.Should().Be(PayrollItemStatus.Ok);
        result.BaseSalary.Should().Be(5000m);
        result.GrossPay.Should().Be(5585.23m);
        result.IncomeTax.Should().Be(258.52m);
        result.TotalDeductions.Should().Be(508.52m);
        result.NetPay.Should().Be(5076.71m);

        result.CalculationHash.Should().NotBeNullOrWhiteSpace();
        result.SnapshotJson.Should().Contain("EMP003");
    }

    [Fact]
    public void PayrollCalculator_MidPeriodJoin_ProratesBaseSalary()
    {
        var employee = CreateEmployee("EMP004", "Clara", "Oswald");
        // Joined March 16 in a 31-day month -> 16 days active (March 16 to 31)
        employee.JoinDate = new DateTime(2026, 3, 16);
        employee.BankAccountNumber = "123456789";
        employee.BankRoutingNumber = "987654321";

        var comp = new EmployeeCompensation { BaseSalary = 6200m, EffectiveFrom = new DateTime(2026, 3, 16) };
        var context = new EmployeePayrollContext(
            Employee: employee,
            ActiveCompensation: comp,
            SalaryStructure: null,
            Components: new List<PayComponent>(),
            AttendanceSummary: new AttendanceSummary(22, 12, 0, 0, 0, 0, false),
            TaxConfig: CreateDemoTaxConfig(),
            Period: new PayrollPeriod(2026, 3, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), 22)
        );

        var result = _payrollCalculator.CalculateEmployeePayroll(context);

        // 16 / 31 * 6200 = 3200.00
        result.BaseSalary.Should().Be(3200.00m);
        result.Components.Should().Contain(c => c.Code == "BASIC" && c.Explanation.Contains("Prorated Base"));
    }

    private static Employee CreateEmployee(string no, string first, string last)
    {
        return new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNo = no,
            FirstName = first,
            LastName = last,
            WorkEmail = $"{first.ToLower()}@northstar.local",
            JoinDate = new DateTime(2025, 1, 1),
            Status = EmploymentStatus.Active,
            PaymentMethod = PaymentMethod.BankTransfer,
            BankAccountNumber = "111222333",
            BankRoutingNumber = "444555666"
        };
    }

    private static TaxRuleConfig CreateDemoTaxConfig(decimal standardDeduction = 0m)
    {
        return new TaxRuleConfig
        {
            StandardDeduction = standardDeduction,
            Brackets = new List<TaxBracket>
            {
                new() { LowerBound = 0, UpperBound = 3000, Rate = 0.00m, Description = "0% up to $3k" },
                new() { LowerBound = 3000, UpperBound = 6000, Rate = 0.10m, Description = "10% from $3k to $6k" },
                new() { LowerBound = 6000, UpperBound = 10000, Rate = 0.15m, Description = "15% from $6k to $10k" },
                new() { LowerBound = 10000, UpperBound = null, Rate = 0.20m, Description = "20% above $10k" }
            }
        };
    }
}
