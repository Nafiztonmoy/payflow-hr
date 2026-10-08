import React, { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { auditApi } from "../api/commonApis";
import { PageHeader } from "../components/ui/Workspace";
import { Search, X, ShieldCheck, FileCode } from "lucide-react";

export const AuditLogsPage: React.FC = () => {
  const [actionFilter, setActionFilter] = useState("");
  const [entityFilter, setEntityFilter] = useState("");
  const [selectedLog, setSelectedLog] = useState<any | null>(null);

  const { data: auditData, isLoading } = useQuery({
    queryKey: ["audit-logs", actionFilter, entityFilter],
    queryFn: () =>
      auditApi.getLogs({
        action: actionFilter || undefined,
        entityType: entityFilter || undefined,
      }),
  });

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Governance / Audit trail"
        title="A clear trail of every change."
        description="Inspect payroll approvals, compensation updates and attendance corrections."
        actions={
          <span className="text-xs text-slate-500">Admin workspace</span>
        }
      />

      {/* Filter toolbar */}
      <div className="bg-white p-4 sm:p-5 rounded-2xl border border-slate-200/90 shadow-[0_1px_3px_rgba(0,0,0,0.03)] flex flex-wrap items-center gap-3">
        <div className="relative flex-1 min-w-[240px]">
          <Search className="w-4 h-4 text-slate-400 absolute left-3.5 top-3" />
          <input
            type="text"
            placeholder="Filter by action (e.g. CALCULATE, APPROVE, RESOLVE)..."
            value={actionFilter}
            onChange={(e) => setActionFilter(e.target.value)}
            className="w-full pl-10 pr-3.5 py-2 text-xs border border-slate-300 rounded-xl focus:outline-hidden focus:ring-2 focus:ring-indigo-500 bg-white"
          />
        </div>

        <input
          type="text"
          placeholder="Filter by entity (e.g. PayrollRun, Employee)..."
          value={entityFilter}
          onChange={(e) => setEntityFilter(e.target.value)}
          className="w-full sm:w-64 px-3.5 py-2 text-xs border border-slate-300 rounded-xl focus:outline-hidden focus:ring-2 focus:ring-indigo-500 bg-white"
        />

        {(actionFilter || entityFilter) && (
          <button
            onClick={() => {
              setActionFilter("");
              setEntityFilter("");
            }}
            className="px-3 py-2 text-xs text-slate-500 hover:text-slate-800 font-medium transition-colors cursor-pointer"
          >
            Clear filters
          </button>
        )}
      </div>

      {/* Audit Log Table */}
      <div className="bg-white rounded-2xl border border-slate-200/90 shadow-[0_1px_3px_rgba(0,0,0,0.03)] overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead className="bg-slate-50 text-slate-500 font-semibold uppercase tracking-wider border-b border-slate-200/80">
              <tr>
                <th className="py-3.5 px-4">Timestamp (UTC)</th>
                <th className="py-3.5 px-4">Actor</th>
                <th className="py-3.5 px-4">Action</th>
                <th className="py-3.5 px-4">Entity Type</th>
                <th className="py-3.5 px-4">Summary Description</th>
                <th className="py-3.5 px-4">Correlation ID</th>
                <th className="py-3.5 px-4 text-center">JSON Diff</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {isLoading ? (
                <tr>
                  <td colSpan={7} className="py-12 text-center text-slate-500">
                    <div className="w-7 h-7 border-3 border-indigo-600 border-t-transparent rounded-full animate-spin mx-auto mb-2" />
                    Loading immutable audit trail...
                  </td>
                </tr>
              ) : auditData?.items && auditData.items.length > 0 ? (
                auditData.items.map((log: any) => (
                  <tr
                    key={log.id}
                    onClick={() => setSelectedLog(log)}
                    className="hover:bg-slate-50/80 cursor-pointer transition-colors"
                  >
                    <td className="py-3.5 px-4 text-slate-500 text-[11px] font-mono whitespace-nowrap">
                      {new Date(
                        log.timestampUtc || log.createdAtUtc,
                      ).toLocaleString()}
                    </td>
                    <td className="py-3.5 px-4">
                      <span className="font-bold text-slate-900">
                        {log.actorEmail}
                      </span>
                      <span className="text-[10px] text-slate-400 block font-mono">
                        ({log.actorRole})
                      </span>
                    </td>
                    <td className="py-3.5 px-4">
                      <span className="bg-indigo-50 text-indigo-700 px-2 py-0.5 rounded-md text-[11px] font-mono font-bold border border-indigo-100">
                        {log.action}
                      </span>
                    </td>
                    <td className="py-3.5 px-4 text-slate-700 font-medium">
                      {log.entityType}
                      {log.entityId && (
                        <span className="text-[10px] text-slate-400 block font-mono">
                          ID: {log.entityId.slice(0, 8)}...
                        </span>
                      )}
                    </td>
                    <td className="py-3.5 px-4 text-slate-800 max-w-xs truncate">
                      {log.summary}
                    </td>
                    <td className="py-3.5 px-4 text-slate-500 font-mono text-[10px]">
                      {log.correlationId
                        ? log.correlationId.slice(0, 16)
                        : "system"}
                    </td>
                    <td className="py-3.5 px-4 text-center">
                      <button
                        onClick={(e) => {
                          e.stopPropagation();
                          setSelectedLog(log);
                        }}
                        className="px-2.5 py-1 bg-slate-100 hover:bg-indigo-50 hover:text-indigo-700 border border-slate-200 text-slate-700 rounded-lg text-[11px] font-semibold transition-colors inline-flex items-center"
                      >
                        <FileCode className="w-3 h-3 mr-1" /> Inspect
                      </button>
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={7} className="py-12 text-center text-slate-500">
                    No audit records found matching query parameters.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Audit Log Modal Detail View */}
      {selectedLog && (
        <div className="fixed inset-0 z-50 bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl shadow-2xl max-w-2xl w-full p-6 max-h-[90vh] overflow-y-auto border border-slate-200 animate-in fade-in zoom-in-95 duration-150">
            <div className="flex items-center justify-between pb-4 border-b border-slate-100">
              <div className="flex items-center space-x-2">
                <ShieldCheck className="w-5 h-5 text-indigo-600" />
                <h3 className="text-base font-bold text-slate-900">
                  Audit Record Detail
                </h3>
              </div>
              <button
                aria-label="Close dialog"
                onClick={() => setSelectedLog(null)}
                className="text-slate-400 hover:text-slate-600 p-1.5 rounded-lg"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <div className="mt-4 space-y-4 text-xs">
              <div className="grid grid-cols-2 gap-3 bg-slate-50 p-4 rounded-xl border border-slate-200/80">
                <div>
                  <span className="text-slate-400 font-semibold uppercase text-[10px] tracking-wider">
                    Event Action
                  </span>
                  <div className="font-bold text-indigo-700 font-mono mt-0.5">
                    {selectedLog.action}
                  </div>
                </div>
                <div>
                  <span className="text-slate-400 font-semibold uppercase text-[10px] tracking-wider">
                    Acting User
                  </span>
                  <div className="font-semibold text-slate-800 mt-0.5">
                    {selectedLog.actorEmail} ({selectedLog.actorRole})
                  </div>
                </div>
                <div>
                  <span className="text-slate-400 font-semibold uppercase text-[10px] tracking-wider">
                    Timestamp (UTC)
                  </span>
                  <div className="font-mono text-slate-800 mt-0.5">
                    {new Date(
                      selectedLog.timestampUtc || selectedLog.createdAtUtc,
                    ).toUTCString()}
                  </div>
                </div>
                <div>
                  <span className="text-slate-400 font-semibold uppercase text-[10px] tracking-wider">
                    Correlation Trace ID
                  </span>
                  <div className="font-mono text-slate-800 mt-0.5">
                    {selectedLog.correlationId || "N/A"}
                  </div>
                </div>
              </div>

              <div>
                <span className="text-slate-400 font-semibold uppercase text-[10px] tracking-wider block mb-1">
                  Human-Readable Event Summary
                </span>
                <p className="p-3 bg-slate-50 rounded-xl border border-slate-200 text-slate-800 font-medium leading-relaxed">
                  {selectedLog.summary}
                </p>
              </div>

              {selectedLog.beforeValuesJson && (
                <div>
                  <span className="text-slate-400 font-semibold uppercase text-[10px] tracking-wider block mb-1">
                    Before State Snapshot (JSON)
                  </span>
                  <pre className="p-3 bg-slate-900 text-slate-200 rounded-xl text-[11px] font-mono overflow-x-auto max-h-40 border border-slate-800">
                    {(() => {
                      try {
                        return JSON.stringify(
                          JSON.parse(selectedLog.beforeValuesJson),
                          null,
                          2,
                        );
                      } catch {
                        return selectedLog.beforeValuesJson;
                      }
                    })()}
                  </pre>
                </div>
              )}

              {selectedLog.afterValuesJson && (
                <div>
                  <span className="text-slate-400 font-semibold uppercase text-[10px] tracking-wider block mb-1">
                    After State Snapshot (JSON)
                  </span>
                  <pre className="p-3 bg-slate-900 text-slate-200 rounded-xl text-[11px] font-mono overflow-x-auto max-h-40 border border-slate-800">
                    {(() => {
                      try {
                        return JSON.stringify(
                          JSON.parse(selectedLog.afterValuesJson),
                          null,
                          2,
                        );
                      } catch {
                        return selectedLog.afterValuesJson;
                      }
                    })()}
                  </pre>
                </div>
              )}
            </div>

            <div className="mt-6 flex justify-end">
              <button
                onClick={() => setSelectedLog(null)}
                className="px-4 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-xl text-xs font-semibold transition-colors cursor-pointer"
              >
                Close Inspector
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
