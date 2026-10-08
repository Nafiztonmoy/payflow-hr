import React, { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { employeesApi } from "../api/employeesApi";
import { orgApi } from "../api/commonApis";
import { useAuth } from "../context/auth";
import { PageHeader, QueryState } from "../components/ui/Workspace";
import { Badge } from "../components/Badge";
import { Search, Eye, X, CreditCard, Plus } from "lucide-react";

export const EmployeesPage: React.FC = () => {
  const { user } = useAuth();
  const queryClient = useQueryClient();

  // Filters
  const [search, setSearch] = useState("");
  const [departmentId, setDepartmentId] = useState("");
  const [status, setStatus] = useState("");
  const [page, setPage] = useState(1);

  // Selected Employee for View/Edit
  const [selectedEmpId, setSelectedEmpId] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<"profile" | "compensation">(
    "profile",
  );

  // New Compensation Modal
  const [isCompModalOpen, setIsCompModalOpen] = useState(false);
  const [newCompData, setNewCompData] = useState(() => ({
    salaryStructureId: "",
    baseSalary: 0,
    effectiveFrom: new Date().toISOString().split("T")[0],
    remarks: "",
  }));

  // Queries
  const { data: employeeData, isLoading } = useQuery({
    queryKey: ["employees", search, departmentId, status, page],
    queryFn: () =>
      employeesApi.getEmployees({
        search: search || undefined,
        departmentId: departmentId || undefined,
        status: status || undefined,
        page,
        pageSize: 30,
      }),
  });

  const { data: departments } = useQuery({
    queryKey: ["departments"],
    queryFn: orgApi.getDepartments,
  });

  const { data: salaryStructures } = useQuery({
    queryKey: ["salary-structures"],
    queryFn: orgApi.getSalaryStructures,
    enabled: user?.role === "Admin" || user?.role === "HR",
  });

  // Query selected employee detail
  const { data: selectedEmployee } = useQuery({
    queryKey: ["employee", selectedEmpId],
    queryFn: () => employeesApi.getEmployeeById(selectedEmpId!),
    enabled: !!selectedEmpId,
  });

  // Query selected employee compensation
  const { data: compensations } = useQuery({
    queryKey: ["compensation", selectedEmpId],
    queryFn: () => employeesApi.getCompensation(selectedEmpId!),
    enabled:
      !!selectedEmpId &&
      activeTab === "compensation" &&
      (user?.role === "Admin" ||
        user?.role === "HR" ||
        user?.role === "Accountant" ||
        user?.employeeId === selectedEmpId),
  });

  const addCompMutation = useMutation({
    mutationFn: () => employeesApi.addCompensation(selectedEmpId!, newCompData),
    onSuccess: () => {
      setIsCompModalOpen(false);
      queryClient.invalidateQueries({
        queryKey: ["compensation", selectedEmpId],
      });
      queryClient.invalidateQueries({ queryKey: ["payroll-runs"] });
    },
  });

  const canEdit = user?.role === "Admin" || user?.role === "HR";
  const canViewCompensation =
    user?.role === "Admin" ||
    user?.role === "HR" ||
    user?.role === "Accountant" ||
    user?.employeeId === selectedEmpId;

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Workforce / Employees"
        title="People"
        description="A clear view of your workforce, roles and compensation history."
        actions={
          <span className="text-xs text-slate-500">
            {isLoading
              ? "Loading directory…"
              : employeeData
                ? `${employeeData.total} employees`
                : "Directory unavailable"}
          </span>
        }
      />

      {/* Filter Toolbar */}
      <div className="bg-white p-4 sm:p-5 rounded-2xl border border-slate-200/90 shadow-[0_1px_3px_rgba(0,0,0,0.03)] flex flex-wrap items-center gap-3">
        <div className="relative flex-1 min-w-[240px]">
          <Search className="w-4 h-4 text-slate-400 absolute left-3.5 top-3" />
          <input
            type="text"
            placeholder="Search by employee name, email, or EMP-001..."
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
            className="w-full pl-10 pr-3.5 py-2 text-xs border border-slate-300 rounded-xl focus:outline-hidden focus:ring-2 focus:ring-indigo-500 bg-white"
          />
        </div>

        {user?.role !== "Manager" && (
          <select
            value={departmentId}
            onChange={(e) => {
              setDepartmentId(e.target.value);
              setPage(1);
            }}
            className="border border-slate-300 rounded-xl px-3.5 py-2 text-xs text-slate-700 bg-white focus:outline-hidden focus:ring-2 focus:ring-indigo-500 cursor-pointer"
          >
            <option value="">All Departments</option>
            {departments?.map((d: any) => (
              <option key={d.id} value={d.id}>
                {d.name} ({d.code})
              </option>
            ))}
          </select>
        )}

        <select
          value={status}
          onChange={(e) => {
            setStatus(e.target.value);
            setPage(1);
          }}
          className="border border-slate-300 rounded-xl px-3.5 py-2 text-xs text-slate-700 bg-white focus:outline-hidden focus:ring-2 focus:ring-indigo-500 cursor-pointer"
        >
          <option value="">All Statuses</option>
          <option value="Active">Active</option>
          <option value="OnLeave">On Leave</option>
          <option value="Terminated">Terminated</option>
        </select>

        {(search || departmentId || status) && (
          <button
            onClick={() => {
              setSearch("");
              setDepartmentId("");
              setStatus("");
              setPage(1);
            }}
            className="px-3 py-2 text-xs text-slate-500 hover:text-slate-800 font-medium transition-colors cursor-pointer"
          >
            Clear filters
          </button>
        )}
      </div>

      {/* Employees Table */}
      <div className="bg-white rounded-2xl border border-slate-200/90 shadow-[0_1px_3px_rgba(0,0,0,0.03)] overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead className="bg-slate-50 text-slate-500 font-semibold uppercase tracking-wider border-b border-slate-200/80">
              <tr>
                <th className="py-3.5 px-4">Employee</th>
                <th className="py-3.5 px-4">Department</th>
                <th className="py-3.5 px-4">Designation</th>
                <th className="py-3.5 px-4">Reporting Manager</th>
                <th className="py-3.5 px-4 text-center">Status</th>
                <th className="py-3.5 px-4 text-center">Action</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {isLoading ? (
                <tr>
                  <td colSpan={6} className="py-12 text-center text-slate-500">
                    <div className="w-7 h-7 border-3 border-indigo-600 border-t-transparent rounded-full animate-spin mx-auto mb-2" />
                    Loading workforce directory...
                  </td>
                </tr>
              ) : employeeData?.items && employeeData.items.length > 0 ? (
                employeeData.items.map((emp) => (
                  <tr
                    key={emp.id}
                    onClick={() => {
                      setSelectedEmpId(emp.id);
                      setActiveTab("profile");
                    }}
                    className="hover:bg-slate-50/80 cursor-pointer transition-colors"
                  >
                    <td className="py-3.5 px-4">
                      <div className="flex items-center space-x-3">
                        <div className="w-8 h-8 rounded-full bg-slate-100 text-slate-700 font-bold flex items-center justify-center text-xs shrink-0 border border-slate-200">
                          {emp.fullName
                            .split(" ")
                            .map((n) => n[0])
                            .filter(Boolean)
                            .slice(0, 2)
                            .join("") || "EP"}
                        </div>
                        <div>
                          <div className="font-bold text-slate-900">
                            {emp.fullName}
                          </div>
                          <div className="text-[11px] text-slate-500 font-mono">
                            {emp.employeeNo} • {emp.workEmail}
                          </div>
                        </div>
                      </div>
                    </td>
                    <td className="py-3.5 px-4 text-slate-700 font-medium">
                      {emp.departmentName}
                    </td>
                    <td className="py-3.5 px-4 text-slate-600">
                      {emp.designationTitle}
                    </td>
                    <td className="py-3.5 px-4 text-slate-500">
                      {emp.managerName ?? "Unassigned"}
                    </td>
                    <td className="py-3.5 px-4 text-center">
                      <Badge status={emp.status} size="sm" />
                    </td>
                    <td className="py-3.5 px-4 text-center">
                      <button
                        onClick={(e) => {
                          e.stopPropagation();
                          setSelectedEmpId(emp.id);
                        }}
                        className="px-2.5 py-1 bg-slate-100 hover:bg-indigo-50 hover:text-indigo-700 border border-slate-200 text-slate-700 rounded-lg text-[11px] font-semibold transition-colors inline-flex items-center"
                      >
                        <Eye className="w-3 h-3 mr-1" /> View Profile
                      </button>
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={6} className="py-12 text-center text-slate-500">
                    No employees matched the specified filters.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
        <div className="filter-footer">
          <span>
            {employeeData
              ? `${employeeData.items.length} of ${employeeData.total} employees · Page ${page}`
              : "Directory data unavailable"}
          </span>
          <div className="flex gap-2">
            <button
              className="btn btn-secondary"
              disabled={page === 1 || isLoading}
              onClick={() => setPage(page - 1)}
            >
              Previous
            </button>
            <button
              className="btn btn-secondary"
              disabled={
                !employeeData || page * 30 >= employeeData.total || isLoading
              }
              onClick={() => setPage(page + 1)}
            >
              Next
            </button>
          </div>
        </div>
      </div>

      {/* Employee Detail & Compensation History Modal */}
      {selectedEmpId && selectedEmployee && (
        <div className="fixed inset-0 z-50 bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl shadow-2xl max-w-2xl w-full p-6 max-h-[90vh] overflow-y-auto border border-slate-200 animate-in fade-in zoom-in-95 duration-150">
            <div className="flex items-center justify-between pb-4 border-b border-slate-100">
              <div className="flex items-center space-x-3.5">
                <div className="w-12 h-12 rounded-xl bg-linear-to-br from-indigo-600 to-indigo-800 text-white font-bold flex items-center justify-center text-base shadow-xs shrink-0">
                  {selectedEmployee.firstName[0]}
                  {selectedEmployee.lastName[0]}
                </div>
                <div>
                  <h3 className="text-lg font-extrabold text-slate-900">
                    {selectedEmployee.fullName}
                  </h3>
                  <p className="text-xs text-slate-500 font-medium">
                    {selectedEmployee.employeeNo} •{" "}
                    {selectedEmployee.departmentName} •{" "}
                    {selectedEmployee.designationTitle}
                  </p>
                </div>
              </div>
              <button
                aria-label="Close dialog"
                onClick={() => setSelectedEmpId(null)}
                className="text-slate-400 hover:text-slate-600 p-1.5 rounded-lg"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            {/* Navigation Tabs */}
            <div className="flex border-b border-slate-200 mt-4 text-xs font-semibold space-x-2">
              <button
                onClick={() => setActiveTab("profile")}
                className={`py-2 px-3 border-b-2 transition-colors ${
                  activeTab === "profile"
                    ? "border-indigo-600 text-indigo-600"
                    : "border-transparent text-slate-500 hover:text-slate-700"
                }`}
              >
                Profile & Direct Deposit
              </button>

              {canViewCompensation && (
                <button
                  onClick={() => setActiveTab("compensation")}
                  className={`py-2 px-3 border-b-2 transition-colors ${
                    activeTab === "compensation"
                      ? "border-indigo-600 text-indigo-600"
                      : "border-transparent text-slate-500 hover:text-slate-700"
                  }`}
                >
                  Compensation & Salary History
                </button>
              )}
            </div>

            <div className="py-4">
              {activeTab === "profile" && (
                <div className="space-y-4 text-xs">
                  <div className="grid grid-cols-2 gap-3.5 bg-slate-50/80 p-4 rounded-xl border border-slate-200/80">
                    <div>
                      <span className="text-slate-400 font-semibold uppercase text-[10px] tracking-wider">
                        Work Email
                      </span>
                      <div className="font-semibold text-slate-800 mt-0.5">
                        {selectedEmployee.workEmail}
                      </div>
                    </div>
                    <div>
                      <span className="text-slate-400 font-semibold uppercase text-[10px] tracking-wider">
                        Contact Phone
                      </span>
                      <div className="font-semibold text-slate-800 mt-0.5">
                        {selectedEmployee.phone ?? "N/A"}
                      </div>
                    </div>
                    <div>
                      <span className="text-slate-400 font-semibold uppercase text-[10px] tracking-wider">
                        Employment Commenced
                      </span>
                      <div className="font-semibold text-slate-800 mt-0.5">
                        {new Date(
                          selectedEmployee.joinDate,
                        ).toLocaleDateString()}
                      </div>
                    </div>
                    <div>
                      <span className="text-slate-400 font-semibold uppercase text-[10px] tracking-wider">
                        Employment Status
                      </span>
                      <div className="mt-1">
                        <Badge status={selectedEmployee.status} size="sm" />
                      </div>
                    </div>
                  </div>

                  {/* Payment Details */}
                  <div className="border border-slate-200/90 rounded-xl p-4 bg-white">
                    <div className="flex items-center justify-between mb-3">
                      <div className="flex items-center space-x-2 font-bold text-slate-900 text-xs sm:text-sm">
                        <CreditCard className="w-4 h-4 text-indigo-600" />
                        <span>Direct Deposit Banking Settings</span>
                      </div>
                      <Badge
                        status={selectedEmployee.paymentMethod}
                        size="sm"
                      />
                    </div>

                    <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 text-xs bg-slate-50/50 p-3 rounded-lg border border-slate-100">
                      <div>
                        <span className="text-slate-400 font-semibold uppercase text-[10px]">
                          Bank Institution
                        </span>
                        <div className="font-semibold text-slate-800 mt-0.5">
                          {selectedEmployee.bankName ?? "Not Configured"}
                        </div>
                      </div>
                      <div>
                        <span className="text-slate-400 font-semibold uppercase text-[10px]">
                          Account Number
                        </span>
                        <div className="font-semibold font-mono text-slate-800 mt-0.5">
                          {selectedEmployee.bankAccountNumber ?? (
                            <span className="text-rose-600 font-bold bg-rose-50 px-1.5 py-0.5 rounded text-[10px]">
                              MISSING (Blocks Payroll)
                            </span>
                          )}
                        </div>
                      </div>
                      <div>
                        <span className="text-slate-400 font-semibold uppercase text-[10px]">
                          ABA / Routing Number
                        </span>
                        <div className="font-semibold font-mono text-slate-800 mt-0.5">
                          {selectedEmployee.bankRoutingNumber ?? "N/A"}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              )}

              {activeTab === "compensation" && canViewCompensation && (
                <div className="space-y-4">
                  <div className="flex items-center justify-between">
                    <div>
                      <h4 className="text-xs font-bold uppercase tracking-wider text-slate-600">
                        Effective-Dated Compensation History
                      </h4>
                      <p className="text-[11px] text-slate-400">
                        Past compensation records are preserved immutably for
                        audit accuracy
                      </p>
                    </div>
                    {canEdit && (
                      <button
                        onClick={() => {
                          setNewCompData({
                            salaryStructureId:
                              compensations?.[0]?.salaryStructureId ||
                              salaryStructures?.[0]?.id ||
                              "",
                            baseSalary: compensations?.[0]?.baseSalary ?? 0,
                            effectiveFrom: new Date()
                              .toISOString()
                              .split("T")[0],
                            remarks: "",
                          });
                          setIsCompModalOpen(true);
                        }}
                        className="px-3 py-1.5 bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl text-xs font-semibold flex items-center cursor-pointer shadow-xs"
                      >
                        <Plus className="w-3.5 h-3.5 mr-1" /> Assign Revision
                      </button>
                    )}
                  </div>

                  <div className="divide-y divide-slate-100 border border-slate-200 rounded-xl overflow-hidden text-xs">
                    {compensations?.map((comp: any) => (
                      <div
                        key={comp.id}
                        className="p-3.5 flex items-center justify-between bg-white hover:bg-slate-50 transition-colors"
                      >
                        <div>
                          <div className="flex items-center space-x-2">
                            <span className="font-bold text-slate-900 text-sm font-mono">
                              $
                              {comp.baseSalary.toLocaleString(undefined, {
                                minimumFractionDigits: 2,
                              })}{" "}
                              / mo
                            </span>
                            {comp.isActive && (
                              <Badge status="Active" size="sm" />
                            )}
                          </div>
                          <p className="text-slate-500 text-[11px] mt-0.5">
                            Effective:{" "}
                            <strong className="text-slate-700">
                              {new Date(
                                comp.effectiveFrom,
                              ).toLocaleDateString()}
                            </strong>{" "}
                            to{" "}
                            {comp.effectiveTo
                              ? new Date(comp.effectiveTo).toLocaleDateString()
                              : "Present"}
                          </p>
                          {comp.remarks && (
                            <p className="text-slate-400 italic text-[11px] mt-0.5">
                              Note: {comp.remarks}
                            </p>
                          )}
                        </div>
                        <span className="text-[11px] font-mono text-slate-400 bg-slate-50 px-2 py-1 rounded border border-slate-200">
                          {comp.salaryStructureName}
                        </span>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Modal: Assign New Salary */}
      {isCompModalOpen && (
        <div className="fixed inset-0 z-50 bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl shadow-2xl max-w-md w-full p-6 border border-slate-200 animate-in fade-in zoom-in-95 duration-150">
            <div className="flex items-center justify-between pb-3 border-b border-slate-100">
              <h3 className="text-base font-bold text-slate-900">
                Assign New Compensation Contract
              </h3>
              <button
                aria-label="Close dialog"
                onClick={() => setIsCompModalOpen(false)}
                className="text-slate-400 hover:text-slate-600 p-1"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <QueryState loading={false} error={addCompMutation.error} />
            <div className="mt-4 space-y-3.5 text-xs">
              <div>
                <label
                  htmlFor="employeespage-field-1"
                  className="block font-semibold text-slate-700 uppercase tracking-wider text-[10px] mb-1"
                >
                  Salary Structure Plan
                </label>
                <select
                  id="employeespage-field-1"
                  value={newCompData.salaryStructureId}
                  onChange={(e) =>
                    setNewCompData({
                      ...newCompData,
                      salaryStructureId: e.target.value,
                    })
                  }
                  className="w-full px-3 py-2 border border-slate-300 rounded-xl bg-white focus:outline-hidden focus:ring-2 focus:ring-indigo-500"
                >
                  <option value="">
                    Default Structure (Executive/Standard)
                  </option>
                  {salaryStructures?.map((s: any) => (
                    <option key={s.id} value={s.id}>
                      {s.name} ({s.code})
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label
                  htmlFor="employeespage-field-2"
                  className="block font-semibold text-slate-700 uppercase tracking-wider text-[10px] mb-1"
                >
                  Monthly Base Salary ($ USD)
                </label>
                <input
                  id="employeespage-field-2"
                  type="number"
                  value={newCompData.baseSalary}
                  onChange={(e) =>
                    setNewCompData({
                      ...newCompData,
                      baseSalary: parseFloat(e.target.value) || 0,
                    })
                  }
                  className="w-full px-3 py-2 border border-slate-300 rounded-xl font-mono focus:outline-hidden focus:ring-2 focus:ring-indigo-500"
                />
              </div>

              <div>
                <label
                  htmlFor="employeespage-field-3"
                  className="block font-semibold text-slate-700 uppercase tracking-wider text-[10px] mb-1"
                >
                  Effective Start Date
                </label>
                <input
                  id="employeespage-field-3"
                  type="date"
                  value={newCompData.effectiveFrom}
                  onChange={(e) =>
                    setNewCompData({
                      ...newCompData,
                      effectiveFrom: e.target.value,
                    })
                  }
                  className="w-full px-3 py-2 border border-slate-300 rounded-xl focus:outline-hidden focus:ring-2 focus:ring-indigo-500"
                />
              </div>

              <div>
                <label
                  htmlFor="employeespage-field-4"
                  className="block font-semibold text-slate-700 uppercase tracking-wider text-[10px] mb-1"
                >
                  Audit Justification Note
                </label>
                <textarea
                  id="employeespage-field-4"
                  rows={2}
                  value={newCompData.remarks}
                  onChange={(e) =>
                    setNewCompData({ ...newCompData, remarks: e.target.value })
                  }
                  className="w-full px-3 py-2 border border-slate-300 rounded-xl focus:outline-hidden focus:ring-2 focus:ring-indigo-500"
                />
              </div>
            </div>

            <div className="mt-6 flex justify-end space-x-2.5">
              <button
                onClick={() => setIsCompModalOpen(false)}
                className="px-4 py-2 bg-slate-100 text-slate-700 rounded-xl text-xs font-semibold hover:bg-slate-200 transition-colors"
              >
                Cancel
              </button>
              <button
                onClick={() => addCompMutation.mutate()}
                disabled={
                  addCompMutation.isPending ||
                  !newCompData.salaryStructureId ||
                  newCompData.baseSalary <= 0 ||
                  !newCompData.effectiveFrom
                }
                className="px-4 py-2 bg-indigo-600 text-white rounded-xl text-xs font-semibold hover:bg-indigo-700 transition-colors cursor-pointer"
              >
                {addCompMutation.isPending
                  ? "Saving Contract..."
                  : "Create Contract Revision"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
