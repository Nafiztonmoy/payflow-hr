# PayFlow HR — RESTful API Specification

## 1. Overview & Standards

The **PayFlow HR** Web API is architected according to modern REST standards, utilizing ASP.NET Core 10.

- **Base URL Prefix**: `/api/v1`
- **Content Type**: `application/json` (UTF-8)
- **Error Format**: [RFC 7807 ProblemDetails](https://datatracker.ietf.org/doc/html/rfc7807)
- **Correlation ID Header**: `X-Correlation-ID` (accepted on request, automatically generated if omitted, echoed in response headers)
- **Authentication**: `Authorization: Bearer <JWT>` header or `payflow_jwt` HttpOnly cookie.

---

## 2. Authentication Endpoints (`/api/v1/auth`)

### 2.1 User Login
`POST /api/v1/auth/login`
Authenticates user credentials and issues JWT token and session cookie.

**Request Body:**
```json
{
  "emailOrUsername": "accountant@northstar.local",
  "password": "Accountant@PayFlow2026!"
}
```

**Response (200 OK):**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "ref_8f4d9203a...",
  "user": {
    "id": "11111111-2222-3333-4444-555555555555",
    "userName": "accountant",
    "email": "accountant@northstar.local",
    "role": "Accountant",
    "employeeId": "e2222222-3333-4444-5555-666666666666",
    "employeeName": "Angela Martin"
  }
}
```

### 2.2 Current User Profile
`GET /api/v1/auth/me`
Retrieves claims and linked employee profile for the authenticated token.

**Response (200 OK):**
```json
{
  "id": "11111111-2222-3333-4444-555555555555",
  "userName": "accountant",
  "email": "accountant@northstar.local",
  "role": "Accountant",
  "employeeId": "e2222222-3333-4444-5555-666666666666"
}
```

---

## 3. Payroll Workspace Endpoints (`/api/v1/payroll-runs`)

### 3.1 List Payroll Runs
`GET /api/v1/payroll-runs`
**Authorized Roles**: `Admin`, `Accountant`, `HR`

**Response (200 OK):**
```json
[
  {
    "id": "77777777-8888-9999-aaaa-bbbbbbbbbbbb",
    "periodYear": 2026,
    "periodMonth": 3,
    "periodStart": "2026-03-01",
    "periodEnd": "2026-03-31",
    "status": "Calculated",
    "employeeCount": 25,
    "totalGross": 172450.00,
    "totalNet": 147820.00,
    "totalTax": 14580.00,
    "totalDeductions": 10050.00,
    "blockingCount": 1,
    "warningCount": 2,
    "calculationHash": "8f8883e20d575c5ecbc059...bfa1",
    "concurrencyToken": "9a12bc34-56de-78fa-bcde-f0123456789a",
    "approvedAtUtc": null,
    "paidAtUtc": null
  }
]
```

### 3.2 Execute Calculations
`POST /api/v1/payroll-runs/{id}/calculate`
**Authorized Roles**: `Admin`, `Accountant`
Executes pure deterministic formula calculations across all active employees for the period.

**Response (200 OK):** Returns updated `PayrollRunSummaryDto`.

### 3.3 Submit for Review
`POST /api/v1/payroll-runs/{id}/submit-review`
**Authorized Roles**: `Admin`, `Accountant`

**Request Body:**
```json
{
  "concurrencyToken": "9a12bc34-56de-78fa-bcde-f0123456789a"
}
```

### 3.4 Approve Payroll Run
`POST /api/v1/payroll-runs/{id}/approve`
**Authorized Roles**: `Admin`, `HR`
> **Guarded Operation**: Fails with `400 Bad Request` if any unresolved blocking exceptions exist.

**Request Body:**
```json
{
  "concurrencyToken": "9a12bc34-56de-78fa-bcde-f0123456789a"
}
```

### 3.5 Mark as Paid (Execute Disbursement)
`POST /api/v1/payroll-runs/{id}/mark-paid`
**Authorized Roles**: `Admin`, `Accountant`

**Request Body:**
```json
{
  "paymentReference": "FEDWIRE-ACH-2026-03-31",
  "disbursedAtUtc": "2026-03-31T16:00:00Z",
  "concurrencyToken": "9a12bc34-56de-78fa-bcde-f0123456789a"
}
```

### 3.6 Query Exceptions
`GET /api/v1/payroll-runs/{id}/exceptions`
**Authorized Roles**: `Admin`, `Accountant`, `HR`

**Response (200 OK):**
```json
[
  {
    "id": "e1111111-2222-3333-4444-555555555555",
    "employeeId": "e3333333-4444-5555-6666-777777777777",
    "employeeNo": "EMP-ENG-003",
    "employeeName": "James Liu",
    "code": "MISSING_BANK_ACCOUNT",
    "severity": "Blocking",
    "message": "Direct deposit account number is missing from employee profile.",
    "isResolved": false,
    "resolutionNotes": null
  }
]
```

### 3.7 Resolve Exception
`POST /api/v1/payroll-runs/exceptions/{id}/resolve`
**Authorized Roles**: `Admin`, `Accountant`, `HR`

**Request Body:**
```json
{
  "resolutionNotes": "Verified manual check payment voucher approved by Treasury Controller."
}
```

### 3.8 Export CSV Summary
`GET /api/v1/payroll-runs/{id}/export-csv`
**Authorized Roles**: `Admin`, `Accountant`, `HR`
Returns `text/csv` stream with complete employee disbursement details.

---

## 4. Payslip Endpoints (`/api/v1/payroll-items`)

### 4.1 Retrieve Payslip
`GET /api/v1/payroll-items/{id}/payslip`
**Authorized Roles**: `Admin`, `HR`, `Accountant`, `Manager`, `Employee`

> **Security Rule**: Ordinary employees and managers can ONLY access their own payslip. Requesting an ID belonging to another employee returns `403 Forbidden`.

**Response (200 OK):**
```json
{
  "payrollItemId": "88888888-9999-aaaa-bbbb-cccccccccccc",
  "payrollRunId": "77777777-8888-9999-aaaa-bbbbbbbbbbbb",
  "employeeId": "e2222222-3333-4444-5555-666666666666",
  "employeeNo": "EMP-ENG-001",
  "employeeName": "Alex Carter",
  "designation": "Senior Software Engineer",
  "department": "Engineering",
  "periodStart": "2026-03-01",
  "periodEnd": "2026-03-31",
  "paymentDate": "2026-03-31",
  "baseSalary": 7500.00,
  "proratedBaseSalary": 7500.00,
  "allowancesTotal": 700.00,
  "overtimeHours": 6.00,
  "overtimePay": 421.88,
  "unpaidLeaveDeduction": 0.00,
  "grossPay": 8621.88,
  "taxWithholding": 1058.40,
  "pensionDeduction": 375.00,
  "healthContribution": 100.00,
  "totalDeductions": 1533.40,
  "netPay": 7088.48,
  "employerPensionMatch": 375.00,
  "employerHealthContribution": 200.00,
  "totalCostToCompany": 9196.88,
  "paymentMethod": "BankTransfer",
  "bankName": "Silicon Valley Commercial Bank",
  "bankAccountNumberMasked": "••••••••••••1001",
  "components": [
    {
      "componentCode": "BASE",
      "componentName": "Base Salary",
      "componentType": "Earning",
      "amount": 7500.00
    },
    {
      "componentCode": "HOUSING",
      "componentName": "Housing Allowance",
      "componentType": "Earning",
      "amount": 450.00
    },
    {
      "componentCode": "TAX_PROGRESSIVE",
      "componentName": "Income Tax Withholding (DEMO-PROGRESSIVE-2026)",
      "componentType": "Tax",
      "amount": 1058.40
    }
  ]
}
```

---

## 5. Security & System Audit Endpoints (`/api/v1/audit-logs`)

### 5.1 Query Audit Trail
`GET /api/v1/audit-logs?action=APPROVE_PAYROLL_RUN&entityType=PayrollRun&page=1&pageSize=50`
**Authorized Roles**: `Admin`

**Response (200 OK):**
```json
{
  "totalCount": 42,
  "page": 1,
  "pageSize": 50,
  "items": [
    {
      "id": "a1111111-2222-3333-4444-555555555555",
      "actorId": "11111111-2222-3333-4444-555555555555",
      "actorEmail": "admin@northstar.local",
      "actorRole": "Admin",
      "action": "APPROVE_PAYROLL_RUN",
      "entityType": "PayrollRun",
      "entityId": "77777777-8888-9999-aaaa-bbbbbbbbbbbb",
      "correlationId": "corr_9a12bc34-56de",
      "timestampUtc": "2026-03-31T14:30:00Z",
      "summary": "Approved March 2026 payroll run after verifying 0 blocking exceptions.",
      "beforeValuesJson": "{\"status\":\"InReview\"}",
      "afterValuesJson": "{\"status\":\"Approved\"}"
    }
  ]
}
```

---

## 6. System Health Check (`/api/v1/health`)

`GET /api/v1/health`
**Authorized Roles**: Anonymous

**Response (200 OK):**
```json
{
  "status": "Healthy",
  "database": "Connected",
  "version": "1.0.0",
  "timestamp": "2026-10-08T12:10:00Z"
}
```
