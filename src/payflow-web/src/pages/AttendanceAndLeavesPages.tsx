import React, { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  attendanceApi,
  type AttendanceRecordDto,
  leaveApi,
} from "../api/commonApis";
import { useAuth } from "../context/auth";
import { Modal } from "../components/ui/Modal";
import { PageHeader, QueryState } from "../components/ui/Workspace";
import { Badge } from "../components/Badge";
import { Plus, X } from "lucide-react";

export const AttendancePage: React.FC = () => {
  const queryClient = useQueryClient();

  const { user } = useAuth();
  const canCorrect =
    user?.role === "Admin" || user?.role === "HR" || user?.role === "Manager";
  const [month, setMonth] = useState(() => new Date().getMonth() + 1);
  const [year, setYear] = useState(() => new Date().getFullYear());

  const [isCorrectModalOpen, setIsCorrectModalOpen] = useState(false);
  const [selectedRecord, setSelectedRecord] =
    useState<AttendanceRecordDto | null>(null);
  const [correctionData, setCorrectionData] = useState({
    reason: "",
    workedMinutes: 480,
    overtimeMinutes: 0,
    status: "Present",
  });

  const { data: records, isLoading } = useQuery({
    queryKey: ["attendance", month, year],
    queryFn: () => attendanceApi.getAttendance({ month, year }),
  });

  const correctMutation = useMutation({
    mutationFn: () =>
      attendanceApi.correctAttendance(selectedRecord!.id, {
        workedMinutes: correctionData.workedMinutes,
        overtimeMinutes: correctionData.overtimeMinutes,
        status: correctionData.status,
        reason: correctionData.reason,
      }),
    onSuccess: () => {
      setIsCorrectModalOpen(false);
      queryClient.invalidateQueries({ queryKey: ["attendance", month, year] });
    },
  });

  // Calculate totals
  const totalWorkedMins =
    records?.reduce(
      (acc: number, r: AttendanceRecordDto) => acc + r.workedMinutes,
      0,
    ) || 0;
  const totalOvertime =
    records?.reduce(
      (acc: number, r: AttendanceRecordDto) => acc + r.overtimeMinutes,
      0,
    ) || 0;
  const totalWorkedHours = (totalWorkedMins / 60).toFixed(1);

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Payroll & time / Attendance"
        title="Every hour, accounted for."
        description="Review recorded shifts, overtime and attendance corrections."
        actions={
          <>
            <label className="sr-only" htmlFor="attendance-month">
              Attendance month
            </label>
            <select
              id="attendance-month"
              value={month}
              onChange={(e) => setMonth(Number(e.target.value))}
              className="border border-slate-200 rounded-lg px-3 py-2 bg-white text-xs"
            >
              {Array.from({ length: 12 }, (_, i) => (
                <option value={i + 1} key={i}>
                  {new Date(2026, i).toLocaleDateString("en-US", {
                    month: "long",
                  })}
                </option>
              ))}
            </select>
            <label className="sr-only" htmlFor="attendance-year">
              Attendance year
            </label>
            <input
              id="attendance-year"
              type="number"
              min="2000"
              max="2100"
              value={year}
              onChange={(e) => {
                const n = Number(e.target.value);
                if (n >= 2000 && n <= 2100) setYear(n);
              }}
              className="border border-slate-200 rounded-lg px-3 py-2 bg-white text-xs w-24"
            />
          </>
        }
      />

      {/* Summary KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-[0_1px_2px_rgba(0,0,0,0.03)]">
          <span className="text-[11px] font-semibold text-slate-400 uppercase tracking-wider">
            Total Recorded Hours
          </span>
          <div className="text-2xl sm:text-3xl font-extrabold text-slate-900 mt-1 font-mono">
            {isLoading ? "…" : records ? `${totalWorkedHours} hrs` : "—"}
          </div>
          <span className="text-xs text-slate-500">
            Period: {year}-{month.toString().padStart(2, "0")}
          </span>
        </div>

        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-[0_1px_2px_rgba(0,0,0,0.03)]">
          <span className="text-[11px] font-semibold text-slate-400 uppercase tracking-wider">
            Total Overtime Hours
          </span>
          <div className="text-2xl sm:text-3xl font-extrabold text-indigo-600 mt-1 font-mono">
            {isLoading
              ? "…"
              : records
                ? `${(totalOvertime / 60).toFixed(1)} hrs`
                : "—"}
          </div>
          <span className="text-xs text-indigo-600 font-medium">
            Overtime recorded for this period
          </span>
        </div>

        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-[0_1px_2px_rgba(0,0,0,0.03)]">
          <span className="text-[11px] font-semibold text-slate-400 uppercase tracking-wider">
            Punched Attendance Records
          </span>
          <div className="text-2xl sm:text-3xl font-extrabold text-emerald-700 mt-1">
            {isLoading ? "…" : (records?.length ?? "—")}
          </div>
          <span className="text-xs text-emerald-600 font-medium">
            Recorded attendance entries
          </span>
        </div>
      </div>

      {/* Attendance Table */}
      <div className="bg-white rounded-2xl border border-slate-200/90 shadow-[0_1px_3px_rgba(0,0,0,0.03)] overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead className="bg-slate-50 text-slate-500 font-semibold uppercase tracking-wider border-b border-slate-200/80">
              <tr>
                <th className="py-3.5 px-4">Date</th>
                <th className="py-3.5 px-4">Employee</th>
                <th className="py-3.5 px-4 text-center">Status</th>
                <th className="py-3.5 px-4 text-right">Worked (Mins)</th>
                <th className="py-3.5 px-4 text-right">Overtime (Mins)</th>
                <th className="py-3.5 px-4">Punch Source</th>
                <th className="py-3.5 px-4 text-center">Action</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {isLoading ? (
                <tr>
                  <td colSpan={7} className="py-12 text-center text-slate-500">
                    <div className="w-7 h-7 border-3 border-indigo-600 border-t-transparent rounded-full animate-spin mx-auto mb-2" />
                    Loading attendance records...
                  </td>
                </tr>
              ) : records && records.length > 0 ? (
                records.map((r: AttendanceRecordDto) => (
                  <tr
                    key={r.id}
                    className="hover:bg-slate-50/80 transition-colors"
                  >
                    <td className="py-3.5 px-4 font-mono font-medium text-slate-900">
                      {r.date}
                    </td>
                    <td className="py-3.5 px-4">
                      <span className="font-bold text-slate-900">
                        {r.employeeName}
                      </span>
                      <span className="text-slate-400 text-[11px] ml-1.5 font-mono">
                        ({r.employeeNo})
                      </span>
                    </td>
                    <td className="py-3.5 px-4 text-center">
                      <Badge status={r.status} size="sm" />
                    </td>
                    <td className="py-3.5 px-4 text-right font-mono text-slate-700">
                      {r.workedMinutes}m
                    </td>
                    <td className="py-3.5 px-4 text-right font-mono font-semibold text-indigo-700">
                      {r.overtimeMinutes > 0 ? `+${r.overtimeMinutes}m` : "0m"}
                    </td>
                    <td className="py-3.5 px-4 text-slate-500">{r.source}</td>
                    <td className="py-3.5 px-4 text-center">
                      {canCorrect && (
                        <button
                          onClick={() => {
                            setSelectedRecord(r);
                            setCorrectionData({
                              reason: r.correctionReason || "",
                              workedMinutes: r.workedMinutes,
                              overtimeMinutes: r.overtimeMinutes || 0,
                              status: r.status,
                            });
                            setIsCorrectModalOpen(true);
                          }}
                          className="px-2.5 py-1 bg-slate-100 hover:bg-indigo-50 hover:text-indigo-700 border border-slate-200 text-slate-700 rounded-lg text-[11px] font-semibold transition-colors cursor-pointer"
                        >
                          Correct Punch
                        </button>
                      )}
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={7} className="py-12 text-center text-slate-500">
                    No attendance records found for this period.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Modal: Correct Attendance */}
      {isCorrectModalOpen && (
        <div className="fixed inset-0 z-50 bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl shadow-2xl max-w-md w-full p-6 border border-slate-200 animate-in fade-in zoom-in-95 duration-150">
            <div className="flex items-center justify-between pb-3 border-b border-slate-100">
              <h3 className="text-base font-bold text-slate-900">
                Attendance Supervisor Correction
              </h3>
              <button
                aria-label="Close dialog"
                onClick={() => setIsCorrectModalOpen(false)}
                className="text-slate-400 hover:text-slate-600 p-1 rounded-lg"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <p className="text-xs text-slate-500 mt-3">
              Employee:{" "}
              <strong className="text-slate-900">
                {selectedRecord?.employeeName}
              </strong>{" "}
              on {selectedRecord?.date}. All adjustments are captured in the
              security audit trail.
            </p>

            <QueryState loading={false} error={correctMutation.error} />
            <div className="mt-4 space-y-3.5 text-xs">
              <div>
                <label
                  htmlFor="attendanceandleavespages-field-1"
                  className="block font-semibold text-slate-700 uppercase tracking-wider text-[10px] mb-1"
                >
                  Status
                </label>
                <select
                  id="attendanceandleavespages-field-1"
                  value={correctionData.status}
                  onChange={(e) =>
                    setCorrectionData({
                      ...correctionData,
                      status: e.target.value,
                    })
                  }
                  className="w-full px-3 py-2 border border-slate-300 rounded-xl bg-white focus:outline-hidden focus:ring-2 focus:ring-indigo-500"
                >
                  <option value="Present">Present</option>
                  <option value="Absent">Absent</option>
                  <option value="OnLeave">On Leave</option>
                  <option value="HalfDay">Half Day</option>
                </select>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label
                    htmlFor="attendanceandleavespages-field-2"
                    className="block font-semibold text-slate-700 uppercase tracking-wider text-[10px] mb-1"
                  >
                    Worked Minutes
                  </label>
                  <input
                    id="attendanceandleavespages-field-2"
                    type="number"
                    value={correctionData.workedMinutes}
                    onChange={(e) =>
                      setCorrectionData({
                        ...correctionData,
                        workedMinutes: parseInt(e.target.value) || 0,
                      })
                    }
                    className="w-full px-3 py-2 border border-slate-300 rounded-xl font-mono focus:outline-hidden focus:ring-2 focus:ring-indigo-500"
                  />
                </div>
                <div>
                  <label
                    htmlFor="attendanceandleavespages-field-3"
                    className="block font-semibold text-slate-700 uppercase tracking-wider text-[10px] mb-1"
                  >
                    Overtime Minutes
                  </label>
                  <input
                    id="attendanceandleavespages-field-3"
                    type="number"
                    value={correctionData.overtimeMinutes}
                    onChange={(e) =>
                      setCorrectionData({
                        ...correctionData,
                        overtimeMinutes: parseInt(e.target.value) || 0,
                      })
                    }
                    className="w-full px-3 py-2 border border-slate-300 rounded-xl font-mono focus:outline-hidden focus:ring-2 focus:ring-indigo-500"
                  />
                </div>
              </div>

              <div>
                <label
                  htmlFor="attendanceandleavespages-field-4"
                  className="block font-semibold text-slate-700 uppercase tracking-wider text-[10px] mb-1"
                >
                  Supervisor Audit Justification
                </label>
                <textarea
                  id="attendanceandleavespages-field-4"
                  rows={3}
                  value={correctionData.reason}
                  onChange={(e) =>
                    setCorrectionData({
                      ...correctionData,
                      reason: e.target.value,
                    })
                  }
                  className="w-full px-3 py-2 border border-slate-300 rounded-xl focus:outline-hidden focus:ring-2 focus:ring-indigo-500"
                />
              </div>
            </div>

            <div className="mt-6 flex justify-end space-x-2.5">
              <button
                onClick={() => setIsCorrectModalOpen(false)}
                className="px-4 py-2 bg-slate-100 text-slate-700 rounded-xl text-xs font-semibold hover:bg-slate-200 transition-colors"
              >
                Cancel
              </button>
              <button
                onClick={() => correctMutation.mutate()}
                disabled={
                  correctMutation.isPending ||
                  !correctionData.reason.trim() ||
                  correctionData.workedMinutes < 0 ||
                  correctionData.overtimeMinutes < 0
                }
                className="px-4 py-2 bg-indigo-600 text-white rounded-xl text-xs font-semibold hover:bg-indigo-700 transition-colors cursor-pointer"
              >
                {correctMutation.isPending ? "Saving..." : "Apply Correction"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export const LeavesPage: React.FC = () => {
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const [rejectId, setRejectId] = useState<string | null>(null);
  const [rejectReason, setRejectReason] = useState("");
  const [isRequestModalOpen, setIsRequestModalOpen] = useState(false);
  const [requestData, setRequestData] = useState(() => ({
    leaveTypeId: "",
    startDate: new Date().toISOString().slice(0, 10),
    endDate: new Date().toISOString().slice(0, 10),
    reason: "",
  }));

  const canApprove =
    user?.role === "Admin" || user?.role === "HR" || user?.role === "Manager";

  const { data: requests, isLoading: isRequestsLoading } = useQuery({
    queryKey: ["leave-requests", canApprove ? "all" : "my"],
    queryFn: () => leaveApi.getRequests(),
  });

  const { data: myBalances } = useQuery({
    queryKey: ["my-leave-balances"],
    queryFn: leaveApi.getMyBalances,
    enabled: !!user?.employeeId,
  });

  const submitMutation = useMutation({
    mutationFn: () => leaveApi.submitRequest(requestData),
    onSuccess: () => {
      setIsRequestModalOpen(false);
      queryClient.invalidateQueries({ queryKey: ["leave-requests"] });
      queryClient.invalidateQueries({ queryKey: ["my-leave-balances"] });
    },
  });

  const approveMutation = useMutation({
    mutationFn: (id: string) => leaveApi.approveRequest(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["leave-requests"] });
      queryClient.invalidateQueries({ queryKey: ["my-leave-balances"] });
    },
  });

  const rejectMutation = useMutation({
    mutationFn: (id: string) => leaveApi.rejectRequest(id, rejectReason.trim()),
    onSuccess: () => {
      setRejectId(null);
      queryClient.invalidateQueries({ queryKey: ["leave-requests"] });
    },
  });

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Payroll & time / Time off"
        title="A little time for yourself."
        description="Plan your time off, review balances and keep requests moving."
        actions={
          user?.employeeId && (
            <button
              className="btn btn-primary"
              disabled={!myBalances?.length}
              onClick={() => {
                setRequestData({
                  ...requestData,
                  leaveTypeId: myBalances?.[0]?.leaveTypeId ?? "",
                });
                setIsRequestModalOpen(true);
              }}
            >
              <Plus size={16} />
              Request time off
            </button>
          )
        }
      />

      {/* Leave Balances Grid (Self-Service) */}
      {myBalances && myBalances.length > 0 && (
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          {myBalances.map((b) => (
            <div
              key={b.id}
              className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-[0_1px_2px_rgba(0,0,0,0.03)]"
            >
              <div className="flex items-center justify-between mb-2">
                <span className="text-xs font-bold text-slate-900">
                  {b.leaveTypeName}
                </span>
                <span
                  className={`text-[10px] font-semibold px-2 py-0.5 rounded-full ${
                    b.isPaid
                      ? "bg-emerald-50 text-emerald-700 border border-emerald-200"
                      : "bg-slate-100 text-slate-600 border border-slate-200"
                  }`}
                >
                  {b.isPaid ? "Paid" : "Unpaid"}
                </span>
              </div>
              <div className="text-3xl font-extrabold text-slate-900 mt-1 font-mono">
                {b.remainingDays} days
              </div>
              <p className="text-[11px] text-slate-500 mt-1.5">
                Allocated:{" "}
                <strong className="text-slate-700">{b.openingBalance}d</strong>{" "}
                • Used:{" "}
                <strong className="text-slate-700">{b.usedDays}d</strong> •
                Remaining:{" "}
                <strong className="text-emerald-700">{b.remainingDays}d</strong>
              </p>
            </div>
          ))}
        </div>
      )}

      {/* Leave Requests Table & Manager Approvals Inbox */}
      <div className="bg-white rounded-2xl border border-slate-200/90 shadow-[0_1px_3px_rgba(0,0,0,0.03)] overflow-hidden">
        <div className="p-4 sm:p-5 border-b border-slate-200/80">
          <h3 className="text-sm font-bold text-slate-900">
            {canApprove
              ? "Leave Requests & Approvals Inbox"
              : "My Leave Requests"}
          </h3>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead className="bg-slate-50 text-slate-500 font-semibold uppercase tracking-wider border-b border-slate-200/80">
              <tr>
                <th className="py-3.5 px-4">Employee</th>
                <th className="py-3.5 px-4">Leave Type</th>
                <th className="py-3.5 px-4">Duration Range</th>
                <th className="py-3.5 px-4 text-center">Days</th>
                <th className="py-3.5 px-4">Reason</th>
                <th className="py-3.5 px-4 text-center">Status</th>
                {canApprove && (
                  <th className="py-3.5 px-4 text-center">Manager Review</th>
                )}
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {isRequestsLoading ? (
                <tr>
                  <td colSpan={7} className="py-12 text-center text-slate-500">
                    <div className="w-7 h-7 border-3 border-indigo-600 border-t-transparent rounded-full animate-spin mx-auto mb-2" />
                    Loading leave requests...
                  </td>
                </tr>
              ) : requests && requests.length > 0 ? (
                requests.map((req) => (
                  <tr
                    key={req.id}
                    className="hover:bg-slate-50/80 transition-colors"
                  >
                    <td className="py-3.5 px-4 font-bold text-slate-900">
                      {req.employeeName}
                    </td>
                    <td className="py-3.5 px-4 font-medium text-slate-700">
                      {req.leaveTypeName}
                    </td>
                    <td className="py-3.5 px-4 font-mono text-slate-600">
                      {req.startDate} to {req.endDate}
                    </td>
                    <td className="py-3.5 px-4 text-center font-bold text-slate-900 font-mono">
                      {req.dayCount}d
                    </td>
                    <td className="py-3.5 px-4 text-slate-600 max-w-xs truncate">
                      {req.reason}
                    </td>
                    <td className="py-3.5 px-4 text-center">
                      <Badge status={req.status} size="sm" />
                    </td>
                    {canApprove && (
                      <td className="py-3.5 px-4 text-center">
                        {req.status === "Pending" ? (
                          <div className="flex items-center justify-center space-x-1.5">
                            <button
                              disabled={
                                approveMutation.isPending ||
                                rejectMutation.isPending
                              }
                              onClick={() => approveMutation.mutate(req.id)}
                              className="px-2.5 py-1 bg-emerald-600 hover:bg-emerald-700 text-white rounded-lg text-[11px] font-semibold transition-colors cursor-pointer"
                            >
                              Approve
                            </button>
                            <button
                              disabled={
                                approveMutation.isPending ||
                                rejectMutation.isPending
                              }
                              onClick={() => {
                                setRejectId(req.id);
                                setRejectReason("");
                              }}
                              className="px-2.5 py-1 bg-rose-50 hover:bg-rose-100 text-rose-700 border border-rose-200 rounded-lg text-[11px] font-semibold transition-colors cursor-pointer"
                            >
                              Reject
                            </button>
                          </div>
                        ) : (
                          <span className="text-slate-400 text-[11px] italic">
                            Processed
                          </span>
                        )}
                      </td>
                    )}
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={7} className="py-12 text-center text-slate-500">
                    No time off requests found.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {rejectId && (
        <Modal
          title="Reject time off request"
          description="Explain the decision so the employee has a clear next step."
          close={() => setRejectId(null)}
        >
          <QueryState loading={false} error={rejectMutation.error} />
          <form
            className="mt-5 space-y-4"
            onSubmit={(e) => {
              e.preventDefault();
              rejectMutation.mutate(rejectId);
            }}
          >
            <label className="block text-xs font-semibold">
              Review note
              <textarea
                className="w-full border border-slate-300 rounded-lg p-3 mt-2"
                required
                rows={3}
                value={rejectReason}
                onChange={(e) => setRejectReason(e.target.value)}
              />
            </label>
            <button
              className="btn btn-primary w-full"
              disabled={rejectMutation.isPending || !rejectReason.trim()}
            >
              {rejectMutation.isPending ? "Saving…" : "Confirm rejection"}
            </button>
          </form>
        </Modal>
      )}

      {/* Modal: Submit Leave Request */}
      {isRequestModalOpen && (
        <div className="fixed inset-0 z-50 bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl shadow-2xl max-w-md w-full p-6 border border-slate-200 animate-in fade-in zoom-in-95 duration-150">
            <div className="flex items-center justify-between pb-3 border-b border-slate-100">
              <h3 className="text-base font-bold text-slate-900">
                Request Time Off
              </h3>
              <button
                aria-label="Close dialog"
                onClick={() => setIsRequestModalOpen(false)}
                className="text-slate-400 hover:text-slate-600 p-1 rounded-lg"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <p className="text-xs text-slate-500 mt-3">
              Select date range and leave type. Working days will be validated
              against your allocated balance.
            </p>

            <QueryState loading={false} error={submitMutation.error} />
            <div className="mt-4 space-y-3.5 text-xs">
              <div>
                <label
                  htmlFor="attendanceandleavespages-field-5"
                  className="block font-semibold text-slate-700 uppercase tracking-wider text-[10px] mb-1"
                >
                  Leave Type
                </label>
                <select
                  id="attendanceandleavespages-field-5"
                  value={requestData.leaveTypeId}
                  onChange={(e) =>
                    setRequestData({
                      ...requestData,
                      leaveTypeId: e.target.value,
                    })
                  }
                  className="w-full px-3 py-2 border border-slate-300 rounded-xl bg-white focus:outline-hidden focus:ring-2 focus:ring-indigo-500"
                >
                  {myBalances?.map((b) => (
                    <option key={b.leaveTypeId} value={b.leaveTypeId}>
                      {b.leaveTypeName} ({b.remainingDays} days remaining)
                    </option>
                  ))}
                </select>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label
                    htmlFor="attendanceandleavespages-field-6"
                    className="block font-semibold text-slate-700 uppercase tracking-wider text-[10px] mb-1"
                  >
                    Start Date
                  </label>
                  <input
                    id="attendanceandleavespages-field-6"
                    type="date"
                    value={requestData.startDate}
                    onChange={(e) =>
                      setRequestData({
                        ...requestData,
                        startDate: e.target.value,
                      })
                    }
                    className="w-full px-3 py-2 border border-slate-300 rounded-xl focus:outline-hidden focus:ring-2 focus:ring-indigo-500"
                  />
                </div>
                <div>
                  <label
                    htmlFor="attendanceandleavespages-field-7"
                    className="block font-semibold text-slate-700 uppercase tracking-wider text-[10px] mb-1"
                  >
                    End Date
                  </label>
                  <input
                    id="attendanceandleavespages-field-7"
                    type="date"
                    value={requestData.endDate}
                    onChange={(e) =>
                      setRequestData({
                        ...requestData,
                        endDate: e.target.value,
                      })
                    }
                    className="w-full px-3 py-2 border border-slate-300 rounded-xl focus:outline-hidden focus:ring-2 focus:ring-indigo-500"
                  />
                </div>
              </div>

              <div>
                <label
                  htmlFor="attendanceandleavespages-field-8"
                  className="block font-semibold text-slate-700 uppercase tracking-wider text-[10px] mb-1"
                >
                  Reason for Request
                </label>
                <textarea
                  id="attendanceandleavespages-field-8"
                  rows={3}
                  value={requestData.reason}
                  onChange={(e) =>
                    setRequestData({ ...requestData, reason: e.target.value })
                  }
                  className="w-full px-3 py-2 border border-slate-300 rounded-xl focus:outline-hidden focus:ring-2 focus:ring-indigo-500"
                />
              </div>
            </div>

            <div className="mt-6 flex justify-end space-x-2.5">
              <button
                onClick={() => setIsRequestModalOpen(false)}
                className="px-4 py-2 bg-slate-100 text-slate-700 rounded-xl text-xs font-semibold hover:bg-slate-200 transition-colors"
              >
                Cancel
              </button>
              <button
                onClick={() => submitMutation.mutate()}
                disabled={
                  submitMutation.isPending ||
                  !requestData.leaveTypeId ||
                  !requestData.startDate ||
                  requestData.endDate < requestData.startDate ||
                  !requestData.reason.trim()
                }
                className="px-4 py-2 bg-indigo-600 text-white rounded-xl text-xs font-semibold hover:bg-indigo-700 transition-colors cursor-pointer"
              >
                {submitMutation.isPending ? "Submitting..." : "Submit Request"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
