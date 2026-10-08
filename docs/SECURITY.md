# PayFlow HR — Enterprise Security & Compliance Specification

## 1. Security Architecture & Threat Model

**PayFlow HR** handles highly sensitive organizational financial data, employee identities, and compensation records. The platform is designed around a **Zero Trust** security posture, implementing defense-in-depth across authentication, authorization, cryptographic storage, and immutable auditability.

```
+---------------------------------------------------------------------------------------------------+
|                                       SECURITY DEFENSE RINGS                                      |
|                                                                                                   |
|  [ RING 1: Network & Protocol ] ---> HTTPS / TLS 1.3, Strict CORS, Security Headers (OWASP)     |
|  [ RING 2: Authentication     ] ---> PBKDF2 100k Iterations, JWT + HttpOnly SameSite Cookies     |
|  [ RING 3: Authorization      ] ---> Strict 5-Role Server-Side RBAC, Resource Ownership Checks   |
|  [ RING 4: Data Integrity     ] ---> Optimistic Concurrency, SHA-256 Hashes, Immutable Logs      |
|  [ RING 5: Audit & Tracing    ] ---> Correlation ID Propagation, RFC 7807 Error Envelopes        |
+---------------------------------------------------------------------------------------------------+
```

---

## 2. Authentication Architecture

### 2.1 Password Hashing (RFC 2898 PBKDF2)
Passwords in PayFlow HR are never stored in plaintext or weak single-pass hashes. The platform implements `IPasswordHasher` using the industry-standard PBKDF2 algorithm:
- **Algorithm**: PBKDF2 with `HMAC-SHA256`
- **Salt**: 32 bytes (256 bits) of cryptographically secure pseudo-random bytes generated via `RandomNumberGenerator`
- **Iterations**: 100,000 iterations
- **Key Derivation Length**: 32 bytes (256 bits)
- **Format**: Constant-length format `{iterations}.{salt_base64}.{hash_base64}` using timing-safe comparisons (`CryptographicOperations.FixedTimeEquals`) to eliminate side-channel timing attacks.

### 2.2 Dual Delivery Token System
PayFlow HR supports modern web applications and mobile API consumers via dual token transmission:
1. **Bearer Authorization Header**: `Authorization: Bearer <JWT>` for programmatic clients and mobile applications.
2. **HttpOnly Cookie**: Secure session cookie (`payflow_jwt`) configured with:
   - `HttpOnly = true` (blocks JavaScript access, mitigating Cross-Site Scripting [XSS] token theft).
   - `SameSite = SameSiteMode.Lax` or `Strict` (mitigates Cross-Site Request Forgery [CSRF]).
   - `Secure = true` in production environments.

---

## 3. Strict 5-Role Server-Side Authorization (RBAC)

Authorization is enforced strictly on the server in ASP.NET Core controllers and domain services using `[Authorize(Roles = "...")]` attributes and explicit ownership checks. Client-side navigation guards serve solely as user experience conveniences and are never trusted for authorization.

| Role | Permitted Capabilities | Prohibited Boundaries |
| :--- | :--- | :--- |
| **Admin** | Full system configuration, user provisioning, global audit logs, emergency override. | Cannot alter finalized historical accounting records without audit record. |
| **HR** | Employee onboarding, compensation contracts, job grades, department management, leave approvals. | Cannot trigger payroll disbursement or execute ACH banking release. |
| **Manager** | View direct reports attendance, approve team leave requests, review team org charts. | Cannot view employee compensation or salary outside their reporting line; cannot approve payroll runs. |
| **Accountant** | Calculate payroll runs, manage salary structures, resolve payroll exceptions, execute disbursements. | Cannot alter core employee contracts or delete audit trails. |
| **Employee** | Self-service portal: view personal attendance, submit leave requests, view own payslips. | **Strictly prohibited** from viewing any other employee's salary, attendance, or payslip (enforces HTTP 403 Forbidden). |

---

## 4. Critical Security Invariants & Verification

