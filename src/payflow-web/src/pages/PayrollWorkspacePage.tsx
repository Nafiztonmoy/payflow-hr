import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { payrollApi, type PayrollException } from "../api/payrollApi";
import { employeesApi } from "../api/employeesApi";
import { useAuth } from "../context/auth";
import { Badge } from "../components/Badge";
import { Modal } from "../components/ui/Modal";
import {
  PageHeader,
  Metric,
  QueryState,
  EmptyState,
} from "../components/ui/Workspace";
import { money, period } from "../utils/format";
import { apiFetch } from "../api/client";
import {
  Plus,
  Download,
  ArrowRight,
  Check,
  AlertTriangle,
  FileText,
  RefreshCw,
  Lock,
} from "lucide-react";

export function PayrollWorkspacePage() {
  const client = useQueryClient();
  const { user } = useAuth();
  const [selected, setSelected] = useState("");
  const [search, setSearch] = useState("");
  const [tab, setTab] = useState<"items" | "exceptions">("items");
  const [modal, setModal] = useState<
    "create" | "paid" | "resolve" | "bank" | null
  >(null);
  const [year, setYear] = useState(() => new Date().getFullYear());
  const [month, setMonth] = useState(() => new Date().getMonth() + 1);
  const [reference, setReference] = useState("");
  const [notes, setNotes] = useState("");
  const [exception, setException] = useState<PayrollException | null>(null);
  const [bank, setBank] = useState({
    employeeId: "",
    employeeName: "",
    account: "",
    routing: "",
  });
  const [exportBusy, setExportBusy] = useState(false);
  const [exportError, setExportError] = useState("");
  const runs = useQuery({
    queryKey: ["payroll-runs"],
    queryFn: payrollApi.getRuns,
  });
  const current = runs.data?.find((r) => r.id === selected) ?? runs.data?.[0];
  const id = current?.id ?? "";
  const items = useQuery({
    queryKey: ["payroll-items", id],
    queryFn: () => payrollApi.getRunItems(id),
    enabled: !!id,
  });
  const exceptions = useQuery({
    queryKey: ["payroll-exceptions", id],
    queryFn: () => payrollApi.getRunExceptions(id),
    enabled: !!id,
  });
  const invalidate = () => {
    void client.invalidateQueries({ queryKey: ["payroll-runs"] });
    void client.invalidateQueries({ queryKey: ["payroll-items", id] });
    void client.invalidateQueries({ queryKey: ["payroll-exceptions", id] });
  };
  const create = useMutation({
    mutationFn: () => payrollApi.createRun(year, month),
    onSuccess: (r) => {
      setSelected(r.id);
      setModal(null);
      invalidate();
    },
  });
  const calculate = useMutation({
    mutationFn: () => payrollApi.calculateRun(id),
    onSuccess: invalidate,
  });
  const review = useMutation({
    mutationFn: () => payrollApi.submitReview(id, current!.concurrencyToken),
    onSuccess: invalidate,
  });
  const approve = useMutation({
    mutationFn: () => payrollApi.approveRun(id, current!.concurrencyToken),
    onSuccess: invalidate,
  });
  const paid = useMutation({
    mutationFn: () =>
      payrollApi.markPaid(
        id,
        reference.trim(),
        new Date().toISOString(),
        current!.concurrencyToken,
      ),
    onSuccess: () => {
      setModal(null);
      invalidate();
    },
  });
  const resolve = useMutation({
    mutationFn: () => payrollApi.resolveException(exception!.id, notes.trim()),
    onSuccess: () => {
      setModal(null);
      invalidate();
    },
  });
  const fixBank = useMutation({
    mutationFn: async () => {
      await employeesApi.updateEmployee(bank.employeeId, {
        bankAccountNumber: bank.account.trim(),
        bankRoutingNumber: bank.routing.trim(),
      });
      return payrollApi.calculateRun(id);
    },
    onSuccess: () => {
      setModal(null);
      invalidate();
      void client.invalidateQueries({ queryKey: ["employees"] });
    },
  });
  const busy =
    create.isPending ||
    calculate.isPending ||
    review.isPending ||
    approve.isPending ||
    paid.isPending ||
    resolve.isPending ||
    fixBank.isPending;
  const immutable =
    current?.status === "Paid" || current?.status === "Approved";
  const canCalculate =
    current &&
    ["Draft", "Calculated", "InReview", "Failed"].includes(current.status);
  const blockers = current?.blockingCount ?? 0;
  const filtered =
    items.data?.filter((i) =>
      `${i.employeeName} ${i.employeeNo} ${i.departmentName}`
        .toLowerCase()
        .includes(search.toLowerCase()),
    ) ?? [];
  const exportCsv = async () => {
    setExportBusy(true);
    setExportError("");
    try {
      const csv = await apiFetch<string>(`/payroll-runs/${id}/export-csv`);
      const url = URL.createObjectURL(
        new Blob([csv], { type: "text/csv;charset=utf-8" }),
      );
      const a = document.createElement("a");
      a.href = url;
      a.download = `${current?.runNumber ?? "payroll"}.csv`;
      a.click();
      URL.revokeObjectURL(url);
    } catch (err) {
      setExportError(err instanceof Error ? err.message : "CSV export failed.");
    } finally {
      setExportBusy(false);
    }
  };
  return (
    <div>
      <PageHeader
        eyebrow="Payroll & time / Payroll"
        title="Payroll workspace"
        description="Review, calculate and close each cycle with confidence."
        actions={
          <>
            {current && (
              <button
                className="btn btn-secondary"
                disabled={exportBusy}
                onClick={() => void exportCsv()}
              >
                <Download size={16} />
                {exportBusy ? "Exporting…" : "Export CSV"}
              </button>
            )}
            <button
              className="btn btn-primary"
              disabled={busy}
              onClick={() => setModal("create")}
            >
              <Plus size={16} />
              Create run
            </button>
          </>
        }
      />
      {exportError && (
        <div className="notice notice-error" role="alert">
          {exportError}
        </div>
      )}
      <QueryState
        loading={runs.isPending}
        error={runs.error}
        retry={() => void runs.refetch()}
      />
      {!runs.isPending && !runs.isError && !current && (
        <div className="panel">
          <EmptyState
            title="Your first cycle starts here"
            description="Create a run for the payroll month you want to process."
          />
        </div>
      )}
      {current && (
        <div className="payroll-grid">
          <aside className="panel payroll-runs">
            <div className="panel-heading">
              <div>
                <h2>Payroll runs</h2>
                <p>{runs.data?.length} recorded cycles</p>
              </div>
            </div>
            <div className="p-3 space-y-2">
              {runs.data?.map((r) => (
                <button
                  key={r.id}
                  className={`run-choice ${r.id === id ? "selected" : ""}`}
                  onClick={() => {
                    setSelected(r.id);
                    setSearch("");
                  }}
                >
                  <div className="flex justify-between items-center gap-3">
                    <strong>{period(r.year, r.month)}</strong>
                    <ArrowRight size={14} />
                  </div>
                  <p>{r.runNumber}</p>
                  <div className="flex items-center justify-between mt-3">
                    <Badge status={r.status} />
                    <span>{r.totalEmployees} people</span>
                  </div>
                </button>
              ))}
            </div>
          </aside>
          <div className="min-w-0 space-y-5">
            <section className="panel p-5 sm:p-6">
              <div className="flex flex-wrap justify-between items-start gap-3 mb-6">
                <div>
                  <p className="eyebrow">{current.runNumber}</p>
                  <h2 className="text-xl font-semibold">
                    {period(current.year, current.month)} payroll
                  </h2>
                  <p className="text-xs text-slate-500 mt-2">
                    Monthly cycle · {current.totalEmployees} employees · USD
                  </p>
                </div>
                <Badge status={current.status} size="md" />
              </div>
              <ol className="payroll-steps">
                {["Draft", "Calculated", "InReview", "Approved", "Paid"].map(
                  (s, index) => {
                    const order = [
                      "Draft",
                      "Calculated",
                      "InReview",
                      "Approved",
                      "Paid",
                    ].indexOf(current.status);
                    const passed = order > index;
                    return (
                      <li
                        key={s}
                        className={
                          current.status === s
                            ? "current"
                            : passed
                              ? "passed"
                              : ""
                        }
                      >
                        <span>{passed ? <Check size={15} /> : index + 1}</span>
                        <div>
                          <strong>
                            {s === "InReview" ? "Under review" : s}
                          </strong>
                          <small>
                            {
                              [
                                "Inputs ready",
                                "Totals computed",
                                "Review & sign off",
                                "Ready to record",
                                "Payment recorded",
                              ][index]
                            }
                          </small>
                        </div>
                      </li>
                    );
                  },
                )}
              </ol>
            </section>
            <div className="payroll-metrics">
              <Metric
                label="Gross pay"
                value={money(current.totalGross)}
                detail="Base, earnings and overtime"
              />
              <Metric
                label="Net pay"
                value={money(current.totalNet)}
                detail="Take-home after deductions"
              />
              <Metric
                label="Tax withheld"
                value={money(current.totalTax)}
                detail={`${money(current.totalDeductions)} total deductions`}
              />
            </div>
            {blockers > 0 && (
              <div className="notice bg-amber-50 border border-amber-200 text-amber-900">
                <div className="flex gap-3">
                  <AlertTriangle size={20} className="shrink-0" />
                  <div>
                    <strong>
                      {blockers} blocking{" "}
                      {blockers === 1 ? "exception" : "exceptions"} require
                      attention.
                    </strong>
                    <p className="text-xs mt-1">
                      Resolve blocking issues before approval.
                    </p>
                  </div>
                </div>
                <button
                  className="btn btn-secondary"
                  onClick={() => setTab("exceptions")}
                >
                  View exceptions
                </button>
              </div>
            )}
            {immutable && (
              <div className="notice notice-info">
                <Lock size={18} />
                <span>
                  {current.status === "Paid"
                    ? "Payment has been recorded. This cycle is locked."
                    : "This cycle is approved. Calculations are locked."}
                  {current.paymentReference &&
                    ` Reference: ${current.paymentReference}`}
                </span>
              </div>
            )}
            <section className="panel">
              <div className="flex flex-col sm:flex-row justify-between gap-3 border-b border-slate-200 p-4">
                <div
                  className="flex gap-2"
                  role="tablist"
                  aria-label="Payroll details"
                >
                  <button
                    role="tab"
                    aria-selected={tab === "items"}
                    className={`btn ${tab === "items" ? "btn-primary" : "btn-secondary"}`}
                    onClick={() => setTab("items")}
                  >
                    Payroll items
                  </button>
                  <button
                    role="tab"
                    aria-selected={tab === "exceptions"}
                    className={`btn ${tab === "exceptions" ? "btn-primary" : "btn-secondary"}`}
                    onClick={() => setTab("exceptions")}
                  >
                    Exceptions{" "}
                    {exceptions.data ? `(${exceptions.data.length})` : ""}
                  </button>
                </div>
                {tab === "items" && (
                  <input
                    aria-label="Search payroll items"
                    placeholder="Search name, ID or department"
                    value={search}
                    onChange={(e) => setSearch(e.target.value)}
                    className="rounded-lg border border-slate-200 px-3 py-2 min-w-0 sm:w-64"
                  />
                )}
              </div>
              {tab === "items" ? (
                <>
                  <QueryState
                    loading={items.isPending}
                    error={items.error}
                    retry={() => void items.refetch()}
                  />
                  {filtered.length ? (
                    <div className="overflow-x-auto">
                      <table className="w-full text-left">
                        <thead>
                          <tr>
                            <th className="px-5">Employee</th>
                            <th className="px-4 text-right">Base / gross</th>
                            <th className="px-4 text-right">
                              Tax / deductions
                            </th>
                            <th className="px-4 text-right">Net pay</th>
                            <th className="px-5">Statement</th>
                          </tr>
                        </thead>
                        <tbody>
                          {filtered.map((i) => (
                            <tr
                              key={i.id}
                              className="border-t border-slate-100"
                            >
                              <td className="px-5">
                                <strong>{i.employeeName}</strong>
                                <p className="text-xs text-slate-500 mt-1">
                                  {i.employeeNo} · {i.departmentName}
                                </p>
                                <p className="text-xs text-slate-500 mt-1">
                                  {i.designationTitle}
                                </p>
                              </td>
                              <td className="px-4 text-right">
                                <strong>{money(i.grossPay)}</strong>
                                <p className="text-xs text-slate-500 mt-1">
                                  {money(i.baseSalary)} base
                                </p>
                              </td>
                              <td className="px-4 text-right">
                                {money(i.incomeTax)}
                                <p className="text-xs text-slate-500 mt-1">
                                  {money(i.totalDeductions)} total
                                </p>
                              </td>
                              <td className="px-4 text-right font-semibold text-indigo-700">
                                {money(i.netPay)}
                              </td>
                              <td className="px-5">
                                <Link
                                  className="text-link"
                                  to={`/payslip/${i.id}`}
                                >
                                  <FileText size={14} />
                                  Payslip
                                </Link>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  ) : (
                    !items.isPending &&
                    !items.isError && (
                      <EmptyState
                        title={
                          search
                            ? "No matching payroll items"
                            : "No payroll items yet"
                        }
                        description={
                          search
                            ? "Try another employee name or department."
                            : "Calculate this run to create employee payroll items."
                        }
                      />
                    )
                  )}
                </>
              ) : (
                <>
                  <QueryState
                    loading={exceptions.isPending}
                    error={exceptions.error}
                    retry={() => void exceptions.refetch()}
                  />
                  {exceptions.data?.length ? (
                    <div className="divide-y divide-slate-100">
                      {exceptions.data.map((ex) => (
                        <div key={ex.id} className="p-5">
                          <div className="flex flex-wrap items-center gap-2 mb-2">
                            <Badge
                              status={ex.isResolved ? "Resolved" : ex.severity}
                            />
                            <strong className="text-sm">
                              {ex.employeeName}
                            </strong>
                            <span className="text-xs text-slate-500">
                              {ex.exceptionType}
                              {ex.isResolved ? " · Resolved" : ""}
                            </span>
                          </div>
                          <p className="text-xs text-slate-600 leading-relaxed">
                            {ex.message}
                          </p>
                          {ex.resolutionNotes && (
                            <p className="text-xs text-slate-500 mt-2">
                              Resolution: {ex.resolutionNotes}
                            </p>
                          )}
                          {!ex.isResolved && !immutable && (
                            <div className="flex flex-wrap gap-2 mt-4">
                              {ex.exceptionType === "MISSING_BANK_ACCOUNT" &&
                                user?.role === "Admin" && (
                                  <button
                                    className="btn btn-secondary"
                                    onClick={() => {
                                      setBank({
                                        employeeId: ex.employeeId,
                                        employeeName: ex.employeeName,
                                        account: "",
                                        routing: "",
                                      });
                                      setModal("bank");
                                    }}
                                  >
                                    Update bank details
                                  </button>
                                )}
                              <button
                                className="btn btn-secondary"
                                disabled={busy}
                                onClick={() => {
                                  setException(ex);
                                  setNotes("");
                                  setModal("resolve");
                                }}
                              >
                                Resolve with note
                              </button>
                              {ex.exceptionType === "MISSING_BANK_ACCOUNT" &&
                                user?.role === "Accountant" && (
                                  <p className="text-xs text-slate-500 self-center">
                                    Ask HR or Admin to update bank details, then
                                    recalculate.
                                  </p>
                                )}
                            </div>
                          )}
                        </div>
                      ))}
                    </div>
                  ) : (
                    !exceptions.isPending &&
                    !exceptions.isError && (
                      <EmptyState
                        title="No exceptions to review"
                        description="Exceptions from payroll calculation appear here."
                      />
                    )
                  )}
                </>
              )}
              <div className="filter-footer">
                <span>
                  {tab === "items"
                    ? `${filtered.length} of ${items.data?.length ?? 0} payroll items`
                    : `${current.blockingCount} blocking · ${current.warningCount} warnings`}
                </span>
                <span>USD</span>
              </div>
            </section>
            <div className="flex flex-wrap justify-end gap-3 no-print">
              {canCalculate && (
                <button
                  className="btn btn-secondary"
                  disabled={busy}
                  onClick={() => calculate.mutate()}
                >
                  <RefreshCw size={15} />
                  {calculate.isPending ? "Calculating…" : "Calculate run"}
                </button>
              )}
              {current.status === "Calculated" && (
                <button
                  className="btn btn-primary"
                  disabled={busy}
                  onClick={() => review.mutate()}
                >
                  Submit for review
                  <ArrowRight size={15} />
                </button>
              )}
              {["Calculated", "InReview"].includes(current.status) && (
                <button
                  className="btn btn-primary"
                  disabled={busy || blockers > 0}
                  title={
                    blockers
                      ? "Resolve blocking exceptions before approval"
                      : undefined
                  }
                  onClick={() => approve.mutate()}
                >
                  Approve run
                  <Check size={15} />
                </button>
              )}
              {current.status === "Approved" && (
                <button
                  className="btn btn-primary"
                  disabled={busy}
                  onClick={() => {
                    setReference("");
                    setModal("paid");
                  }}
                >
                  Record payment
                  <ArrowRight size={15} />
                </button>
              )}
            </div>
          </div>
        </div>
      )}
      {modal === "create" && (
        <Modal
          title="Create a payroll run"
          description="Choose the month to process. The backend validates duplicate periods."
          close={() => setModal(null)}
        >
          <QueryState loading={false} error={create.error} />
          <form
            className="mt-5 space-y-4"
            onSubmit={(e) => {
              e.preventDefault();
              create.mutate();
            }}
          >
            <div className="grid grid-cols-2 gap-4">
              <label className="text-xs font-semibold">
                Year
                <input
                  type="number"
                  min="2000"
                  max="2100"
                  required
                  value={year}
                  onChange={(e) => setYear(Number(e.target.value))}
                  className="w-full mt-2 rounded-lg border border-slate-300 px-3 py-2"
                />
              </label>
              <label className="text-xs font-semibold">
                Month
                <select
                  value={month}
                  onChange={(e) => setMonth(Number(e.target.value))}
                  className="w-full mt-2 rounded-lg border border-slate-300 px-3 py-2"
                >
                  {Array.from({ length: 12 }, (_, i) => (
                    <option key={i} value={i + 1}>
                      {new Date(2026, i).toLocaleDateString("en-US", {
                        month: "long",
                      })}
                    </option>
                  ))}
                </select>
              </label>
            </div>
            <button className="btn btn-primary w-full" disabled={busy}>
              {create.isPending ? "Creating…" : "Create run"}
            </button>
          </form>
        </Modal>
      )}
      {modal === "paid" && (
        <Modal
          title="Record payroll payment"
          description="Record a payment already made outside PayFlow. This action does not initiate a bank transfer and locks the payroll cycle."
          close={() => setModal(null)}
        >
          <QueryState loading={false} error={paid.error} />
          <form
            className="mt-5 space-y-4"
            onSubmit={(e) => {
              e.preventDefault();
              paid.mutate();
            }}
          >
            <label className="text-xs font-semibold">
              Payment reference
              <input
                required
                value={reference}
                onChange={(e) => setReference(e.target.value)}
                placeholder="Your payment or batch reference"
                className="w-full mt-2 rounded-lg border border-slate-300 px-3 py-2"
              />
            </label>
            <button
              className="btn btn-primary w-full"
              disabled={busy || !reference.trim()}
            >
              {paid.isPending ? "Recording…" : "Confirm payment recorded"}
            </button>
          </form>
        </Modal>
      )}
      {modal === "resolve" && (
        <Modal
          title="Resolve payroll exception"
          description={exception?.message}
          close={() => setModal(null)}
        >
          <QueryState loading={false} error={resolve.error} />
          <form
            className="mt-5 space-y-4"
            onSubmit={(e) => {
              e.preventDefault();
              resolve.mutate();
            }}
          >
            <label className="text-xs font-semibold">
              Resolution note
              <textarea
                required
                rows={4}
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                placeholder="Describe what was verified or corrected."
                className="w-full mt-2 rounded-lg border border-slate-300 p-3"
              />
            </label>
            <button
              className="btn btn-primary w-full"
              disabled={busy || !notes.trim()}
            >
              {resolve.isPending ? "Saving…" : "Save resolution"}
            </button>
          </form>
        </Modal>
      )}
      {modal === "bank" && (
        <Modal
          title={`Update bank details · ${bank.employeeName}`}
          description="Enter verified details. Payroll will be recalculated after saving."
          close={() => setModal(null)}
        >
          <QueryState loading={false} error={fixBank.error} />
          <form
            className="mt-5 space-y-4"
            onSubmit={(e) => {
              e.preventDefault();
              fixBank.mutate();
            }}
          >
            <label className="block text-xs font-semibold">
              Account number
              <input
                required
                value={bank.account}
                onChange={(e) => setBank({ ...bank, account: e.target.value })}
                className="w-full mt-2 rounded-lg border border-slate-300 px-3 py-2"
              />
            </label>
            <label className="block text-xs font-semibold">
              Routing number
              <input
                required
                value={bank.routing}
                onChange={(e) => setBank({ ...bank, routing: e.target.value })}
                className="w-full mt-2 rounded-lg border border-slate-300 px-3 py-2"
              />
            </label>
            <button
              className="btn btn-primary w-full"
              disabled={busy || !bank.account.trim() || !bank.routing.trim()}
            >
              {fixBank.isPending ? "Saving…" : "Save and recalculate"}
            </button>
          </form>
        </Modal>
      )}
    </div>
  );
}
