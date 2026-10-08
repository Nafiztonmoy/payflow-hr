import { useParams, Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { payrollApi } from "../api/payrollApi";
import { useAuth } from "../context/auth";
import {
  PageHeader,
  Panel,
  Metric,
  EmptyState,
  QueryState,
} from "../components/ui/Workspace";
import { money, period } from "../utils/format";
import { Printer, ArrowLeft, Sprout, Receipt, ShieldCheck } from "lucide-react";
export function PayslipPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const query = useQuery({
    queryKey: ["payslip", id],
    queryFn: () => payrollApi.getPayslip(id!),
    enabled: !!id,
  });
  const p = query.data;
  const back =
    user?.role === "Admin" || user?.role === "Accountant"
      ? "/payroll"
      : "/my-payslips";
  if (query.isPending || query.isError || !p)
    return (
      <div className="panel">
        <QueryState
          loading={query.isPending}
          error={query.error}
          retry={() => void query.refetch()}
        />
        {!query.isPending && (
          <EmptyState
            title="Payslip unavailable"
            description="The statement may not be issued yet, or your role may not have access."
          />
        )}
        <div className="p-5 text-center">
          <Link className="btn btn-secondary" to={back}>
            Back to workspace
          </Link>
        </div>
      </div>
    );
  const format = (v: number) => money(v, p.currency);
  return (
    <div className="max-w-4xl mx-auto">
      <div className="flex justify-between items-center gap-3 mb-6 no-print">
        <Link to={back} className="text-link">
          <ArrowLeft size={15} />
          Back to {back === "/payroll" ? "payroll" : "my payslips"}
        </Link>
        <button className="btn btn-primary" onClick={() => window.print()}>
          <Printer size={16} />
          Print statement
        </button>
      </div>
      <article className="payslip-document panel p-6 sm:p-10 print:border-0 print:shadow-none">
        <header className="flex flex-col sm:flex-row justify-between gap-6 pb-7 border-b border-slate-200">
          <div>
            <span className="brand-mark mb-4">
              <Sprout size={23} />
            </span>
            <h1 className="text-xl font-semibold">{p.organizationName}</h1>
            <p className="text-xs text-slate-500 mt-2">
              Employee pay statement
            </p>
          </div>
          <div className="sm:text-right">
            <p className="eyebrow">Payslip</p>
            <h2 className="text-lg font-semibold">
              {period(p.periodYear, p.periodMonth)}
            </h2>
            <p className="text-xs text-slate-500 mt-2">{p.payslipNumber}</p>
            <p className="text-xs text-slate-500 mt-1">
              Issued {new Date(p.issueDateUtc).toLocaleDateString()}
            </p>
          </div>
        </header>
        <dl className="grid grid-cols-2 sm:grid-cols-4 gap-5 py-7 border-b border-slate-200 text-xs">
          <div>
            <dt className="text-slate-500 mb-2">Employee</dt>
            <dd className="font-semibold">{p.employeeName}</dd>
            <dd className="text-slate-500 mt-1">{p.employeeNo}</dd>
          </div>
          <div>
            <dt className="text-slate-500 mb-2">Department & role</dt>
            <dd className="font-semibold">{p.department}</dd>
            <dd className="text-slate-500 mt-1">{p.designation}</dd>
          </div>
          <div>
            <dt className="text-slate-500 mb-2">Payment method</dt>
            <dd className="font-semibold">{p.paymentMethod}</dd>
            <dd className="text-slate-500 mt-1">{p.maskedBankAccount}</dd>
          </div>
          <div>
            <dt className="text-slate-500 mb-2">Base salary</dt>
            <dd className="font-semibold">{format(p.baseSalary)}</dd>
            <dd className="text-slate-500 mt-1">{p.currency} / month</dd>
          </div>
        </dl>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-8 py-7">
          {[
            { title: "Earnings", items: p.earnings, total: p.grossPay },
            {
              title: "Deductions",
              items: p.deductions,
              total: p.totalDeductions,
            },
          ].map((section) => (
            <section key={section.title}>
              <h3 className="text-sm font-semibold border-b-2 border-indigo-600 pb-3 mb-4">
                {section.title}
              </h3>
              <div className="space-y-4">
                {section.items.map((c) => (
                  <div
                    key={c.componentCode}
                    className="flex justify-between gap-4 text-xs"
                  >
                    <div>
                      <strong className="font-medium">{c.componentName}</strong>
                      <p className="text-slate-500 mt-1 leading-relaxed">
                        {c.explanation}
                      </p>
                    </div>
                    <span className="font-semibold whitespace-nowrap">
                      {format(c.amount)}
                    </span>
                  </div>
                ))}
              </div>
              <div className="flex justify-between text-sm font-semibold border-t border-slate-200 mt-6 pt-4">
                <span>Total {section.title.toLowerCase()}</span>
                <span>{format(section.total)}</span>
              </div>
            </section>
          ))}
        </div>
        <div className="flex flex-col sm:flex-row justify-between gap-4 p-6 bg-indigo-50 border border-indigo-200 rounded-xl">
          <div>
            <p className="text-sm font-semibold">Net take-home pay</p>
            <p className="text-xs text-slate-500 mt-2">
              Gross earnings less total deductions
            </p>
          </div>
          <strong className="text-3xl font-semibold text-indigo-700">
            {format(p.netPay)}
          </strong>
        </div>
        <footer className="pt-6 mt-7 border-t border-slate-200 text-xs text-slate-500 flex gap-3 items-start">
          <ShieldCheck size={17} className="shrink-0 text-indigo-600" />
          <div>
            <p>Calculation snapshot</p>
            <code className="block mt-1 text-[10px] break-all">
              {p.calculationHash}
            </code>
          </div>
        </footer>
      </article>
    </div>
  );
}
export function MyPayslipsPage() {
  const query = useQuery({
    queryKey: ["my-payslips"],
    queryFn: payrollApi.getMyPayslips,
  });
  const latest = query.data?.[0];
  return (
    <div>
      <PageHeader
        eyebrow="Payroll & time / My payslips"
        title="Your pay, made clear."
        description="Review and print your issued payroll statements."
      />
      <div className="metrics-grid">
        <Metric
          label="Issued statements"
          value={
            query.isPending
              ? "…"
              : query.isError
                ? "—"
                : (query.data?.length ?? 0)
          }
          detail="From paid payroll cycles"
          icon={<Receipt size={18} />}
        />
        <Metric
          label="Latest take-home"
          value={query.isPending ? "…" : latest ? money(latest.netPay) : "—"}
          detail={
            latest
              ? period(latest.year, latest.month)
              : "No statement issued yet"
          }
        />
        <Metric
          label="Latest gross pay"
          value={query.isPending ? "…" : latest ? money(latest.grossPay) : "—"}
          detail="Before deductions"
        />
        <Metric
          label="Latest deductions"
          value={
            query.isPending ? "…" : latest ? money(latest.totalDeductions) : "—"
          }
          detail="Withheld from gross pay"
        />
      </div>
      <Panel
        title="Statement history"
        subtitle="Open a statement for the itemized breakdown."
      >
        <QueryState
          loading={query.isPending}
          error={query.error}
          retry={() => void query.refetch()}
        />
        {query.data?.length ? (
          <div className="divide-y divide-slate-100">
            {query.data.map((p) => (
              <div
                className="p-5 sm:p-6 flex flex-col sm:flex-row justify-between sm:items-center gap-4"
                key={p.id}
              >
                <div className="flex gap-4 items-center">
                  <span className="empty-icon mb-0">
                    <Receipt size={22} />
                  </span>
                  <div>
                    <h3 className="text-sm font-semibold">
                      {period(p.year, p.month)}
                    </h3>
                    <p className="text-xs text-slate-500 mt-1">
                      {p.payslipNumber}
                    </p>
                    <p className="text-xs text-slate-500 mt-2">
                      Gross {money(p.grossPay)} · Deductions{" "}
                      {money(p.totalDeductions)}
                    </p>
                  </div>
                </div>
                <div className="flex items-center gap-5">
                  <div className="sm:text-right">
                    <p className="text-xs text-slate-500 mb-1">Net pay</p>
                    <strong className="text-lg font-semibold text-indigo-700">
                      {money(p.netPay)}
                    </strong>
                  </div>
                  <Link
                    className="btn btn-secondary"
                    to={`/payslip/${p.payrollItemId}`}
                  >
                    View & print
                  </Link>
                </div>
              </div>
            ))}
          </div>
        ) : (
          !query.isPending &&
          !query.isError && (
            <EmptyState
              title="No payslips issued yet"
              description="Statements appear after payroll is marked paid."
            />
          )
        )}
      </Panel>
    </div>
  );
}