### 4.1 Resource Ownership & Payslip Isolation (Invariant #1)
An employee must never be able to access or enumerate another employee's payslip or salary:

```csharp
[HttpGet("{id}/payslip")]
[Authorize(Roles = "Admin,HR,Accountant,Employee,Manager")]
public async Task<IActionResult> GetPayslip(Guid id)
{
    var item = await _payrollRunService.GetItemByIdAsync(id);
    if (item == null) return NotFound();

    var currentUserId = GetCurrentUserId();
    var currentRole = GetCurrentUserRole();

    // Invariant: Non-privileged users can ONLY access their own payslip
    if (currentRole == UserRole.Employee || currentRole == UserRole.Manager)
    {
        var employee = await _employeeService.GetByUserIdAsync(currentUserId);
        if (employee == null || item.EmployeeId != employee.Id)
        {
            return Forbid(); // HTTP 403 Forbidden
        }
    }

    return Ok(item.ToPayslipDto());
}
```

> **Verified in Automated Tests**: [`RBAC_EmployeeCannotAccessOtherEmployeePayslip_ReturnsForbidden`](file:///d:/Enterprise%20Payroll%20&%20HR%20Platform/tests/PayFlow.IntegrationTests/PayFlowIntegrationTests.cs#L75-L89) executes against the live API and verifies an HTTP 403 response.

### 4.2 Zero-Blocking Exception Approval Gate (Invariant #2)
A payroll run cannot be approved if a single blocking exception is open:
- If `BlockingExceptionsCount > 0`, `ApproveRunAsync` immediately rejects the transition and throws `PayrollDomainException`.
- The exception details and resolving officer must be permanently recorded before approval can proceed.

> **Verified in Automated Tests**: [`PayrollLifecycle_AccountantRunsCalculationsAndEnforcesApprovalZeroBlockingRule`](file:///d:/Enterprise%20Payroll%20&%20HR%20Platform/tests/PayFlow.IntegrationTests/PayFlowIntegrationTests.cs#L110-L184) confirms that approval attempts fail with HTTP 400 Bad Request until the blocking exception is resolved.

### 4.3 Immutability of Paid Cycles (Invariant #3)
Once a payroll run transitions to `Paid`:
- State cannot be reverted.
- Recalculation endpoints return HTTP 400 Bad Request.
- Individual line items cannot be modified or deleted.

---

## 5. Tamper-Evident Auditing & Cryptographic Integrity

### 5.1 Immutable Append-Only Audit Trail
The [`AuditLog`](file:///d:/Enterprise%20Payroll%20&%20HR%20Platform/src/PayFlow.Domain/Entities/AuditLog.cs) table records every state mutation, authentication event, exception resolution, and compensation revision.
- Records contain: `ActorId`, `ActorEmail`, `ActorRole`, `Action`, `EntityType`, `EntityId`, `TimestampUtc`, `CorrelationId`, and JSON snapshots (`BeforeValuesJson`, `AfterValuesJson`).
- Database constraints and application policies prohibit `UPDATE` and `DELETE` queries on the `AuditLogs` table.

### 5.2 SHA-256 Calculation Snapshots
Calculated payroll runs compute a deterministic SHA-256 digest across the ordered set of all line items, net disbursements, and tax amounts. Any unauthorized manual manipulation of numbers in the persistence tier invalidates the stored calculation hash, providing immediate tamper detection for compliance audits.

---

## 6. Information Leakage Prevention & Error Handling

- **RFC 7807 ProblemDetails**: Unhandled exceptions are intercepted by [`GlobalExceptionMiddleware`](file:///d:/Enterprise%20Payroll%20&%20HR%20Platform/src/PayFlow.Api/Infrastructure/GlobalExceptionMiddleware.cs) and formatted into structured JSON problem objects. Stack traces and database internal errors are hidden in production environments.
- **Request Correlation**: The `X-Correlation-ID` header is propagated through Serilog log context and returned in all responses for end-to-end trace correlation without exposing internal identifiers.
