import assert from "node:assert/strict";
import { JSDOM } from "jsdom";
const root = "http://localhost:5173";
const dom = new JSDOM(
  '<!doctype html><html><body><div id="root"></div></body></html>',
  { url: root + "/login", pretendToBeVisual: true },
);
for (const key of [
  "window",
  "document",
  "navigator",
  "HTMLElement",
  "HTMLInputElement",
  "HTMLSelectElement",
  "HTMLTextAreaElement",
  "MutationObserver",
  "localStorage",
  "Event",
  "MouseEvent",
  "KeyboardEvent",
  "PopStateEvent",
  "getComputedStyle",
])
  Object.defineProperty(globalThis, key, {
    value:
      key === "getComputedStyle"
        ? dom.window.getComputedStyle.bind(dom.window)
        : dom.window[key],
    configurable: true,
  });
globalThis.requestAnimationFrame = window.requestAnimationFrame.bind(window);
globalThis.cancelAnimationFrame = window.cancelAnimationFrame.bind(window);
window.scrollTo = () => {};
window.print = () => {};
HTMLElement.prototype.getBoundingClientRect = () => ({
  x: 0,
  y: 0,
  width: 600,
  height: 300,
  top: 0,
  left: 0,
  right: 600,
  bottom: 300,
  toJSON() {
    return {};
  },
});
globalThis.ResizeObserver = class {
  callback;
  constructor(callback) {
    this.callback = callback;
  }
  observe(target) {
    this.callback([{ target, contentRect: { width: 600, height: 300 } }]);
  }
  unobserve() {}
  disconnect() {}
};
globalThis.IS_REACT_ACT_ENVIRONMENT = true;
const errors = [];
window.addEventListener("error", (e) => errors.push(e.message));
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
globalThis.fetch = async (input, options = {}) => {
  const url = new URL(String(input), root);
  const method = options.method || "GET";
  const req = { postDataJSON: () => JSON.parse(options.body || "{}") };
  const path = url.pathname.slice("/api/v1".length);
  requests.push({
    path,
    query: url.search,
    method,
    body: method === "POST" ? req.postDataJSON() : null,
  });
  const send = (data, status = 200, type = "application/json") =>
    new Response(type === "application/json" ? JSON.stringify(data) : data, {
      status,
      headers: { "Content-Type": type },
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
};

const { render, screen, fireEvent, waitFor, act, cleanup } =
  await import("@testing-library/react");
const { default: userEvent } = await import("@testing-library/user-event");
const { App } = await import("../src/App");
const React = await import("react");
const user = userEvent.setup({ document });
const until = (fn) => waitFor(fn, { timeout: 5000 });
const go = async (path, title) => {
  await act(async () => {
    window.history.pushState({}, "", path);
    window.dispatchEvent(new PopStateEvent("popstate"));
  });
  if (title)
    await until(() => assert.ok(screen.getByRole("heading", { name: title })));
};
const click = async (name) => {
  await user.click(screen.getByRole("button", { name, exact: true }));
};
const checks = [];
render(React.createElement(App));
try {
  await until(() =>
    assert.ok(screen.getByRole("heading", { name: "Make yourself at work." })),
  );
  await user.click(screen.getByRole("button", { name: /^Admin/ }));
  await until(() =>
    assert.ok(
      screen.getByRole("heading", { name: "Your people. Your progress." }),
    ),
  );
  await until(() => assert.ok(screen.getByText("PR-2026-03-01")));
  checks.push("login and API-backed dashboard");
  for (const [path, title] of [
    ["/employees", "People"],
    ["/organization", "Built around your people."],
    ["/compensation-plans", "Salary structures"],
    ["/attendance", "Every hour, accounted for."],
    ["/leaves", "A little time for yourself."],
    ["/reports", "Clarity behind the numbers."],
    ["/audit-logs", "A clear trail of every change."],
    ["/payroll", "Payroll workspace"],
  ])
    await go(path, title);
  checks.push("all main routes render");
  await go("/compensation-plans", "Salary structures");
  await until(() => assert.ok(screen.getByText("Professional salary")));
  assert.equal(screen.queryByRole("tab", { name: "Departments" }), null);
  checks.push("salary route selects structures");
  await go("/employees", "People");
  await until(() => assert.ok(screen.getByText("Alex Carter")));
  await click("Next");
  await until(() => assert.ok(screen.getByText(/Page 2/)));
  await click("View Profile");
  await until(() => assert.ok(screen.getByRole("dialog")));
  fireEvent.keyDown(document, { key: "Escape" });
  await until(() => assert.equal(screen.queryByRole("dialog"), null));
  assert.ok(
    requests.some((r) => r.path === "/employees" && r.query.includes("page=2")),
  );
  await click("View Profile");
  await until(() => assert.ok(screen.getByRole("dialog")));
  await click("Compensation & Salary History");
  await until(() => assert.ok(screen.getByText("Professional salary")));
  await click("Assign Revision");
  await until(() => assert.equal(screen.getAllByRole("dialog").length, 2));
  fireEvent.change(screen.getByLabelText("Monthly Base Salary ($ USD)"), {
    target: { value: "6700" },
  });
  await click("Create Contract Revision");
  await until(() =>
    assert.ok(
      requests.some(
        (r) =>
          r.path === "/employees/emp-1/compensation" && r.method === "POST",
      ),
    ),
  );
  assert.equal(
    requests.findLast(
      (r) => r.path === "/employees/emp-1/compensation" && r.method === "POST",
    ).body.baseSalary,
    6700,
  );
  fireEvent.keyDown(document, { key: "Escape" });
  await until(() => assert.equal(screen.queryByRole("dialog"), null));
  checks.push("employee pagination, compensation revision and dialog escape");
  await go("/leaves", "A little time for yourself.");
  await until(() =>
    assert.ok(screen.getByRole("button", { name: "Reject", exact: true })),
  );
  await click("Reject");
  await user.type(
    screen.getByRole("textbox", { name: "Review note" }),
    "Please select another date.",
  );
  await click("Confirm rejection");
  await until(() => assert.equal(leaveStatus, "Rejected"));
  assert.equal(
    requests.findLast((r) => r.path.endsWith("/reject")).body.remarks,
    "Please select another date.",
  );
  checks.push("leave rejection uses entered note");
  await go("/payroll", "Payroll workspace");
  await until(() =>
    assert.ok(
      screen.getByRole("button", { name: "Approve run", exact: true }).disabled,
    ),
  );
  await user.click(screen.getByRole("tab", { name: /Exceptions/ }));
  await until(() =>
    assert.ok(screen.getByRole("button", { name: "Resolve with note" })),
  );
  await click("Resolve with note");
  await user.type(
    screen.getByRole("textbox", { name: "Resolution note" }),
    "Verified payment details with HR.",
  );
  await click("Save resolution");
  await until(() =>
    assert.equal(
      screen.getByRole("button", { name: "Approve run", exact: true }).disabled,
      false,
    ),
  );
  await click("Submit for review");
  await until(() => assert.equal(runStatus, "InReview"));
  failApprove = true;
  await click("Approve run");
  await until(() =>
    assert.ok(
      screen
        .getAllByRole("alert")
        .some((el) => el.textContent.includes("Concurrency conflict")),
    ),
  );
  failApprove = false;
  await click("Dismiss");
  await click("Approve run");
  await until(() =>
    assert.ok(
      screen.getByRole("button", { name: "Record payment", exact: true }),
    ),
  );
  assert.equal(
    screen.queryByRole("button", { name: "Calculate run", exact: true }),
    null,
  );
  await click("Record payment");
  assert.ok(screen.getByText(/does not initiate a bank transfer/));
  await user.type(
    screen.getByRole("textbox", { name: "Payment reference" }),
    "TEST-PAYMENT-001",
  );
  await click("Confirm payment recorded");
  await until(() => assert.equal(runStatus, "Paid"));
  checks.push(
    "payroll blockers, resolution, lifecycle, conflict recovery and immutable approval",
  );
  await go("/payslip/item-1", "Northstar Technologies");
  await until(() => assert.ok(screen.getByText("$5,250.00", { exact: true })));
  checks.push("payslip uses returned currency and values");
  for (const role of ["HR", "Manager", "Accountant", "Employee"]) {
    const marker = requests.length;
    await user.selectOptions(
      screen.getByRole("combobox", { name: "Demo persona" }),
      role.toLowerCase(),
    );
    await until(() => assert.equal(window.location.pathname, "/dashboard"));
    await until(() =>
      assert.equal(
        screen.getByRole("combobox", { name: "Demo persona" }).value,
        role.toLowerCase(),
      ),
    );
    if (role !== "Employee") {
      await go("/reports", "Clarity behind the numbers.");
      await until(() => assert.ok(requests.length > marker));
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
      await go("/attendance", "Every hour, accounted for.");
      await until(() => assert.ok(screen.getByText("480m")));
      assert.equal(
        screen.queryByRole("button", { name: "Correct Punch" }),
        null,
      );
      await go("/payroll", "This page belongs to another role");
    }
  }
  checks.push(
    "five role views, report query permissions and restricted routes",
  );
  loginFail = true;
  const savedToken = localStorage.getItem("payflow_token");
  await user.selectOptions(
    screen.getByRole("combobox", { name: "Demo persona" }),
    "admin",
  );
  await until(() =>
    assert.ok(
      screen
        .getAllByRole("alert")
        .some((el) => el.textContent.includes("Invalid demo credentials")),
    ),
  );
  assert.equal(localStorage.getItem("payflow_token"), savedToken);
  loginFail = false;
  await click("Dismiss error");
  checks.push("failed persona switch preserves the current session");
  await go("/leaves", "A little time for yourself.");
  await until(() =>
    assert.equal(
      screen.getByRole("button", { name: "Request time off", exact: true })
        .disabled,
      false,
    ),
  );
  await click("Request time off");
  await user.type(
    screen.getByLabelText("Reason for Request"),
    "Test time off request.",
  );
  fireEvent.change(screen.getByLabelText("End Date"), {
    target: { value: new Date(Date.now() + 3 * 86400000).toISOString().slice(0, 10) },
  });
  await click("Submit Request");
  await until(() =>
    assert.ok(
      requests.some((r) => r.path === "/leave-requests" && r.method === "POST"),
    ),
  );
  checks.push("employee leave submission");
  await user.selectOptions(
    screen.getByRole("combobox", { name: "Demo persona" }),
    "admin",
  );
  await until(() =>
    assert.equal(
      screen.getByRole("combobox", { name: "Demo persona" }).value,
      "admin",
    ),
  );
  failEmployees = true;
  await go("/employees", "People");
  await until(() =>
    assert.ok(
      screen
        .getAllByRole("alert")
        .some((el) =>
          el.textContent.includes("Directory temporarily unavailable"),
        ),
    ),
  );
  failEmployees = false;
  await click("Retry");
  await until(() => assert.ok(screen.getByText("Alex Carter")));
  checks.push("API failure and retry");
  await click("Sign out");
  await until(() =>
    assert.ok(screen.getByRole("heading", { name: "Make yourself at work." })),
  );
  empty = true;
  await user.click(screen.getByRole("button", { name: /^Admin/ }));
  await until(() =>
    assert.ok(
      screen.getByRole("heading", { name: "Your people. Your progress." }),
    ),
  );
  await go("/payroll", "Payroll workspace");
  await until(() =>
    assert.ok(
      screen.getByRole("heading", { name: "Your first cycle starts here" }),
    ),
  );
  checks.push("empty payroll state with no fabricated metrics");
  await click("Sign out");
  await until(() =>
    assert.ok(screen.getByRole("heading", { name: "Make yourself at work." })),
  );
  loginFail = true;
  await user.click(screen.getByRole("button", { name: /^Admin/ }));
  await until(() =>
    assert.ok(
      screen
        .getByRole("alert")
        .textContent.includes("Invalid demo credentials"),
    ),
  );
  checks.push("logout and failed login");
  assert.deepEqual(errors, []);
  console.log(
    JSON.stringify(
      {
        result: "PASS",
        scope:
          "React DOM tests with API fixtures; no browser layout or live backend verification",
        checks,
      },
      null,
      2,
    ),
  );
} finally {
  cleanup();
  dom.window.close();
}
process.exit(0);
