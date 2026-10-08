import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { useAuth } from "../context/auth";
import { payrollApi } from "../api/payrollApi";
import { leaveApi, orgApi } from "../api/commonApis";
import { employeesApi } from "../api/employeesApi";
import { Badge } from "../components/Badge";
import {
  PageHeader,
  Metric,
  Panel,
  EmptyState,
  QueryState,
} from "../components/ui/Workspace";
import { money, period } from "../utils/format";
import {
  Users,
  CalendarDays,
  Calculator,
  ArrowUpRight,
  ArrowRight,
  Building2,
  Receipt,
  ShieldCheck,
  Clock,
  CircleCheck,
  AlertTriangle,
} from "lucide-react";

export function DashboardPage() {
  const { user } = useAuth();
  const role = user?.role;
  const payrollRole = role === "Admin" || role === "Accountant";
  const peopleRole = role === "Admin" || role === "HR";
  const leaveRole = peopleRole || role === "Manager";
  const selfRole =
    role === "Employee" || role === "Manager" || role === "Accountant";
  const runs = useQuery({
    queryKey: ["payroll-runs"],
    queryFn: payrollApi.getRuns,
    enabled: payrollRole,
  });
  const leaves = useQuery({
    queryKey: ["leave-requests", "pending"],
    queryFn: () => leaveApi.getRequests("Pending"),
    enabled: leaveRole,
  });
  const employees = useQuery({
    queryKey: ["employees", "count"],
    queryFn: () => employeesApi.getEmployees({ pageSize: 1 }),
    enabled: peopleRole,
  });
  const departments = useQuery({
    queryKey: ["departments"],
    queryFn: orgApi.getDepartments,
    enabled: role === "HR",
  });
  const structures = useQuery({
    queryKey: ["salary-structures"],
    queryFn: orgApi.getSalaryStructures,
    enabled: role === "HR",
  });
  const balances = useQuery({
    queryKey: ["my-leave-balances"],
    queryFn: leaveApi.getMyBalances,
    enabled: !!user?.employeeId && selfRole,
  });
  const slips = useQuery({
    queryKey: ["my-payslips"],
    queryFn: payrollApi.getMyPayslips,
    enabled: selfRole,
  });
  if (!user) return null;
  const latest = runs.data?.[0];
  const display = (
    query: { isPending: boolean; isError: boolean },
    value: string | number | undefined,
  ) => (query.isPending ? "…" : query.isError ? "—" : (value ?? 0));
  const primary = payrollRole
    ? { to: "/payroll", label: "Open payroll workspace" }
    : role === "Employee"
      ? { to: "/leaves", label: "Request time off" }
      : {
          to: "/employees",
          label: role === "Manager" ? "View directory" : "View workforce",
        };
  const pendingBlocks =
    runs.data?.reduce((n, r) => n + r.blockingCount, 0) ?? 0;
  return (
    <div>
      <PageHeader
        eyebrow={`${role} workspace / Overview`}
        title={
          role === "Employee"
            ? "Your work, at a glance."
            : "Your people. Your progress."
        }
        description={`Welcome back, ${user.userName}. ${role === "Employee" ? "Your time off and payslips, together in one place." : "A clear view of your workforce, approvals and payroll cycles."}`}
        actions={
          <Link className="btn btn-primary" to={primary.to}>
            {primary.label}
            <ArrowUpRight size={16} />
          </Link>
        }
      />
      <div
        className={`metrics-grid ${role === "Employee" ? "metrics-three" : ""}`}
      >
        {peopleRole && (
          <>
            <Metric
              label="Total workforce"
              value={display(employees, employees.data?.total)}
              detail="Employees in the directory"
              icon={<Users size={18} />}
            />
            {role === "HR" && (
              <Metric
                label="Departments"
                value={display(departments, departments.data?.length)}
                detail="Your organization structure"
                icon={<Building2 size={18} />}
              />
            )}
            {role === "HR" && (
              <Metric
                label="Salary structures"
                value={display(structures, structures.data?.length)}
                detail="Versioned compensation plans"
                icon={<Receipt size={18} />}
              />
            )}
          </>
        )}
        {leaveRole && (
          <Metric
            label="Pending time off"
            value={display(leaves, leaves.data?.length)}
            detail={
              role === "Manager"
                ? "Requests available for your review"
                : "Requests awaiting review"
            }
            icon={<CalendarDays size={18} />}
          />
        )}
        {payrollRole && (
          <>
            <Metric
              label="Payroll cycles"
              value={display(runs, runs.data?.length)}
              detail="Recorded payroll runs"
              icon={<Calculator size={18} />}
            />
            {!peopleRole && (
              <Metric
                label="Latest gross pay"
                value={display(runs, latest ? money(latest.totalGross) : "—")}
                detail={
                  latest
                    ? period(latest.year, latest.month)
                    : "No payroll cycle recorded"
                }
                icon={<Receipt size={18} />}
              />
            )}
            <Metric
              label="Latest net pay"
              value={display(runs, latest ? money(latest.totalNet) : "—")}
              detail={
                latest
                  ? `${period(latest.year, latest.month)} · ${latest.status}`
                  : "No payroll cycle recorded"
              }
              icon={<CircleCheck size={18} />}
            />
            {!peopleRole && (
              <Metric
                label="Blocking exceptions"
                value={display(runs, pendingBlocks)}
                detail="Unresolved across recorded cycles"
                icon={<AlertTriangle size={18} />}
              />
            )}
          </>
        )}
        {selfRole && !payrollRole && (
          <>
            <Metric
              label="Available leave"
              value={
                !user.employeeId
                  ? "—"
                  : display(
                      balances,
                      balances.data?.reduce((n, b) => n + b.remainingDays, 0),
                    )
              }
              detail="Days remaining across leave types"
              icon={<CalendarDays size={18} />}
            />
            <Metric
              label="My payslips"
              value={display(slips, slips.data?.length)}
              detail="Issued from paid payroll cycles"
              icon={<Receipt size={18} />}
            />
            <Metric
              label="Latest take-home"
              value={display(
                slips,
                slips.data?.[0] ? money(slips.data[0].netPay) : "—",
              )}
              detail={
                slips.data?.[0]
                  ? period(slips.data[0].year, slips.data[0].month)
                  : "No payslip issued yet"
              }
              icon={<CircleCheck size={18} />}
            />
          </>
        )}
      </div>
      <div className="dashboard-grid">
        <div>
          {payrollRole ? (
            <Panel
              title="Recent payroll runs"
              subtitle="Every cycle, from draft to recorded payment."
              link="/payroll"
            >
              <QueryState
                loading={runs.isPending}
                error={runs.error}
                retry={() => void runs.refetch()}
              />
              {runs.data?.length ? (
                <div className="overflow-x-auto">
                  <table className="w-full text-left">
                    <thead>
                      <tr>
                        <th className="px-6">Cycle</th>
                        <th className="px-4 text-right">Employees</th>
                        <th className="px-4 text-right">Net pay</th>
                        <th className="px-4">Status</th>
                      </tr>
                    </thead>
                    <tbody>
                      {runs.data.slice(0, 5).map((run) => (
                        <tr key={run.id} className="border-t border-slate-100">
                          <td className="px-6">
                            <Link
                              className="font-semibold text-slate-900"
                              to="/payroll"
                            >
                              {period(run.year, run.month)}
                            </Link>
                            <p className="text-slate-500 text-xs mt-1">
                              {run.runNumber}
                            </p>
                          </td>
                          <td className="px-4 text-right">
                            {run.totalEmployees}
                          </td>
                          <td className="px-4 text-right font-semibold">
                            {money(run.totalNet)}
                          </td>
                          <td className="px-4">
                            <Badge status={run.status} />
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : (
                !runs.isPending &&
                !runs.isError && (
                  <EmptyState
                    title="Your first payroll cycle starts here"
                    description="Open the payroll workspace to create a run."
                  />
                )
              )}
            </Panel>
          ) : leaveRole ? (
            <Panel
              title="Time off awaiting review"
              subtitle="Keep your team's next steps moving."
              link="/leaves"
            >
              <QueryState
                loading={leaves.isPending}
                error={leaves.error}
                retry={() => void leaves.refetch()}
              />
              {leaves.data?.length ? (
                <div className="action-list">
                  {leaves.data.slice(0, 5).map((l) => (
                    <Link to="/leaves" key={l.id} className="action-row">
                      <span className="avatar">
                        {l.employeeName
                          .split(" ")
                          .map((n) => n[0])
                          .slice(0, 2)
                          .join("")}
                      </span>
                      <div>
                        <strong>{l.employeeName}</strong>
                        <p>
                          {l.leaveTypeName} · {l.dayCount} days ·{" "}
                          {new Date(l.startDate).toLocaleDateString()}
                        </p>
                      </div>
                      <ArrowRight size={16} />
                    </Link>
                  ))}
                </div>
              ) : (
                !leaves.isPending &&
                !leaves.isError && (
                  <EmptyState
                    title="You're all caught up"
                    description="There are no pending time off requests available for review."
                  />
                )
              )}
            </Panel>
          ) : (
            <Panel
              title="Your latest payslips"
              subtitle="A clear record of your earnings."
              link="/my-payslips"
            >
              <QueryState
                loading={slips.isPending}
                error={slips.error}
                retry={() => void slips.refetch()}
              />
              {slips.data?.length ? (
                <div className="action-list">
                  {slips.data.slice(0, 4).map((p) => (
                    <Link
                      className="action-row"
                      key={p.id}
                      to={`/payslip/${p.payrollItemId}`}
                    >
                      <span className="empty-icon mb-0">
                        <Receipt size={20} />
                      </span>
                      <div>
                        <strong>{period(p.year, p.month)}</strong>
                        <p>
                          {p.payslipNumber} · {money(p.netPay)} net
                        </p>
                      </div>
                      <ArrowRight size={16} />
                    </Link>
                  ))}
                </div>
              ) : (
                !slips.isPending &&
                !slips.isError && (
                  <EmptyState
                    title="Payslips will appear here"
                    description="Your payslips are issued after a payroll cycle is marked paid."
                  />
                )
              )}
            </Panel>
          )}
          {selfRole && user.employeeId && (
            <div className="dashboard-secondary">
              <Panel
                title="Your leave balances"
                subtitle="Plan a little time for yourself."
                link={role === "Accountant" ? undefined : "/leaves"}
              >
                <QueryState
                  loading={balances.isPending}
                  error={balances.error}
                />
                {balances.data?.length ? (
                  <div className="action-list">
                    {balances.data.map((b) => (
                      <div className="action-row" key={b.id}>
                        <CalendarDays size={20} className="text-indigo-600" />
                        <div className="flex-1">
                          <strong>{b.leaveTypeName}</strong>
                          <p>
                            {b.usedDays} days used · {b.year}
                          </p>
                        </div>
                        <strong>{b.remainingDays} days</strong>
                      </div>
                    ))}
                  </div>
                ) : (
                  !balances.isPending &&
                  !balances.isError && (
                    <EmptyState
                      title="No leave allocation yet"
                      description="Contact HR if you expected an available balance."
                    />
                  )
                )}
              </Panel>
            </div>
          )}
        </div>
        <div className="space-y-5">
          <Panel
            title="Needs your attention"
            subtitle="The things that matter next."
          >
            <div className="action-list">
              {leaveRole && (
                <Link className="action-row" to="/leaves">
                  <CalendarDays size={20} className="text-indigo-600" />
                  <div>
                    <strong>Review time off</strong>
                    <p>
                      {leaves.isPending
                        ? "Loading requests…"
                        : leaves.isError
                          ? "Requests unavailable"
                          : `${leaves.data?.length ?? 0} pending requests`}
                    </p>
                  </div>
                  <ArrowRight size={16} />
                </Link>
              )}
              {payrollRole && (
                <Link className="action-row" to="/payroll">
                  <AlertTriangle size={20} className="text-amber-600" />
                  <div>
                    <strong>Payroll exceptions</strong>
                    <p>
                      {runs.isPending
                        ? "Loading cycles…"
                        : runs.isError
                          ? "Exception count unavailable"
                          : `${pendingBlocks} blocking exceptions across runs`}
                    </p>
                  </div>
                  <ArrowRight size={16} />
                </Link>
              )}
              {role === "Employee" && (
                <Link className="action-row" to="/leaves">
                  <CalendarDays size={20} className="text-indigo-600" />
                  <div>
                    <strong>Plan your time off</strong>
                    <p>Review balances and submit a request.</p>
                  </div>
                  <ArrowRight size={16} />
                </Link>
              )}
              {role === "Admin" && (
                <Link className="action-row" to="/audit-logs">
                  <ShieldCheck size={20} className="text-indigo-600" />
                  <div>
                    <strong>Review audit trail</strong>
                    <p>Trace approvals and workforce changes.</p>
                  </div>
                  <ArrowRight size={16} />
                </Link>
              )}
              {role !== "Accountant" && (
                <Link className="action-row" to="/attendance">
                  <Clock size={20} className="text-indigo-600" />
                  <div>
                    <strong>Attendance records</strong>
                    <p>Review recorded hours and corrections.</p>
                  </div>
                  <ArrowRight size={16} />
                </Link>
              )}
              {role === "Accountant" && (
                <Link className="action-row" to="/reports">
                  <Receipt size={20} className="text-indigo-600" />
                  <div>
                    <strong>Explore payroll reports</strong>
                    <p>Compare cycles and departmental costs.</p>
                  </div>
                  <ArrowRight size={16} />
                </Link>
              )}
            </div>
          </Panel>
          <div className="notice notice-info">
            <div>
              <p className="eyebrow mb-2">A connected workspace</p>
              <strong className="text-sm">People, time and pay.</strong>
              <p className="text-xs mt-2">
                Metrics reflect API data. Unavailable data is shown explicitly.
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
