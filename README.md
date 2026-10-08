# PayFlow HR — Auditable Enterprise Payroll & Human Capital Platform

**Redesigned frontend:** Start with [START_HERE.md](START_HERE.md). Current validation and limitations are in [docs/FRONTEND_REDESIGN.md](docs/FRONTEND_REDESIGN.md). Backend test claims elsewhere in this inherited README were not reverified in this redesign.

[![.NET 10](https://img.shields.io/badge/.NET-10.0%20LTS-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![React 19](https://img.shields.io/badge/React-19-61DAFB?logo=react)](https://react.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-6.0-3178C6?logo=typescript)](https://www.typescriptlang.org/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql)](https://www.postgresql.org/)
[![Tailwind CSS v4](https://img.shields.io/badge/Tailwind-v4.0-06B6D4?logo=tailwindcss)](https://tailwindcss.com/)
[![Tests](https://img.shields.io/badge/Frontend-Build%20%26%20DOM%20verified-success)](#testing--verification)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

**PayFlow HR** is an enterprise-grade, auditable Human Resources & Payroll Platform built on modern cloud-native standards. Engineered for defense-in-depth security and mathematical certainty, PayFlow HR features a pure deterministic payroll calculation engine, a 6-stage lifecycle state machine, tamper-evident SHA-256 calculation snapshots, and server-enforced role-based access control (RBAC).

The platform comes pre-configured with the sample enterprise **Northstar Technologies Inc.** (25 realistic employees, 5 departments, 1 historical paid payroll run, and 1 active run featuring a fixable blocking exception).

---

## 🏛️ System Architecture

PayFlow HR follows **Clean Architecture** and Domain-Driven Design principles:

```
+---------------------------------------------------------------------------------------------------------+
|                                        PAYFLOW HR ARCHITECTURE                                          |
|                                                                                                         |
|  [ Frontend Client ]     React 19 + TypeScript + Tailwind v4 + TanStack Query + Recharts                |
|           |                                                                                             |
|           v (REST / JSON / RFC 7807)                                                                    |
|  [ Presentation ]        PayFlow.Api (.NET 10 Web API, JWT + HttpOnly Cookies, CorrelationId)           |
|           |                                                                                             |
|           v (DTOs / Contracts)                                                                          |
|  [ Application  ]        PayFlow.Application (Pure Deterministic Engine, Tax Rules, Exception Rules)    |
|           |                                                                                             |
|           v (Entities / Enums / Aggregates)                                                             |
|  [ Domain       ]        PayFlow.Domain (PayrollRun Aggregate, Compensation Contracts, AuditLog)        |
|           ^                                                                                             |
|           | (Data Access Implementations)                                                               |
|  [ Infrastructure ]      PayFlow.Infrastructure (EF Core 10, Npgsql, PBKDF2 Hasher, Data Seeder)        |
|           |                                                                                             |
|           v                                                                                             |
|  [ Database     ]        PostgreSQL 16 (Port 5434, numeric(18, 4) financial arithmetic)                  |
+---------------------------------------------------------------------------------------------------------+
```

---

## ⭐ Distinctive Core Values

1. **Pure Deterministic Calculation Engine**:
   - Zero floating-point drift: 100% fixed-point financial arithmetic (`decimal` in C#, `numeric(18, 4)` in PostgreSQL).
   - Commercial rounding: `MidpointRounding.AwayFromZero` across all intermediate components.
   - Exact formulas for base proration, unpaid leave (loss of pay), 1.5x overtime multiplier, progressive tax withholding (`DEMO-PROGRESSIVE-2026`), and employer Cost to Company (CTC).
2. **State Machine Lifecycle & Zero-Blocking Rule**:
   - Workflow: `Draft` $\rightarrow$ `Calculating` $\rightarrow$ `Calculated` $\rightarrow$ `InReview` $\rightarrow$ `Approved` $\rightarrow$ `Paid`.
   - **Guarded Gate**: Approval strictly requires **zero unresolved blocking exceptions**. The active March 2026 run includes a blocking exception (`MISSING_BANK_ACCOUNT` for James Liu) that must be resolved with an auditable note before approval can proceed.
3. **Tamper-Evident SHA-256 Calculation Snapshots**:
   - Every calculated run computes a SHA-256 cryptographic digest across the normalized set of line items and totals.
   - Any manual database tampering breaks the hash, providing verifiable audit proof.
4. **Strict 5-Role Server-Side RBAC & Resource Isolation**:
   - 5 roles: `Admin`, `HR`, `Manager`, `Accountant`, `Employee`.
   - Employees and managers can **only view their own payslips**. Requesting another employee's payslip is denied with **HTTP 403 Forbidden**.
5. **Historical Compensation Immutability**:
   - Dated compensation contracts (`EffectiveFrom` / `EffectiveTo`) preserve historical pay rates; previous cycles are never altered when salaries change.
6. **Immutable Security Audit Trail**:
   - Append-only `AuditLog` captures actor, role, action, timestamp, correlation ID, and before/after JSON diffs.

---

## 👥 Demo Personas & Credentials

The application includes a **1-Click Persona Switcher** in the top navigation bar for immediate evaluator demonstration:

| Persona Role | Name | Email / Username | Password | Key Responsibilities |
| :--- | :--- | :--- | :--- | :--- |
| **Admin** | System Administrator | `admin@northstar.local` | `Admin@PayFlow2026!` | Global configuration, security audit logs, overrides. |
| **HR** | Rachel Green | `hr@northstar.local` | `Hr@PayFlow2026!` | Employee directory, compensation contracts, departments, leave approvals. |
| **Manager** | David Miller | `manager@northstar.local` | `Manager@PayFlow2026!` | Engineering team view, team leave approvals. Prohibited from other salaries. |
| **Accountant** | Angela Martin | `accountant@northstar.local` | `Accountant@PayFlow2026!` | Payroll workspace, calculation engine, exception resolution, disbursements. |
| **Employee** | Alex Carter | `employee@northstar.local` | `Employee@PayFlow2026!` | Self-service portal: view personal attendance, submit leaves, view own payslips. |

---

## 🚀 Quick Start Guide

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 24.15+](https://nodejs.org/) & `npm`
- [Docker & Docker Compose](https://www.docker.com/) (optional, for containerized run)

### Option A: Local Development (Recommended)

1. **Start PostgreSQL Database**:
   ```bash
   docker compose up -d db
   ```
   *Note: Database is mapped to host port `5434` to prevent collisions with any existing PostgreSQL instances.*

2. **Launch ASP.NET Core API**:
   ```bash
   dotnet run --project src/PayFlow.Api
   ```
   *The API boots on `http://localhost:5005`, automatically runs schema migrations, and seeds the full Northstar Technologies dataset on startup.*
   - Swagger / OpenAPI Docs: [http://localhost:5005/swagger](http://localhost:5005/swagger)
   - Health Check: [http://localhost:5005/api/v1/health](http://localhost:5005/api/v1/health)

3. **Launch React SPA**:
   ```bash
   cd src/payflow-web
   npm ci
   npm run dev
   ```
   *Open [http://localhost:5173](http://localhost:5173) in your browser.*

---

### Option B: Docker Compose Deployment

Run the entire ecosystem (PostgreSQL, Backend API, and Nginx React SPA) with a single command:

```bash
docker compose up -d --build
```

- **Frontend Web UI**: [http://localhost:5173](http://localhost:5173)
- **Backend Web API**: [http://localhost:5005](http://localhost:5005)
- **Database**: `localhost:5434`

---

## 🧪 Testing & Verification

PayFlow HR includes 30 automated tests covering unit mathematical accuracy and end-to-end integration flows:

```bash
# Run all 30 automated backend tests
dotnet test PayFlow.slnx
```

### Test Suite Summary:
- **`PayFlow.UnitTests` (18 Tests - 100% Pass)**:
  - Baseline monthly salary calculation
  - Mid-month join proration factor
  - Unpaid leave daily deduction rate
  - 1.5x overtime multiplier
  - Tiered progressive tax brackets (`DEMO-PROGRESSIVE-2026`)
  - Commercial midpoint rounding precision (`AwayFromZero`)
  - Exception detection engine
  - Deterministic SHA-256 calculation hashing
- **`PayFlow.IntegrationTests` (12 Tests - 100% Pass)**:
  - Authentication for all 5 persona accounts
  - Invalid credential rejection (401)
  - RBAC payroll run access restrictions (403)
  - Manager payroll approval prohibition (403)
  - Employee payslip isolation (verifies 403 when requesting other employee's payslip)
  - Employee own payslip access (200)
  - **Full Payroll Lifecycle**: Recalculate $\rightarrow$ Zero-blocking gate rejects approval (400) $\rightarrow$ Resolve blocking exception $\rightarrow$ Submit for review $\rightarrow$ Approve $\rightarrow$ Mark as Paid $\rightarrow$ Immutability check $\rightarrow$ CSV Export
  - Audit trail event persistence

```bash
# Build & verify frontend TypeScript types
cd src/payflow-web
npm run build
```

---

## 📚 Technical Documentation Index

Detailed specifications and architectural guides are available in the [`docs/`](file:///d:/Enterprise%20Payroll%20&%20HR%20Platform/docs) directory:

- [**docs/ARCHITECTURE.md**](file:///d:/Enterprise%20Payroll%20&%20HR%20Platform/docs/ARCHITECTURE.md) — System layers, request lifecycle, dependency diagrams, and concurrency control.
- [**docs/DATA_MODEL.md**](file:///d:/Enterprise%20Payroll%20&%20HR%20Platform/docs/DATA_MODEL.md) — 3NF Entity-Relationship model, schema reference, and historical contract versioning.
- [**docs/PAYROLL_ENGINE.md**](file:///d:/Enterprise%20Payroll%20&%20HR%20Platform/docs/PAYROLL_ENGINE.md) — Mathematical formulas, state machine, exception engine, and SHA-256 snapshots.
- [**docs/SECURITY.md**](file:///d:/Enterprise%20Payroll%20&%20HR%20Platform/docs/SECURITY.md) — Threat model, PBKDF2 hashing, dual-token delivery, RBAC boundaries, and OWASP protections.
- [**docs/API.md**](file:///d:/Enterprise%20Payroll%20&%20HR%20Platform/docs/API.md) — Complete RESTful endpoint reference with request/response schemas.
- [**docs/PORTFOLIO.md**](file:///d:/Enterprise%20Payroll%20&%20HR%20Platform/docs/PORTFOLIO.md) — Engineering showcase, architectural decision records (ADRs), and verification scorecards.

---

## ⚖️ Statutory Disclaimer

The progressive tax calculation ruleset (`DEMO-PROGRESSIVE-2026`) utilized in PayFlow HR is a configurable demonstration model designed to showcase tiered marginal withholding calculations. It does **not** constitute authoritative legal or statutory tax advice for Bangladesh, the United States, or any other jurisdiction.

---

## 📄 License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
