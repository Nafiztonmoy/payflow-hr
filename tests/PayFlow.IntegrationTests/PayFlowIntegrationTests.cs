using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PayFlow.Api.Controllers;
using PayFlow.Application.Payroll.Interfaces;
using PayFlow.Domain.Enums;
using PayFlow.Infrastructure.Data;
using Xunit;

namespace PayFlow.IntegrationTests;

public class PayFlowIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public PayFlowIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new AuthController.LoginRequest("admin@northstar.local", "WrongPassword!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("admin@northstar.local", "Admin@PayFlow2026!", "Admin")]
    [InlineData("hr@northstar.local", "Hr@PayFlow2026!", "HR")]
    [InlineData("manager@northstar.local", "Manager@PayFlow2026!", "Manager")]
    [InlineData("accountant@northstar.local", "Accountant@PayFlow2026!", "Accountant")]
    [InlineData("employee@northstar.local", "Employee@PayFlow2026!", "Employee")]
    public async Task Login_WithValidPersonas_SucceedsWithCorrectRole(string email, string password, string expectedRole)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new AuthController.LoginRequest(email, password));

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AuthController.AuthResponse>();

        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.User.Role.Should().Be(expectedRole);
    }

    [Fact]
    public async Task RBAC_EmployeeCannotAccessPayrollRuns_ReturnsForbidden()
    {
        var empClient = await _factory.CreateAuthenticatedClientAsync("employee@northstar.local", "Employee@PayFlow2026!");
        var response = await empClient.GetAsync("/api/v1/payroll-runs");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RBAC_ManagerCannotApprovePayrollRuns_ReturnsForbidden()
    {
        var mgrClient = await _factory.CreateAuthenticatedClientAsync("manager@northstar.local", "Manager@PayFlow2026!");
        var response = await mgrClient.PostAsJsonAsync($"/api/v1/payroll-runs/{Guid.NewGuid()}/approve", new PayrollRunsController.ApproveRunRequest(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RBAC_EmployeeCannotAccessOtherEmployeePayslip_ReturnsForbidden()
    {
        // Obtain a payslip that belongs to another employee (e.g. CEO or Manager) from February run
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PayFlowDbContext>();
        
        var otherEmployee = await db.Employees.FirstAsync(e => e.WorkEmail != "employee@northstar.local");
        var otherPayslipItem = await db.PayrollItems.FirstAsync(i => i.EmployeeId == otherEmployee.Id);

        var empClient = await _factory.CreateAuthenticatedClientAsync("employee@northstar.local", "Employee@PayFlow2026!");
        var response = await empClient.GetAsync($"/api/v1/payroll-items/{otherPayslipItem.Id}/payslip");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RBAC_EmployeeCanAccessOwnPayslip_ReturnsSuccess()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PayFlowDbContext>();
        
        var myEmployee = await db.Employees.FirstAsync(e => e.WorkEmail == "employee@northstar.local");
        var myPayslipItem = await db.PayrollItems.FirstAsync(i => i.EmployeeId == myEmployee.Id);

        var empClient = await _factory.CreateAuthenticatedClientAsync("employee@northstar.local", "Employee@PayFlow2026!");
        var response = await empClient.GetAsync($"/api/v1/payroll-items/{myPayslipItem.Id}/payslip");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payslip = await response.Content.ReadFromJsonAsync<PayslipDto>(JsonOptions);
        payslip.Should().NotBeNull();
        payslip!.EmployeeNo.Should().Be("EMP-ENG-001");
        payslip.NetPay.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PayrollLifecycle_AccountantRunsCalculationsAndEnforcesApprovalZeroBlockingRule()
    {
        var actClient = await _factory.CreateAuthenticatedClientAsync("accountant@northstar.local", "Accountant@PayFlow2026!");

        // 1. Get runs
        var runsResponse = await actClient.GetAsync("/api/v1/payroll-runs");
        runsResponse.EnsureSuccessStatusCode();
        var runs = await runsResponse.Content.ReadFromJsonAsync<List<PayrollRunSummaryDto>>(JsonOptions);
        runs.Should().NotBeNull();

        var marchRun = runs!.First(r => r.Year == 2026 && r.Month == 3);

        // 2. Re-calculate run (idempotency check)
        var calcResponse = await actClient.PostAsync($"/api/v1/payroll-runs/{marchRun.Id}/calculate", null);
        calcResponse.EnsureSuccessStatusCode();
        var updatedRun = await calcResponse.Content.ReadFromJsonAsync<PayrollRunSummaryDto>(JsonOptions);

        updatedRun!.BlockingCount.Should().BeGreaterThan(0, "Seeded March run contains a blocking exception (James Liu missing bank info)");

        // 3. Attempt to approve directly while blocking exceptions exist -> MUST BE REJECTED with 400!
        var approveAttempt = await actClient.PostAsJsonAsync($"/api/v1/payroll-runs/{marchRun.Id}/approve", new PayrollRunsController.ApproveRunRequest(updatedRun.ConcurrencyToken));
        approveAttempt.StatusCode.Should().Be(HttpStatusCode.BadRequest, "Zero blocking exceptions required to approve!");

        // 4. Resolve all blocking exceptions
        var exceptionsResponse = await actClient.GetAsync($"/api/v1/payroll-runs/{marchRun.Id}/exceptions");
        var exceptions = await exceptionsResponse.Content.ReadFromJsonAsync<List<PayrollExceptionDto>>(JsonOptions);

        var blockingException = exceptions!.First(e => e.Severity == ExceptionSeverity.Blocking);
        var resolveResponse = await actClient.PostAsJsonAsync(
            $"/api/v1/payroll-runs/exceptions/{blockingException.Id}/resolve",
            new PayrollRunsController.ResolveExceptionRequest("Verified manual check payment method approved by Finance controller.")
        );
        resolveResponse.EnsureSuccessStatusCode();

        // 5. Submit for review
        var latestRunSummary = await (await actClient.GetAsync($"/api/v1/payroll-runs/{marchRun.Id}")).Content.ReadFromJsonAsync<PayrollRunSummaryDto>(JsonOptions);
        var submitReviewResponse = await actClient.PostAsJsonAsync(
            $"/api/v1/payroll-runs/{marchRun.Id}/submit-review",
            new PayrollRunsController.SubmitReviewRequest(latestRunSummary!.ConcurrencyToken)
        );
        submitReviewResponse.EnsureSuccessStatusCode();

        // 6. Approve run (now 0 unresolved blocking exceptions!)
        var inReviewRun = await submitReviewResponse.Content.ReadFromJsonAsync<PayrollRunSummaryDto>(JsonOptions);
        var approveResponse = await actClient.PostAsJsonAsync(
            $"/api/v1/payroll-runs/{marchRun.Id}/approve",
            new PayrollRunsController.ApproveRunRequest(inReviewRun!.ConcurrencyToken)
        );
        approveResponse.EnsureSuccessStatusCode();
        var approvedRun = await approveResponse.Content.ReadFromJsonAsync<PayrollRunSummaryDto>(JsonOptions);
        approvedRun!.Status.Should().Be(PayrollRunStatus.Approved);

        // 7. Mark as Paid
        var markPaidResponse = await actClient.PostAsJsonAsync(
            $"/api/v1/payroll-runs/{marchRun.Id}/mark-paid",
            new PayrollRunsController.MarkPaidRequest("FEDWIRE-ACH-2026-03-31", DateTime.UtcNow, approvedRun.ConcurrencyToken)
        );
        markPaidResponse.EnsureSuccessStatusCode();
        var paidRun = await markPaidResponse.Content.ReadFromJsonAsync<PayrollRunSummaryDto>(JsonOptions);
        paidRun!.Status.Should().Be(PayrollRunStatus.Paid);
        paidRun.PaymentReference.Should().Be("FEDWIRE-ACH-2026-03-31");

        // 8. Recalculation on Paid run must be forbidden/rejected
        var illegalRecalc = await actClient.PostAsync($"/api/v1/payroll-runs/{marchRun.Id}/calculate", null);
        illegalRecalc.StatusCode.Should().Be(HttpStatusCode.BadRequest, "Paid runs are immutable!");

        // 9. Export CSV
        var csvResponse = await actClient.GetAsync($"/api/v1/payroll-runs/{marchRun.Id}/export-csv");
        csvResponse.EnsureSuccessStatusCode();
        var csvContent = await csvResponse.Content.ReadAsStringAsync();
        csvContent.Should().Contain("Employee No,Employee Name,Department");
        csvContent.Should().Contain("Alex Carter");
    }

    [Fact]
    public async Task AuditLog_LogsActionsProperly()
    {
        var adminClient = await _factory.CreateAuthenticatedClientAsync("admin@northstar.local", "Admin@PayFlow2026!");
        var response = await adminClient.GetAsync("/api/v1/audit-logs");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("items");
    }
}
