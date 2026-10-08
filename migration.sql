CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE "AuditLogs" (
    "Id" uuid NOT NULL,
    "ActorId" uuid,
    "ActorEmail" text NOT NULL,
    "ActorRole" text NOT NULL,
    "Action" character varying(100) NOT NULL,
    "EntityType" character varying(100) NOT NULL,
    "EntityId" text,
    "CorrelationId" text,
    "TimestampUtc" timestamp without time zone NOT NULL,
    "Summary" text NOT NULL,
    "BeforeValuesJson" text,
    "AfterValuesJson" text,
    "CreatedAtUtc" timestamp without time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAtUtc" timestamp without time zone,
    "UpdatedBy" text,
    CONSTRAINT "PK_AuditLogs" PRIMARY KEY ("Id")
);

CREATE TABLE "Designations" (
    "Id" uuid NOT NULL,
    "Title" character varying(100) NOT NULL,
    "Code" character varying(20) NOT NULL,
    "Level" integer NOT NULL,
    "Description" text,
    "IsActive" boolean NOT NULL,
    "CreatedAtUtc" timestamp without time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAtUtc" timestamp without time zone,
    "UpdatedBy" text,
    CONSTRAINT "PK_Designations" PRIMARY KEY ("Id")
);

CREATE TABLE "LeaveTypes" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Code" character varying(20) NOT NULL,
    "AnnualEntitlementDays" integer NOT NULL,
    "MaxCarryForwardDays" integer NOT NULL,
    "IsPaid" boolean NOT NULL,
    "IsActive" boolean NOT NULL,
    "Description" text,
    "CreatedAtUtc" timestamp without time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAtUtc" timestamp without time zone,
    "UpdatedBy" text,
    CONSTRAINT "PK_LeaveTypes" PRIMARY KEY ("Id")
);

CREATE TABLE "Organizations" (
    "Id" uuid NOT NULL,
    "Name" character varying(150) NOT NULL,
    "LegalName" text NOT NULL,
    "TaxIdentifier" text NOT NULL,
    "Timezone" text NOT NULL,
    "PayrollFrequency" integer NOT NULL,
    "Currency" character varying(10) NOT NULL,
    "WorkingDaysPerWeek" integer NOT NULL,
    "StandardHoursPerDay" numeric(18,2) NOT NULL,
    "Address" text NOT NULL,
    "ContactEmail" text NOT NULL,
    "Phone" text NOT NULL,
    "CreatedAtUtc" timestamp without time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAtUtc" timestamp without time zone,
    "UpdatedBy" text,
    CONSTRAINT "PK_Organizations" PRIMARY KEY ("Id")
);

CREATE TABLE "PayrollRuns" (
    "Id" uuid NOT NULL,
    "PeriodYear" integer NOT NULL,
    "PeriodMonth" integer NOT NULL,
    "PeriodStart" date NOT NULL,
    "PeriodEnd" date NOT NULL,
    "RunNumber" text NOT NULL,
    "Status" character varying(30) NOT NULL,
    "TotalGross" numeric(18,2) NOT NULL,
    "TotalTax" numeric(18,2) NOT NULL,
    "TotalDeductions" numeric(18,2) NOT NULL,
    "TotalNet" numeric(18,2) NOT NULL,
    "TotalEmployees" integer NOT NULL,
    "CalculatedAtUtc" timestamp without time zone,
    "ReviewedAtUtc" timestamp without time zone,
    "ApprovedAtUtc" timestamp without time zone,
    "PaidAtUtc" timestamp without time zone,
    "CreatedByUserId" uuid,
    "ReviewedByUserId" uuid,
    "ApprovedByUserId" uuid,
    "PaidByUserId" uuid,
    "PaymentReference" text,
    "ConcurrencyToken" uuid NOT NULL,
    "CreatedAtUtc" timestamp without time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAtUtc" timestamp without time zone,
    "UpdatedBy" text,
    CONSTRAINT "PK_PayrollRuns" PRIMARY KEY ("Id")
);

