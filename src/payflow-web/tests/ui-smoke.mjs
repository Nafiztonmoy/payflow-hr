import assert from "node:assert/strict";
import fs from "node:fs/promises";
import { chromium } from "playwright";
const root = process.env.PAYFLOW_URL || "http://127.0.0.1:5173";
const browser = await chromium.launch({
  headless: true,
  ...(process.env.PAYFLOW_CHROMIUM_PATH
    ? {
        executablePath: process.env.PAYFLOW_CHROMIUM_PATH,
        args: [
          "--no-sandbox",
          "--disable-dev-shm-usage",
          "--no-zygote",
          "--single-process",
          "--disable-gpu",
        ],
      }
    : {}),
});
const context = await browser.newContext({
  viewport: { width: 1440, height: 1000 },
  acceptDownloads: true,
});
const page = await context.newPage();
const runtimeErrors = [];
page.on("pageerror", (e) => runtimeErrors.push(e.message));
const requests = [];
let activeRole = "Admin";
let runStatus = "Calculated";
let blocking = 1;
let leaveStatus = "Pending";
let failEmployees = false;
let empty = false;
let loginFail = false;
let failApprove = false;
const run = () => ({
  id: "run-1",
  year: 2026,
  month: 3,
  runNumber: "PR-2026-03-01",
  status: runStatus,
  totalGross: 12500,
  totalNet: 10500,
  totalTax: 1600,
  totalDeductions: 2000,
  totalEmployees: 2,
  blockingCount: blocking,
  warningCount: 0,
  concurrencyToken: "token-1",
});
const emp = {
  id: "emp-1",
  employeeNo: "EMP-001",
  fullName: "Alex Carter",
  workEmail: "alex@example.test",
  departmentName: "Engineering",
  designationTitle: "Software engineer",
  managerName: "David Miller",
  status: "Active",
  joinDate: "2024-01-01",
};
const departments = [
  {
    id: "dep-1",
    name: "Engineering",
    code: "ENG",
    employeeCount: 2,
    managerName: "David Miller",
    isActive: true,
    description: "Build and maintain our products.",
  },
];
const structures = [
  {
    id: "salary-1",
    name: "Professional salary",
    code: "PRO",
    version: 1,
    effectiveFrom: "2026-01-01",
    employeeCount: 2,
    isActive: true,
    components: [
      {
        id: "component-1",
        displayName: "Base salary",
        type: "Earning",
        calculationMode: "Fixed",
        defaultAmount: 6000,
        percentageRate: 0,
        isTaxable: true,
      },
    ],
  },
];
const leave = () => ({
  id: "leave-1",
  employeeId: "emp-1",
  employeeNo: "EMP-001",
  employeeName: "Alex Carter",
  departmentName: "Engineering",
  leaveTypeId: "type-1",
  leaveTypeName: "Annual leave",
  startDate: "2026-10-12",
  endDate: "2026-10-13",
  dayCount: 2,
  reason: "Personal time",
  status: leaveStatus,
});
const balances = [
  {
    id: "balance-1",
    leaveTypeId: "type-1",
    leaveTypeName: "Annual leave",
    leaveTypeCode: "ANNUAL",
    isPaid: true,
    year: 2026,
    openingBalance: 20,
    accruedDays: 0,
    usedDays: 2,
    adjustedDays: 0,
    remainingDays: 18,
  },
];
const item = {
  id: "item-1",
  employeeId: "emp-1",
  employeeNo: "EMP-001",
  employeeName: "Alex Carter",
  departmentName: "Engineering",
  designationTitle: "Software engineer",
  baseSalary: 6000,
  grossPay: 6250,
  taxableAmount: 6250,
  incomeTax: 800,
  totalDeductions: 1000,
  netPay: 5250,
  status: "Ok",
  calculationHash: "a".repeat(64),
  components: [],
  exceptions: [],
};
const slips = [
  {
    id: "slip-1",
    payrollItemId: "item-1",
    payslipNumber: "PS-2026-03-001",
    year: 2026,
    month: 3,
    issueDateUtc: "2026-04-01",
    grossPay: 6250,
    totalDeductions: 1000,
    netPay: 5250,
  },
];
await context.route("**/api/v1/**", async (route) => {
  const req = route.request(),
    url = new URL(req.url()),
    path = url.pathname.slice("/api/v1".length),
    method = req.method();
  requests.push({
    path,
    method,
    body: method === "POST" ? req.postDataJSON() : null,
  });
  const send = (data, status = 200, type = "application/json") =>
    route.fulfill({
      status,
      contentType: type,
      body: type === "application/json" ? JSON.stringify(data) : data,
    });
  if (path === "/auth/login") {
    if (loginFail) return send({ detail: "Invalid demo credentials" }, 401);
    const email = req.postDataJSON().emailOrUsername;
    activeRole = email.startsWith("admin")
      ? "Admin"
      : email.startsWith("hr")
        ? "HR"
        : email.startsWith("manager")
          ? "Manager"
          : email.startsWith("accountant")
            ? "Accountant"
            : "Employee";
    return send({
      accessToken: `test-${activeRole}`,
      refreshToken: "test-only",
      user: {
        id: `user-${activeRole}`,
        userName:
          activeRole === "Admin"
            ? "Admin"
            : activeRole === "Employee"
              ? "Alex Carter"
              : activeRole,
        email,
        role: activeRole,
        employeeId: activeRole === "Admin" ? undefined : "emp-1",
      },
    });
  }
  if (path === "/auth/logout") return send({ message: "Signed out" });
  if (path === "/departments") return send(empty ? [] : departments);
  if (path === "/designations")
    return send(
      empty
        ? []
        : [
            {
              id: "job-1",
              title: "Software engineer",
              code: "SWE",
              level: 3,
              employeeCount: 2,
            },
          ],
    );
  if (path === "/salary-structures") return send(empty ? [] : structures);
  if (path === "/employees")
    return failEmployees
      ? send({ detail: "Directory temporarily unavailable" }, 503)
      : send({
          total: empty ? 0 : 31,
          page: Number(url.searchParams.get("page") || 1),
          pageSize: Number(url.searchParams.get("pageSize") || 30),
          items: empty ? [] : [emp],
        });
  if (path === "/employees/emp-1")
    return send({
      ...emp,
      firstName: "Alex",
      lastName: "Carter",
      dateOfBirth: "1995-01-01",
      departmentId: "dep-1",
      designationId: "job-1",
      paymentMethod: "BankTransfer",
    });
  if (path === "/employees/emp-1/compensation")
    return method === "POST"
      ? send({ message: "Saved", id: "comp-1" })
      : send([
          {
            id: "comp-1",
            salaryStructureId: "salary-1",
            salaryStructureName: "Professional salary",
            baseSalary: 6000,
            effectiveFrom: "2026-01-01",
            isActive: true,
            components: [],
          },
        ]);
  if (path === "/leave-balances/me") return send(empty ? [] : balances);
  if (path === "/leave-requests")
    return method === "POST" ? send(leave()) : send(empty ? [] : [leave()]);
  if (path.includes("/leave-requests/") && method === "POST") {
    leaveStatus = path.endsWith("/approve") ? "Approved" : "Rejected";
    return send({ message: "Reviewed", id: "leave-1" });
  }
  if (path === "/attendance")
    return send(
      empty
        ? []
        : [
            {
              id: "att-1",
              employeeId: "emp-1",
              employeeNo: "EMP-001",
              employeeName: "Alex Carter",
              date: "2026-10-01",
              status: "Present",
              workedMinutes: 480,
              overtimeMinutes: 60,
              source: "Manual",
              correctionStatus: "None",
            },
          ],
    );
  if (path === "/attendance/att-1/correct")
    return send({ message: "Corrected", id: "att-1" });
  if (path === "/payroll-runs") {
    if (method === "POST") {
      runStatus = "Draft";
      blocking = 0;
      return send(run());
    }
    return send(empty ? [] : [run()]);
  }
  if (path.endsWith("/items")) return send(empty ? [] : [item]);
  if (path.endsWith("/exceptions"))
    return send(
      empty
        ? []
        : [
            {
              id: "exception-1",
              employeeId: "emp-1",
              employeeName: "Alex Carter",
              severity: "Blocking",
              exceptionType: "MISSING_BANK_ACCOUNT",
              message: "Bank account details are missing.",
              isResolved: blocking === 0,
            },
          ],
    );
  if (path === "/payroll-runs/exceptions/exception-1/resolve") {
    blocking = 0;
    return send({ id: "exception-1", isResolved: true });
  }
  if (path === "/payroll-runs/run-1/export-csv")
    return send("Employee,Net\nAlex Carter,5250", 200, "text/csv");
  if (path.startsWith("/payroll-runs/run-1/") && method === "POST") {
    if (path.endsWith("/calculate")) runStatus = "Calculated";
    if (path.endsWith("/submit-review")) runStatus = "InReview";
    if (path.endsWith("/approve")) {
      if (failApprove)
        return send({ detail: "Concurrency conflict. Reload the run." }, 409);
      runStatus = "Approved";
    }
    if (path.endsWith("/mark-paid")) runStatus = "Paid";
    return send(run());
  }
  if (path === "/payroll-items/my-payslips") return send(empty ? [] : slips);
  if (path === "/payroll-items/item-1/payslip")
    return send({
      ...item,
      payslipNumber: "PS-2026-03-001",
      organizationName: "Northstar Technologies",
      currency: "USD",
      periodYear: 2026,
      periodMonth: 3,
      department: "Engineering",
      designation: "Software engineer",
      employeeName: "Alex Carter",
      paymentMethod: "Bank transfer",
      maskedBankAccount: "•••• 1234",
      earnings: [
        {
          componentCode: "BASE",
          componentName: "Base salary",
          amount: 6250,
          explanation: "Monthly salary",
        },
      ],
      deductions: [
        {
          componentCode: "TAX",
          componentName: "Income tax",
          amount: 1000,
          explanation: "Withheld from earnings",
        },
      ],
      issueDateUtc: "2026-04-01",
    });
  if (path === "/reports/payroll-summary")
    return send(empty ? [] : [{ ...run(), period: "2026-03" }]);
  if (path === "/reports/labor-cost")
    return send({
      year: 2026,
      departments: empty
        ? []
        : [
            {
              department: "Engineering",
              totalGross: 12500,
              totalNet: 10500,
              employeeCount: 2,
            },
          ],
    });
  if (path === "/reports/leave")
    return send({
      year: 2026,
      utilization: empty
        ? []
        : [
            {
              department: "Engineering",
              leaveType: "Annual leave",
              totalOpening: 40,
              totalUsed: 4,
              totalRemaining: 36,
            },
          ],
    });
  if (path === "/audit-logs")
    return send({
      total: 1,
      page: 1,
      pageSize: 20,
      items: empty
        ? []
        : [
            {
              id: "log-1",
              timestampUtc: "2026-10-01T10:00:00Z",
              actorEmail: "admin@example.test",
              actorRole: "Admin",
              action: "APPROVE",
              entityType: "PayrollRun",
              entityId: "run-1",
              summary: "Approved March payroll",
              correlationId: "trace-1",
              afterValuesJson: '{"status":"Approved"}',
            },
          ],
    });
  throw new Error(`Unhandled fixture ${method} ${path}`);
});
const wait = () => page.waitForLoadState("networkidle");
const go = async (path) => {
  await page.goto(root + path);
  await wait();
};
const checkWidth = async () => {
  const dimensions = await page.evaluate(() => ({
    w: innerWidth,
    scroll: document.documentElement.scrollWidth,
  }));
  assert.ok(
    dimensions.scroll <= dimensions.w + 1,
    `Page overflow ${JSON.stringify(dimensions)}`,
  );
};
await fs.mkdir("docs/screenshots", { recursive: true });
try {
  await go("/login");
  assert.equal(
    await page.getByRole("heading", { name: "Make yourself at work." }).count(),
    1,
  );
  await page.screenshot({
    path: "docs/screenshots/login-desktop.png",
    fullPage: true,
  });
  await page.getByRole("button", { name: "Admin", exact: false }).click();
  await wait();
  assert.equal(
    await page
      .getByRole("heading", { name: "Your people. Your progress." })
      .count(),
    1,
  );
  await page.screenshot({
    path: "docs/screenshots/dashboard-desktop.png",
    fullPage: true,
  });
  await checkWidth();
  for (const path of [
    "/employees",
    "/organization",
    "/compensation-plans",
    "/attendance",
    "/leaves",
    "/reports",
    "/audit-logs",
    "/payroll",
  ]) {
    await go(path);
    assert.equal(await page.locator("h1").count(), 1, `${path} heading`);
    await checkWidth();
    await page.screenshot({
      path: `docs/screenshots/${path.slice(1)}-desktop.png`,
      fullPage: true,
    });
  }
  await go("/compensation-plans");
  assert.ok(
    await page.getByText("Professional salary", { exact: true }).isVisible(),
  );
  assert.equal(await page.getByRole("tab", { name: "Departments" }).count(), 0);
  await go("/employees");
  await page.getByRole("button", { name: "Next", exact: true }).click();
  await wait();
  assert.ok(requests.some((r) => r.path === "/employees"));
  await page.getByRole("button", { name: "View Profile" }).click();
  await wait();
  assert.equal(await page.getByRole("dialog").count(), 1);
  await page.keyboard.press("Escape");
  assert.equal(await page.getByRole("dialog").count(), 0);
  await go("/leaves");
  await page.getByRole("button", { name: "Reject", exact: true }).click();
  await page
    .getByRole("textbox", { name: "Review note" })
    .fill("Please select another date.");
  await page.getByRole("button", { name: "Confirm rejection" }).click();
  await wait();
  assert.equal(
    requests.findLast((r) => r.path.endsWith("/reject")).body.remarks,
    "Please select another date.",
  );
  await go("/payroll");
  assert.equal(
    await page
      .getByRole("button", { name: "Approve run", exact: true })
      .isDisabled(),
    true,
  );
  await page.getByRole("tab", { name: /Exceptions/ }).click();
  await page.getByRole("button", { name: "Resolve with note" }).click();
  await page
    .getByRole("textbox", { name: "Resolution note" })
    .fill("Verified payment details with HR.");
  await page.getByRole("button", { name: "Save resolution" }).click();
  await wait();
  assert.equal(
    await page
      .getByRole("button", { name: "Approve run", exact: true })
      .isDisabled(),
    false,
  );
  await page.getByRole("button", { name: "Submit for review" }).click();
  await wait();
  failApprove = true;
  await page.getByRole("button", { name: "Approve run", exact: true }).click();
  await wait();
  assert.ok(
    await page
      .getByRole("alert")
      .filter({ hasText: "Concurrency conflict" })
      .isVisible(),
  );
  failApprove = false;
  await page.getByRole("button", { name: "Dismiss", exact: true }).click();
  await page.getByRole("button", { name: "Approve run", exact: true }).click();
  await wait();
  assert.equal(
    await page
      .getByRole("button", { name: "Calculate run", exact: true })
      .count(),
    0,
  );
  await page
    .getByRole("button", { name: "Record payment", exact: true })
    .click();
  assert.ok(
    await page.getByText(/does not initiate a bank transfer/).isVisible(),
  );
  await page
    .getByRole("textbox", { name: "Payment reference" })
    .fill("TEST-PAYMENT-001");
  await page.getByRole("button", { name: "Confirm payment recorded" }).click();
  await wait();
  assert.equal(runStatus, "Paid");
  const downloadPromise = page.waitForEvent("download");
  await page.getByRole("button", { name: "Export CSV", exact: true }).click();
  const download = await downloadPromise;
  assert.ok(download.suggestedFilename().endsWith(".csv"));
  await go("/payslip/item-1");
  assert.ok(await page.getByText("$5,250.00", { exact: true }).isVisible());
  await page.emulateMedia({ media: "print" });
  assert.equal(await page.locator(".sidebar").isVisible(), false);
  await page.screenshot({
    path: "docs/screenshots/payslip-print.png",
    fullPage: true,
  });
  await page.emulateMedia({ media: "screen" });
  // Role query boundaries and cache isolation after switching users.
  for (const role of ["HR", "Manager", "Accountant", "Employee"]) {
    const marker = requests.length;
    await page
      .getByRole("combobox", { name: "Demo persona" })
      .selectOption(role.toLowerCase());
    await wait();
    if (role !== "Employee") {
      await go("/reports");
      const recent = requests.slice(marker).map((r) => r.path);
      if (role === "Manager")
        assert.ok(
          !recent.includes("/reports/payroll-summary") &&
            !recent.includes("/reports/labor-cost"),
        );
      if (role === "HR")
        assert.ok(!recent.includes("/reports/payroll-summary"));
      if (role === "Accountant") assert.ok(!recent.includes("/reports/leave"));
    } else {
      await go("/my-payslips");
      assert.ok(
        await page.getByText("March 2026", { exact: true }).isVisible(),
      );
      await go("/attendance");
      assert.equal(
        await page.getByRole("button", { name: "Correct Punch" }).count(),
        0,
      );
      await go("/payroll");
      assert.ok(
        await page
          .getByRole("heading", { name: "This page belongs to another role" })
          .isVisible(),
      );
    }
  }
  await go("/leaves");
  await page
    .getByRole("button", { name: "Request time off", exact: true })
    .click();
  await page.getByLabel("Reason for Request").fill("Test time off request.");
  await page.getByLabel("End Date").fill(new Date(Date.now() + 3 * 86400000).toISOString().slice(0, 10));
  await page
    .getByRole("button", { name: "Submit Request", exact: true })
    .click();
  await wait();
  assert.ok(
    requests.some((r) => r.path === "/leave-requests" && r.method === "POST"),
  );
  await page
    .getByRole("combobox", { name: "Demo persona" })
    .selectOption("admin");
  await wait();
  failEmployees = true;
  await go("/employees");
  assert.ok(
    await page
      .getByRole("alert")
      .filter({ hasText: "Directory temporarily unavailable" })
      .isVisible(),
  );
  failEmployees = false;
  await page.getByRole("button", { name: "Retry", exact: true }).click();
  await wait();
  assert.ok(
    await page.getByText("Alex Carter", { exact: true }).first().isVisible(),
  );
  empty = true;
  await go("/payroll");
  assert.ok(
    await page
      .getByRole("heading", { name: "Your first cycle starts here" })
      .isVisible(),
  );
  await go("/dashboard");
  assert.equal(await page.getByText("25", { exact: true }).count(), 0);
  empty = false;
  // Responsive pages and drawer keyboard behavior.
  for (const width of [375, 768, 1024]) {
    await page.setViewportSize({ width, height: 900 });
    for (const path of [
      "/dashboard",
      "/employees",
      "/organization",
      "/compensation-plans",
      "/attendance",
      "/leaves",
      "/reports",
      "/audit-logs",
      "/payroll",
    ]) {
      await go(path);
      await checkWidth();
    }
    if (width === 375) {
      await go("/dashboard");
      await page
        .getByRole("button", { name: "Open navigation", exact: true })
        .click();
      assert.ok(await page.locator(".sidebar").isVisible());
      await page.keyboard.press("Escape");
      assert.ok(
        !(await page
          .locator(".sidebar")
          .evaluate((el) => el.classList.contains("open"))),
      );
      await page.screenshot({
        path: "docs/screenshots/dashboard-mobile.png",
        fullPage: true,
      });
    }
  }
  await page.setViewportSize({ width: 1440, height: 1000 });
  await page.getByRole("button", { name: "Sign out", exact: true }).click();
  await wait();
  assert.ok(page.url().endsWith("/login"));
  loginFail = true;
  await page.getByRole("button", { name: "Admin", exact: false }).click();
  await wait();
  assert.ok(
    await page
      .getByRole("alert")
      .filter({ hasText: "Invalid demo credentials" })
      .isVisible(),
  );
  assert.deepEqual(runtimeErrors, []);
  console.log(
    JSON.stringify(
      {
        result: "PASS",
        scope:
          "Frontend with mocked API fixtures; not live backend integration",
        routes: 11,
        roles: 5,
        widths: [375, 768, 1024, 1440],
        checks: [
          "navigation",
          "query permissions",
          "cache isolation",
          "employee pagination and dialog",
          "leave rejection and request",
          "payroll exceptions and lifecycle",
          "mutation failure recovery",
          "authenticated CSV download",
          "payslip print",
          "API retry and empty states",
          "mobile drawer",
          "login and logout",
        ],
        runtimeErrors,
      },
      null,
      2,
    ),
  );
} finally {
  await browser.close();
}
