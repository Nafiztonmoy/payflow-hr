
# PayFlow HR — Enterprise Payroll & Human Resource Management Platform

**A full-stack, auditable HR and Payroll Management Platform built with ASP.NET Core 10, React 19, TypeScript, and PostgreSQL.**

[![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-19-61DAFB?logo=react)](https://react.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-6-3178C6?logo=typescript)](https://www.typescriptlang.org/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Neon-4169E1?logo=postgresql)](https://www.postgresql.org/)
[![Tailwind CSS](https://img.shields.io/badge/Tailwind_CSS-v4-06B6D4?logo=tailwindcss)](https://tailwindcss.com/)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)

---

## 🌐 Live Demo

**🚀 [Launch PayFlow HR](https://payflow-i963lyvhu-nafiztonmoys-projects.vercel.app)**

| Component | Platform | Link |
|-----------|----------|------|
| Frontend | Vercel | [Live Application](https://payflow-i963lyvhu-nafiztonmoys-projects.vercel.app) |
| Backend API | Render | [API Server](https://payflow-hr.onrender.com) |
| Database | Neon PostgreSQL | [Neon](https://neon.tech) |
| Repository | GitHub | [Source Code](https://github.com/Nafiztonmoy/payflow-hr) |

> **Note:** The live application is a portfolio demonstration using fictional employee and payroll data. It is not intended for actual payroll processing.

---

## 📌 Project Overview

**PayFlow HR** is a modern enterprise-inspired Human Resource Management and Payroll platform designed to streamline organizational operations.

The application combines a responsive React frontend, a RESTful ASP.NET Core backend, and PostgreSQL for structured data management.

It demonstrates:

- Clean Architecture
- Role-Based Access Control (RBAC)
- Secure authentication workflows
- Deterministic payroll calculations
- Employee lifecycle management
- Payroll approval processes
- Attendance and leave management
- Audit trails and reporting
- Full-stack cloud deployment

The platform includes a fictional organization called **Northstar Technologies**, with sample employees, departments, compensation structures, and payroll records.

---

## ✨ Key Features

### 👥 Employee Management

- Create and manage employee profiles
- Organize employees into departments
- Assign job designations and managers
- Track employment status
- Manage employee compensation records

### 🔐 Authentication & Authorization

- JWT authentication
- HttpOnly authentication cookies
- Password hashing
- Role-based authorization
- Backend-enforced resource permissions

Supported roles:

| Role | Responsibilities |
|------|------------------|
| Admin | System administration, configuration, and auditing |
| HR | Employee management, departments, and leave approvals |
| Manager | Team management and team-related approvals |
| Accountant | Payroll calculation, review, and processing |
| Employee | Personal attendance, leave, and payslip access |

### 💰 Payroll Management

- Salary structure configuration
- Base salary and allowances
- Overtime calculation
- Unpaid leave deductions
- Progressive demo tax calculations
- Gross and net salary calculations
- Payroll review and approval
- Payslip generation
- Payroll exceptions and resolution

### 📅 Attendance & Leave

- Employee attendance tracking
- Leave request submission
- Leave balance management
- Manager and HR approval workflows
- Leave-related payroll adjustments

### 📊 Dashboard & Reporting

- Administrative dashboard
- Employee and payroll statistics
- Payroll reports
- Data visualizations
- Audit activity
- Payroll calculation summaries

---

## 🏗️ System Architecture

PayFlow HR follows **Clean Architecture**, separating the application into independent layers.

```text
                  React Frontend
                 (Vercel Hosting)
                        |
                        | HTTPS REST API
                        v
                 ASP.NET Core API
                  (Render Hosting)
                        |
                        v
                Application Layer
              Services / Use Cases
                        |
                        v
                   Domain Layer
             Entities / Business Logic
                        |
                        v
               Infrastructure Layer
                  EF Core / Npgsql
                        |
                        v
               PostgreSQL Database
                    (Neon)
```

### Backend Layers

**PayFlow.Api**
- REST API controllers
- Authentication configuration
- Middleware
- Dependency injection

**PayFlow.Application**
- Payroll services
- Business use cases
- Application interfaces
- Calculation rules

**PayFlow.Domain**
- Domain entities
- Enums
- Business models
- Payroll lifecycle rules

**PayFlow.Infrastructure**
- Entity Framework Core
- Database context
- PostgreSQL integration
- Database migrations
- Data seeding
- Authentication services

---

## 🛠️ Technology Stack

| Category | Technology |
|----------|------------|
| Frontend | React 19 |
| Language | TypeScript |
| Build Tool | Vite |
| Styling | Tailwind CSS v4 |
| Data Fetching | TanStack Query |
| Charts | Recharts |
| Backend | ASP.NET Core 10 |
| Backend Language | C# |
| Database | PostgreSQL |
| ORM | Entity Framework Core 10 |
| Database Driver | Npgsql |
| Authentication | JWT |
| Authorization | RBAC |
| Password Security | PBKDF2 |
| Containerization | Docker |
| Frontend Hosting | Vercel |
| Backend Hosting | Render |
| Database Hosting | Neon |

---

## 🔄 Payroll Workflow

Payroll processing follows a controlled lifecycle:

```text
Draft
  |
  v
Calculating
  |
  v
Calculated
  |
  v
In Review
  |
  v
Approved
  |
  v
Paid
```

Each payroll run passes through validation and approval stages.

### Payroll Rules

- Uses decimal arithmetic for financial calculations
- Supports employee-specific compensation
- Calculates earnings and deductions
- Accounts for unpaid leave and overtime
- Uses configurable demonstration tax rules
- Checks blocking exceptions before approval
- Produces calculation snapshots and audit records

---

## 📁 Project Structure

```text
payflow-hr/
|
|-- src/
|   |
|   |-- PayFlow.Api/
|   |   |-- Controllers/
|   |   |-- Infrastructure/
|   |   |-- Program.cs
|   |
|   |-- PayFlow.Application/
|   |   |-- Payroll/
|   |   |-- Common/
|   |
|   |-- PayFlow.Domain/
|   |   |-- Entities/
|   |   |-- Enums/
|   |   |-- Models/
|   |
|   |-- PayFlow.Infrastructure/
|   |   |-- Data/
|   |   |-- Migrations/
|   |   |-- Services/
|   |
|   |-- payflow-web/
|       |-- src/
|       |-- package.json
|
|-- docs/
|-- tests/
|-- docker-compose.yml
|-- README.md
```

---

## 🚀 Getting Started

### Prerequisites

Before running the project, install:

- .NET 10 SDK
- Node.js and npm
- PostgreSQL or Docker Desktop
- Git

### 1. Clone the Repository

```bash
git clone https://github.com/Nafiztonmoy/payflow-hr.git

cd payflow-hr
```

### 2. Start PostgreSQL

Using Docker Compose:

```bash
docker compose up -d db
```

The local development database uses port `5434`.

### 3. Configure Environment Variables

Configure the backend database connection and JWT authentication settings.

Required environment variable names:

```env
ConnectionStrings__DefaultConnection=
Jwt__Secret=
Jwt__Issuer=PayFlowHR
Jwt__Audience=PayFlowApp
Cors__AllowedOrigins__0=http://localhost:5173
```

Do not commit real database passwords or JWT secrets.

### 4. Apply Database Migrations

```bash
dotnet ef database update --project src/PayFlow.Infrastructure --startup-project src/PayFlow.Api --context PayFlowDbContext
```

Verify the target database before running the migration.

### 5. Run Backend API

```bash
dotnet run --project src/PayFlow.Api
```

Development API URL:

```text
http://localhost:5005
```

Swagger documentation:

```text
http://localhost:5005/swagger
```

### 6. Run Frontend

Open another terminal:

```bash
cd src/payflow-web

npm ci

npm run dev
```

Frontend URL:

```text
http://localhost:5173
```

---

## 🐳 Docker Setup

Run the full application using Docker Compose:

```bash
docker compose up -d --build
```

Stop the containers:

```bash
docker compose down
```

Use development-only credentials for the local environment.

---

## ☁️ Cloud Deployment

The application is deployed using a modern three-tier cloud architecture.

### Frontend — Vercel

- Framework: React + Vite
- Root Directory: `src/payflow-web`
- Build Command: `npm run build`
- Output Directory: `dist`

### Backend — Render

- Framework: ASP.NET Core 10
- Deployment: Docker-based web service
- Environment: Production
- Database: External PostgreSQL hosted on Neon

### Database — Neon PostgreSQL

- Managed PostgreSQL
- Entity Framework Core migrations
- Relational data storage
- Secure connection through environment variables

### Deployment Flow

```text
GitHub Repository
       |
       +------------------+
       |                  |
       v                  v
     Vercel             Render
   React App        ASP.NET Core API
                          |
                          v
                    Neon PostgreSQL
```

---

## 🧪 Testing

The repository contains backend unit and integration tests.

### Run Backend Tests

```bash
dotnet test PayFlow.slnx
```

### Build Frontend

```bash
cd src/payflow-web

npm ci

npm run build
```

Test results should be verified against the latest code before reporting coverage or pass rates.

---

## 🔒 Security Considerations

The application demonstrates multiple security practices:

- JWT-based authentication
- Password hashing with PBKDF2
- Role-based authorization
- Resource-level access restrictions
- Audit logging
- Environment-based configuration
- Structured exception handling

### Production Security Checklist

Before using the application in a production environment:

1. Remove hardcoded secrets and passwords.
2. Rotate all previously exposed credentials.
3. Disable or rotate publicly known demo accounts.
4. Enforce HTTPS and secure authentication cookies.
5. Restrict CORS to trusted frontend origins.
6. Disable unnecessary production development tools.
7. Review and apply database migrations safely.
8. Enable database backups and operational monitoring.

> **Important:** Removing demonstration passwords from this README does not invalidate existing accounts or erase previously published Git history. Credential rotation is still required.

---

## 📸 Screenshots

Screenshots can be added to the repository to showcase the following interfaces:

- Login Page
- Executive Dashboard
- Employee Directory
- Payroll Management
- Leave Management
- Reporting Dashboard

All screenshots should use fictional or sanitized data.

---

## 📚 Documentation

Additional project documentation:

- [System Architecture](docs/ARCHITECTURE.md)
- [Database Model](docs/DATA_MODEL.md)
- [Payroll Engine](docs/PAYROLL_ENGINE.md)
- [Security](docs/SECURITY.md)
- [API Documentation](docs/API.md)
- [Portfolio Documentation](docs/PORTFOLIO.md)
- [Frontend Redesign](docs/FRONTEND_REDESIGN.md)

---

## ⚠️ Limitations

- Payroll tax rules are illustrative and not official statutory calculations.
- The public deployment is designed for demonstration purposes.
- Real employee and financial data should not be entered into the public demo.
- Production readiness requires additional security validation and operational controls.

---

## 👨‍💻 Author

**Nafiz Tonmoy**

- GitHub: [@Nafiztonmoy](https://github.com/Nafiztonmoy)
- Repository: [PayFlow HR](https://github.com/Nafiztonmoy/payflow-hr)

---

## 📄 License

This project is licensed under the MIT License.

See [LICENSE](LICENSE) for details.

---

⭐ **If you find this project useful, consider starring the repository!**

**Built with ASP.NET Core, React, TypeScript, PostgreSQL, and modern cloud deployment technologies.**
