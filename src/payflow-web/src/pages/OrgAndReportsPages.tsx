import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useLocation } from "react-router-dom";
import { orgApi, reportsApi } from "../api/commonApis";
import { useAuth } from "../context/auth";
import { Badge } from "../components/Badge";
import {
  PageHeader,
  Metric,
  Panel,
  EmptyState,
  QueryState,
} from "../components/ui/Workspace";
import { money } from "../utils/format";
import { Building2, Users, Layers, Coins, CalendarDays } from "lucide-react";
import {
  ResponsiveContainer,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
} from "recharts";

type Department = {
  id: string;
  name: string;
  code: string;
  description?: string;
  employeeCount: number;
  managerName?: string;
  isActive: boolean;
};
type Designation = {
  id: string;
  title: string;
  code: string;
  level: number;
  description?: string;
  employeeCount: number;
};
type Structure = {
  id: string;
  name: string;
  code: string;
  version: number;
  description?: string;
  employeeCount: number;
  effectiveFrom: string;
  isActive: boolean;
  components: {
    id: string;
    displayName: string;
    type: string;
    calculationMode: string;
    defaultAmount: number;
    percentageRate: number;
    isTaxable: boolean;
  }[];
};
export function OrganizationPage() {
  const { pathname } = useLocation();
  const { user } = useAuth();
  const salaryRoute = pathname === "/compensation-plans";
  const [selectedTab, setTab] = useState<
    "departments" | "designations" | "structures"
  >("departments");
  const tab = salaryRoute ? "structures" : selectedTab;
  const departments = useQuery({
    queryKey: ["departments"],
    queryFn: orgApi.getDepartments,
  });
  const designations = useQuery({
    queryKey: ["designations"],
    queryFn: orgApi.getDesignations,
    enabled: !salaryRoute,
  });
  const structures = useQuery({
    queryKey: ["salary-structures"],
    queryFn: orgApi.getSalaryStructures,
  });
  const query =
    tab === "departments"
      ? departments
      : tab === "designations"
        ? designations
        : structures;
  return (
    <div>
      <PageHeader
        eyebrow="Workforce / Organization"
        title={salaryRoute ? "Salary structures" : "Built around your people."}
        description={
          salaryRoute
            ? "Explore pay components and versioned salary templates."
            : "A shared view of departments, roles and compensation frameworks."
        }
      />
      <div className="metrics-grid">
        <Metric
          label="Departments"
          value={
            departments.isPending
              ? "…"
              : departments.isError
                ? "—"
                : (departments.data?.length ?? 0)
          }
          detail="Organization units"
          icon={<Building2 size={18} />}
        />
        <Metric
          label="Employees"
          value={
            departments.isPending
              ? "…"
              : departments.isError
                ? "—"
                : (departments.data?.reduce(
                    (n: number, d: Department) => n + d.employeeCount,
                    0,
                  ) ?? 0)
          }
          detail="Across listed departments"
          icon={<Users size={18} />}
        />
        <Metric
          label="Salary structures"
          value={
            structures.isPending
              ? "…"
              : structures.isError
                ? "—"
                : (structures.data?.length ?? 0)
          }
          detail="Versioned compensation plans"
          icon={<Layers size={18} />}
        />
        <Metric
          label="Your access"
          value={user?.role}
          detail="Actions follow assigned permissions"
          icon={<Coins size={18} />}
        />
      </div>
      {!salaryRoute && (
        <div
          className="flex gap-2 mb-5 border-b border-slate-200 pb-3"
          role="tablist"
          aria-label="Organization catalogs"
        >
          {(["departments", "designations", "structures"] as const).map((t) => (
            <button
              role="tab"
              aria-selected={tab === t}
              key={t}
              onClick={() => setTab(t)}
              className={`btn ${tab === t ? "btn-primary" : "btn-secondary"}`}
            >
              {t === "structures"
                ? "Salary structures"
                : t[0].toUpperCase() + t.slice(1)}
            </button>
          ))}
        </div>
      )}
      <QueryState
        loading={query.isPending}
        error={query.error}
        retry={() => void query.refetch()}
      />
      {query.data?.length ? (
        tab === "departments" ? (
          <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-5">
            {(departments.data as Department[]).map((d) => (
              <section className="panel p-6" key={d.id}>
                <div className="flex justify-between items-center mb-5">
                  <span className="empty-icon mb-0">
                    <Building2 size={22} />
                  </span>
                  <Badge status={d.isActive ? "Active" : "Inactive"} />
                </div>
                <p className="eyebrow">{d.code}</p>
                <h2 className="text-lg font-semibold">{d.name}</h2>
                <p className="text-xs text-slate-500 mt-2 leading-relaxed min-h-10">
                  {d.description || "Organization department"}
                </p>
                <div className="border-t border-slate-100 pt-4 mt-5 flex justify-between text-xs">
                  <span>{d.employeeCount} employees</span>
                  <span className="text-slate-500">
                    {d.managerName || "Manager unassigned"}
                  </span>
                </div>
              </section>
            ))}
          </div>
        ) : tab === "designations" ? (
          <Panel
            title="Roles & designations"
            subtitle="The structure behind each employee's role."
          >
            <div className="overflow-x-auto">
              <table className="w-full text-left">
                <thead>
                  <tr>
                    <th className="px-6">Designation</th>
                    <th className="px-4">Code</th>
                    <th className="px-4">Level</th>
                    <th className="px-6 text-right">Employees</th>
                  </tr>
                </thead>
                <tbody>
                  {(designations.data as Designation[]).map((d) => (
                    <tr key={d.id} className="border-t border-slate-100">
                      <td className="px-6">
                        <strong>{d.title}</strong>
                        <p className="text-slate-500 mt-1">{d.description}</p>
                      </td>
                      <td className="px-4">{d.code}</td>
                      <td className="px-4">Level {d.level}</td>
                      <td className="px-6 text-right">{d.employeeCount}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </Panel>
        ) : (
          <div className="grid grid-cols-1 xl:grid-cols-2 gap-5">
            {(structures.data as Structure[]).map((s) => (
              <Panel
                key={s.id}
                title={s.name}
                subtitle={`${s.code} · Version ${s.version} · Effective ${new Date(s.effectiveFrom).toLocaleDateString()}`}
              >
                <div className="flex justify-between px-6 py-4 text-xs">
                  <span className="text-slate-500">
                    {s.employeeCount} employees assigned
                  </span>
                  <Badge status={s.isActive ? "Active" : "Inactive"} />
                </div>
                <div className="overflow-x-auto">
                  <table className="w-full text-left">
                    <thead>
                      <tr>
                        <th className="px-6">Pay component</th>
                        <th className="px-4">Type</th>
                        <th className="px-6 text-right">Rule</th>
                      </tr>
                    </thead>
                    <tbody>
                      {s.components.map((c) => (
                        <tr key={c.id} className="border-t border-slate-100">
                          <td className="px-6 font-medium">
                            {c.displayName}
                            {c.isTaxable && (
                              <p className="text-slate-500 font-normal mt-1">
                                Taxable
                              </p>
                            )}
                          </td>
                          <td className="px-4">{c.type}</td>
                          <td className="px-6 text-right">
                            {c.percentageRate > 0
                              ? `${c.percentageRate}%`
                              : money(c.defaultAmount)}
                            <p className="text-slate-500 mt-1">
                              {c.calculationMode}
                            </p>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </Panel>
            ))}
          </div>
        )
      ) : (
        !query.isPending &&
        !query.isError && (
          <div className="panel">
            <EmptyState
              title="No catalog entries yet"
              description="Entries will appear here when configured in the organization."
            />
          </div>
        )
      )}
    </div>
  );
}

type Summary = {
  id: string;
  period: string;
  runNumber: string;
  status: string;
  totalGross: number;
  totalNet: number;
  totalTax: number;
  totalEmployees: number;
};
type Labor = {
  department: string;
  totalGross: number;
  totalNet: number;
  employeeCount: number;
};
type Leave = {
  department: string;
  leaveType: string;
  totalOpening: number;
  totalUsed: number;
  totalRemaining: number;
};
export function ReportsPage() {
  const { user } = useAuth();
  const role = user?.role;
  const [year, setYear] = useState(() => new Date().getFullYear());
  const payrollAllowed = role === "Admin" || role === "Accountant";
  const laborAllowed = payrollAllowed || role === "HR";
  const leaveAllowed = role === "Admin" || role === "HR" || role === "Manager";
  const summary = useQuery({
    queryKey: ["report-payroll-summary"],
    queryFn: reportsApi.getPayrollSummary,
    enabled: payrollAllowed,
  });
  const labor = useQuery({
    queryKey: ["report-labor-cost", year],
    queryFn: () => reportsApi.getLaborCost(year),
    enabled: laborAllowed,
  });
  const leave = useQuery({
    queryKey: ["report-leave"],
    queryFn: reportsApi.getLeave,
    enabled: leaveAllowed,
  });
  const chartData =
    (summary.data as Summary[] | undefined)?.filter((r) =>
      r.period.startsWith(`${year}-`),
    ) ?? [];
  const cost = (labor.data?.departments as Labor[] | undefined) ?? [];
  const utilization = (leave.data?.utilization as Leave[] | undefined) ?? [];
  return (
    <div>
      <PageHeader
        eyebrow="Governance / Reports"
        title="Clarity behind the numbers."
        description="Explore payroll trends, departmental costs and time off. Only reports available to your role are shown."
        actions={
          laborAllowed && (
            <div>
              <label htmlFor="report-year" className="sr-only">
                Report year
              </label>
              <input
                id="report-year"
                className="bg-white border border-slate-200 rounded-lg px-3 py-2 w-24"
                type="number"
                min="2000"
                max="2100"
                value={year}
                onChange={(e) => {
                  const n = Number(e.target.value);
                  if (n >= 2000 && n <= 2100) setYear(n);
                }}
              />
            </div>
          )
        }
      />
      <div className="report-grid">
        {payrollAllowed && (
          <Panel
            title="Payroll by cycle"
            subtitle={`Gross pay, net pay and income tax · ${year} · USD`}
          >
            <QueryState loading={summary.isPending} error={summary.error} />
            {chartData.length ? (
              <div className="chart-area">
                <ResponsiveContainer width="100%" height="100%">
                  <BarChart data={chartData} barGap={4}>
                    <CartesianGrid vertical={false} stroke="#eef2ed" />
                    <XAxis
                      dataKey="period"
                      tick={{ fontSize: 11 }}
                      axisLine={false}
                      tickLine={false}
                    />
                    <YAxis
                      tick={{ fontSize: 10 }}
                      tickFormatter={(v) => `${Math.round(Number(v) / 1000)}k`}
                      axisLine={false}
                      tickLine={false}
                    />
                    <Tooltip formatter={(value) => money(Number(value))} />
                    <Legend wrapperStyle={{ fontSize: 11 }} />
                    <Bar
                      dataKey="totalGross"
                      name="Gross pay"
                      fill="#16745b"
                      radius={[3, 3, 0, 0]}
                    />
                    <Bar
                      dataKey="totalNet"
                      name="Net pay"
                      fill="#85b99f"
                      radius={[3, 3, 0, 0]}
                    />
                    <Bar
                      dataKey="totalTax"
                      name="Tax"
                      fill="#d3b778"
                      radius={[3, 3, 0, 0]}
                    />
                  </BarChart>
                </ResponsiveContainer>
              </div>
            ) : (
              !summary.isPending &&
              !summary.isError && (
                <EmptyState
                  title="No cycles for this year"
                  description="Choose another year or create a payroll cycle."
                />
              )
            )}
          </Panel>
        )}
        {laborAllowed && (
          <Panel
            title="Labor cost by department"
            subtitle={`Gross compensation across included cycles · ${year} · USD`}
          >
            <QueryState loading={labor.isPending} error={labor.error} />
            {cost.length ? (
              <div className="chart-area">
                <ResponsiveContainer width="100%" height="100%">
                  <BarChart
                    data={cost}
                    layout="vertical"
                    margin={{ left: 10, right: 30 }}
                  >
                    <CartesianGrid horizontal={false} stroke="#eef2ed" />
                    <XAxis
                      type="number"
                      tick={{ fontSize: 10 }}
                      tickFormatter={(v) => `${Math.round(Number(v) / 1000)}k`}
                      axisLine={false}
                    />
                    <YAxis
                      type="category"
                      dataKey="department"
                      width={100}
                      tick={{ fontSize: 10 }}
                      tickLine={false}
                      axisLine={false}
                    />
                    <Tooltip formatter={(v) => money(Number(v))} />
                    <Bar
                      dataKey="totalGross"
                      name="Gross cost"
                      fill="#16745b"
                      radius={[0, 4, 4, 0]}
                      barSize={22}
                    />
                  </BarChart>
                </ResponsiveContainer>
              </div>
            ) : (
              !labor.isPending &&
              !labor.isError && (
                <EmptyState
                  title="No departmental cost data"
                  description="Cost data appears when payroll items are calculated."
                />
              )
            )}
          </Panel>
        )}
      </div>
      {payrollAllowed && (
        <div className="mt-6">
          <Panel
            title="Payroll cycle summary"
            subtitle="A traceable record of each run."
          >
            <QueryState loading={summary.isPending} error={summary.error} />
            {chartData.length ? (
              <div className="overflow-x-auto">
                <table className="w-full text-left">
                  <thead>
                    <tr>
                      <th className="px-6">Run / period</th>
                      <th className="px-4">Status</th>
                      <th className="px-4 text-right">Employees</th>
                      <th className="px-4 text-right">Gross</th>
                      <th className="px-4 text-right">Tax</th>
                      <th className="px-6 text-right">Net</th>
                    </tr>
                  </thead>
                  <tbody>
                    {chartData.map((r) => (
                      <tr key={r.id} className="border-t border-slate-100">
                        <td className="px-6">
                          <strong>{r.runNumber}</strong>
                          <p className="text-slate-500 mt-1">{r.period}</p>
                        </td>
                        <td className="px-4">
                          <Badge status={r.status} />
                        </td>
                        <td className="px-4 text-right">{r.totalEmployees}</td>
                        <td className="px-4 text-right">
                          {money(r.totalGross)}
                        </td>
                        <td className="px-4 text-right">{money(r.totalTax)}</td>
                        <td className="px-6 text-right font-semibold">
                          {money(r.totalNet)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              !summary.isPending &&
              !summary.isError && (
                <EmptyState
                  title="No payroll runs to show"
                  description="Select a year with recorded payroll cycles."
                />
              )
            )}
          </Panel>
        </div>
      )}
      {leaveAllowed && (
        <div className="mt-6">
          <Panel
            title="Leave utilization"
            subtitle={`Organization report · ${leave.data?.year ?? year} · Days`}
          >
            <QueryState loading={leave.isPending} error={leave.error} />
            {utilization.length ? (
              <div className="overflow-x-auto">
                <table className="w-full text-left">
                  <thead>
                    <tr>
                      <th className="px-6">Department</th>
                      <th className="px-4">Leave type</th>
                      <th className="px-4 text-right">Opening</th>
                      <th className="px-4 text-right">Used</th>
                      <th className="px-6 text-right">Remaining</th>
                    </tr>
                  </thead>
                  <tbody>
                    {utilization.map((r, index) => (
                      <tr
                        key={`${r.department}-${r.leaveType}-${index}`}
                        className="border-t border-slate-100"
                      >
                        <td className="px-6 font-semibold">{r.department}</td>
                        <td className="px-4">
                          <span className="inline-flex items-center gap-2">
                            <CalendarDays size={14} />
                            {r.leaveType}
                          </span>
                        </td>
                        <td className="px-4 text-right">{r.totalOpening}</td>
                        <td className="px-4 text-right">{r.totalUsed}</td>
                        <td className="px-6 text-right font-semibold text-indigo-700">
                          {r.totalRemaining}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              !leave.isPending &&
              !leave.isError && (
                <EmptyState
                  title="No leave utilization data"
                  description="Leave allocations will appear once configured for this year."
                />
              )
            )}
          </Panel>
        </div>
      )}
    </div>
  );
}