CREATE TABLE "SalaryStructures" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Code" character varying(50) NOT NULL,
    "Description" text,
    "Version" integer NOT NULL,
    "EffectiveFrom" timestamp without time zone NOT NULL,
    "EffectiveTo" timestamp without time zone,
    "IsActive" boolean NOT NULL,
    "CreatedAtUtc" timestamp without time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAtUtc" timestamp without time zone,
    "UpdatedBy" text,
    CONSTRAINT "PK_SalaryStructures" PRIMARY KEY ("Id")
);

CREATE TABLE "TaxRuleSets" (
    "Id" uuid NOT NULL,
    "JurisdictionLabel" text NOT NULL,
    "Version" integer NOT NULL,
    "Description" text NOT NULL,
    "Disclaimer" text NOT NULL,
    "EffectiveFrom" timestamp without time zone NOT NULL,
    "EffectiveTo" timestamp without time zone,
    "IsActive" boolean NOT NULL,
    "RulesJson" text NOT NULL,
    "CreatedAtUtc" timestamp without time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAtUtc" timestamp without time zone,
    "UpdatedBy" text,
    CONSTRAINT "PK_TaxRuleSets" PRIMARY KEY ("Id")
);

CREATE TABLE "PayComponents" (
    "Id" uuid NOT NULL,
    "SalaryStructureId" uuid NOT NULL,
    "Code" character varying(50) NOT NULL,
    "DisplayName" character varying(100) NOT NULL,
    "Type" character varying(30) NOT NULL,
    "CalculationMode" character varying(30) NOT NULL,
    "DefaultAmount" numeric(18,2) NOT NULL,
    "PercentageRate" numeric(18,2) NOT NULL,
    "IsTaxable" boolean NOT NULL,
    "IsActive" boolean NOT NULL,
    "DisplayOrder" integer NOT NULL,
    "CreatedAtUtc" timestamp without time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAtUtc" timestamp without time zone,
    "UpdatedBy" text,
    CONSTRAINT "PK_PayComponents" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PayComponents_SalaryStructures_SalaryStructureId" FOREIGN KEY ("SalaryStructureId") REFERENCES "SalaryStructures" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AttendanceRecords" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "Date" date NOT NULL,
    "Status" character varying(30) NOT NULL,
    "CheckInTimeUtc" timestamp without time zone,
    "CheckOutTimeUtc" timestamp without time zone,
    "WorkedMinutes" integer NOT NULL,
    "OvertimeMinutes" integer NOT NULL,
    "Source" character varying(30) NOT NULL,
    "CorrectionStatus" character varying(30) NOT NULL,
    "CorrectionReason" text,
    "ReviewedBy" text,
    "ReviewedAtUtc" timestamp without time zone,
    "CreatedAtUtc" timestamp without time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAtUtc" timestamp without time zone,
    "UpdatedBy" text,
    CONSTRAINT "PK_AttendanceRecords" PRIMARY KEY ("Id")
);

CREATE TABLE "Departments" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Code" character varying(20) NOT NULL,
    "Description" text,
    "ManagerEmployeeId" uuid,
    "IsActive" boolean NOT NULL,
    "CreatedAtUtc" timestamp without time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAtUtc" timestamp without time zone,
    "UpdatedBy" text,
    CONSTRAINT "PK_Departments" PRIMARY KEY ("Id")
);

CREATE TABLE "Employees" (
    "Id" uuid NOT NULL,
    "EmployeeNo" character varying(50) NOT NULL,
    "FirstName" character varying(100) NOT NULL,
    "LastName" character varying(100) NOT NULL,
    "WorkEmail" character varying(150) NOT NULL,
    "Phone" text,
    "DateOfBirth" timestamp without time zone NOT NULL,
    "JoinDate" timestamp without time zone NOT NULL,
    "TerminationDate" timestamp without time zone,
    "Status" character varying(30) NOT NULL,
    "DepartmentId" uuid NOT NULL,
    "DesignationId" uuid NOT NULL,
    "ManagerId" uuid,
    "PaymentMethod" character varying(30) NOT NULL,
    "BankName" text,
    "BankAccountNumber" text,
    "BankRoutingNumber" text,
    "CreatedAtUtc" timestamp without time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAtUtc" timestamp without time zone,
    "UpdatedBy" text,
    CONSTRAINT "PK_Employees" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Employees_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Employees_Designations_DesignationId" FOREIGN KEY ("DesignationId") REFERENCES "Designations" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Employees_Employees_ManagerId" FOREIGN KEY ("ManagerId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "EmployeeCompensations" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "SalaryStructureId" uuid NOT NULL,
    "BaseSalary" numeric(18,2) NOT NULL,
    "EffectiveFrom" timestamp without time zone NOT NULL,
    "EffectiveTo" timestamp without time zone,
    "IsActive" boolean NOT NULL,
    "Remarks" text,
    "CreatedAtUtc" timestamp without time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAtUtc" timestamp without time zone,
    "UpdatedBy" text,
    CONSTRAINT "PK_EmployeeCompensations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EmployeeCompensations_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_EmployeeCompensations_SalaryStructures_SalaryStructureId" FOREIGN KEY ("SalaryStructureId") REFERENCES "SalaryStructures" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "LeaveBalances" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "LeaveTypeId" uuid NOT NULL,
    "Year" integer NOT NULL,
    "OpeningBalance" numeric(18,2) NOT NULL,
    "AccruedDays" numeric(18,2) NOT NULL,
    "UsedDays" numeric(18,2) NOT NULL,
    "AdjustedDays" numeric(18,2) NOT NULL,
    "ConcurrencyToken" uuid NOT NULL,
    "CreatedAtUtc" timestamp without time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAtUtc" timestamp without time zone,
    "UpdatedBy" text,
    CONSTRAINT "PK_LeaveBalances" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_LeaveBalances_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_LeaveBalances_LeaveTypes_LeaveTypeId" FOREIGN KEY ("LeaveTypeId") REFERENCES "LeaveTypes" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "PayrollItems" (
    "Id" uuid NOT NULL,
    "PayrollRunId" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "BaseSalary" numeric(18,2) NOT NULL,
    "GrossPay" numeric(18,2) NOT NULL,
    "TaxableAmount" numeric(18,2) NOT NULL,
    "IncomeTax" numeric(18,2) NOT NULL,
    "TotalDeductions" numeric(18,2) NOT NULL,
    "NetPay" numeric(18,2) NOT NULL,
    "Status" character varying(30) NOT NULL,
    "CalculationSnapshotJson" text NOT NULL,
    "CalculationHash" text NOT NULL,
    "CreatedAtUtc" timestamp without time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAtUtc" timestamp without time zone,
    "UpdatedBy" text,
    CONSTRAINT "PK_PayrollItems" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PayrollItems_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_PayrollItems_PayrollRuns_PayrollRunId" FOREIGN KEY ("PayrollRunId") REFERENCES "PayrollRuns" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Users" (
    "Id" uuid NOT NULL,
    "UserName" character varying(100) NOT NULL,
    "Email" character varying(150) NOT NULL,
    "PasswordHash" text NOT NULL,
    "Role" character varying(50) NOT NULL,
    "EmployeeId" uuid,
    "IsActive" boolean NOT NULL,
    "RefreshToken" text,
    "RefreshTokenExpiryTimeUtc" timestamp without time zone,
    "LastLoginAtUtc" timestamp without time zone,
    "CreatedAtUtc" timestamp without time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAtUtc" timestamp without time zone,
    "UpdatedBy" text,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Users_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE SET NULL
);

CREATE TABLE "PayrollExceptions" (
    "Id" uuid NOT NULL,
    "PayrollRunId" uuid NOT NULL,
    "PayrollItemId" uuid,
    "EmployeeId" uuid NOT NULL,
    "Severity" character varying(30) NOT NULL,
    "ExceptionType" text NOT NULL,
    "Message" text NOT NULL,
    "IsResolved" boolean NOT NULL,
    "ResolvedAtUtc" timestamp without time zone,
    "ResolvedByUserId" uuid,
    "ResolutionNotes" text,
    "CreatedAtUtc" timestamp without time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAtUtc" timestamp without time zone,
    "UpdatedBy" text,
    CONSTRAINT "PK_PayrollExceptions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PayrollExceptions_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_PayrollExceptions_PayrollItems_PayrollItemId" FOREIGN KEY ("PayrollItemId") REFERENCES "PayrollItems" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_PayrollExceptions_PayrollRuns_PayrollRunId" FOREIGN KEY ("PayrollRunId") REFERENCES "PayrollRuns" ("Id") ON DELETE CASCADE
);

CREATE TABLE "PayrollItemComponents" (
    "Id" uuid NOT NULL,
    "PayrollItemId" uuid NOT NULL,
    "ComponentCode" text NOT NULL,
    "ComponentName" text NOT NULL,
    "ComponentType" character varying(30) NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "SourceRule" text NOT NULL,
    "Explanation" text NOT NULL,
    "CreatedAtUtc" timestamp without time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAtUtc" timestamp without time zone,
    "UpdatedBy" text,
    CONSTRAINT "PK_PayrollItemComponents" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PayrollItemComponents_PayrollItems_PayrollItemId" FOREIGN KEY ("PayrollItemId") REFERENCES "PayrollItems" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Payslips" (
    "Id" uuid NOT NULL,
    "PayrollItemId" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "PayslipNumber" text NOT NULL,
    "IssueDateUtc" timestamp without time zone NOT NULL,
    "SnapshotJson" text NOT NULL,
    "CreatedAtUtc" timestamp without time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAtUtc" timestamp without time zone,
    "UpdatedBy" text,
    CONSTRAINT "PK_Payslips" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Payslips_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Payslips_PayrollItems_PayrollItemId" FOREIGN KEY ("PayrollItemId") REFERENCES "PayrollItems" ("Id") ON DELETE CASCADE
);

CREATE TABLE "LeaveRequests" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "LeaveTypeId" uuid NOT NULL,
    "StartDate" date NOT NULL,
    "EndDate" date NOT NULL,
    "DayCount" numeric(18,2) NOT NULL,
    "Reason" text NOT NULL,
    "Status" character varying(30) NOT NULL,
    "ReviewedByUserId" uuid,
    "ReviewedAtUtc" timestamp without time zone,
    "ReviewRemarks" text,
    "CreatedAtUtc" timestamp without time zone NOT NULL,
    "CreatedBy" text,
    "UpdatedAtUtc" timestamp without time zone,
    "UpdatedBy" text,
    CONSTRAINT "PK_LeaveRequests" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_LeaveRequests_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_LeaveRequests_LeaveTypes_LeaveTypeId" FOREIGN KEY ("LeaveTypeId") REFERENCES "LeaveTypes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_LeaveRequests_Users_ReviewedByUserId" FOREIGN KEY ("ReviewedByUserId") REFERENCES "Users" ("Id") ON DELETE SET NULL
);

CREATE UNIQUE INDEX "IX_AttendanceRecords_EmployeeId_Date" ON "AttendanceRecords" ("EmployeeId", "Date");

CREATE INDEX "IX_AuditLogs_CorrelationId" ON "AuditLogs" ("CorrelationId");

CREATE INDEX "IX_AuditLogs_EntityType_EntityId" ON "AuditLogs" ("EntityType", "EntityId");

CREATE INDEX "IX_AuditLogs_TimestampUtc" ON "AuditLogs" ("TimestampUtc");

CREATE UNIQUE INDEX "IX_Departments_Code" ON "Departments" ("Code");

CREATE INDEX "IX_Departments_ManagerEmployeeId" ON "Departments" ("ManagerEmployeeId");

CREATE UNIQUE INDEX "IX_Designations_Code" ON "Designations" ("Code");

CREATE INDEX "IX_EmployeeCompensations_EmployeeId_EffectiveFrom" ON "EmployeeCompensations" ("EmployeeId", "EffectiveFrom");

CREATE INDEX "IX_EmployeeCompensations_SalaryStructureId" ON "EmployeeCompensations" ("SalaryStructureId");

CREATE INDEX "IX_Employees_DepartmentId" ON "Employees" ("DepartmentId");

CREATE INDEX "IX_Employees_DesignationId" ON "Employees" ("DesignationId");

CREATE UNIQUE INDEX "IX_Employees_EmployeeNo" ON "Employees" ("EmployeeNo");

CREATE INDEX "IX_Employees_ManagerId" ON "Employees" ("ManagerId");

CREATE INDEX "IX_Employees_Status" ON "Employees" ("Status");

CREATE UNIQUE INDEX "IX_Employees_WorkEmail" ON "Employees" ("WorkEmail");

CREATE UNIQUE INDEX "IX_LeaveBalances_EmployeeId_LeaveTypeId_Year" ON "LeaveBalances" ("EmployeeId", "LeaveTypeId", "Year");

CREATE INDEX "IX_LeaveBalances_LeaveTypeId" ON "LeaveBalances" ("LeaveTypeId");

CREATE INDEX "IX_LeaveRequests_EmployeeId_Status" ON "LeaveRequests" ("EmployeeId", "Status");

CREATE INDEX "IX_LeaveRequests_LeaveTypeId" ON "LeaveRequests" ("LeaveTypeId");

CREATE INDEX "IX_LeaveRequests_ReviewedByUserId" ON "LeaveRequests" ("ReviewedByUserId");

CREATE UNIQUE INDEX "IX_LeaveTypes_Code" ON "LeaveTypes" ("Code");

CREATE INDEX "IX_PayComponents_SalaryStructureId" ON "PayComponents" ("SalaryStructureId");

CREATE INDEX "IX_PayrollExceptions_EmployeeId" ON "PayrollExceptions" ("EmployeeId");

CREATE INDEX "IX_PayrollExceptions_PayrollItemId" ON "PayrollExceptions" ("PayrollItemId");

CREATE INDEX "IX_PayrollExceptions_PayrollRunId_IsResolved" ON "PayrollExceptions" ("PayrollRunId", "IsResolved");

CREATE INDEX "IX_PayrollItemComponents_PayrollItemId" ON "PayrollItemComponents" ("PayrollItemId");

CREATE INDEX "IX_PayrollItems_EmployeeId" ON "PayrollItems" ("EmployeeId");

CREATE UNIQUE INDEX "IX_PayrollItems_PayrollRunId_EmployeeId" ON "PayrollItems" ("PayrollRunId", "EmployeeId");

CREATE UNIQUE INDEX "IX_PayrollRuns_PeriodYear_PeriodMonth_RunNumber" ON "PayrollRuns" ("PeriodYear", "PeriodMonth", "RunNumber");

CREATE INDEX "IX_PayrollRuns_Status" ON "PayrollRuns" ("Status");

CREATE INDEX "IX_Payslips_EmployeeId" ON "Payslips" ("EmployeeId");

CREATE UNIQUE INDEX "IX_Payslips_PayrollItemId" ON "Payslips" ("PayrollItemId");

CREATE UNIQUE INDEX "IX_Payslips_PayslipNumber" ON "Payslips" ("PayslipNumber");

CREATE INDEX "IX_SalaryStructures_Code" ON "SalaryStructures" ("Code");

CREATE INDEX "IX_TaxRuleSets_JurisdictionLabel_Version" ON "TaxRuleSets" ("JurisdictionLabel", "Version");

CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");

CREATE UNIQUE INDEX "IX_Users_EmployeeId" ON "Users" ("EmployeeId");

CREATE UNIQUE INDEX "IX_Users_UserName" ON "Users" ("UserName");

ALTER TABLE "AttendanceRecords" ADD CONSTRAINT "FK_AttendanceRecords_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE;

ALTER TABLE "Departments" ADD CONSTRAINT "FK_Departments_Employees_ManagerEmployeeId" FOREIGN KEY ("ManagerEmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261008140228_InitialCreate', '10.0.12');

COMMIT;

