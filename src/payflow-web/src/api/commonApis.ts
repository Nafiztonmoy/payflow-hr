import { apiFetch } from "./client";

export interface AttendanceRecordDto {
  id: string;
  employeeId: string;
  employeeNo: string;
  employeeName: string;
  date: string;
  status: string;
  checkInTimeUtc?: string;
  checkOutTimeUtc?: string;
  workedMinutes: number;
  overtimeMinutes: number;
  source: string;
  correctionStatus: string;
  correctionReason?: string;
}

export interface LeaveRequestDto {
  id: string;
  employeeId: string;
  employeeNo: string;
  employeeName: string;
  departmentName: string;
  leaveTypeId: string;
  leaveTypeName: string;
  startDate: string;
  endDate: string;
  dayCount: number;
  reason: string;
  status: string;
  reviewRemarks?: string;
  reviewedAtUtc?: string;
}

export interface LeaveBalanceDto {
  id: string;
  leaveTypeId: string;
  leaveTypeName: string;
  leaveTypeCode: string;
  isPaid: boolean;
  year: number;
  openingBalance: number;
  accruedDays: number;
  usedDays: number;
  adjustedDays: number;
  remainingDays: number;
}

export const attendanceApi = {
  getAttendance: (params?: {
    employeeId?: string;
    year?: number;
    month?: number;
  }): Promise<AttendanceRecordDto[]> => {
    const query = new URLSearchParams();
    if (params?.employeeId) query.append("employeeId", params.employeeId);
    if (params?.year) query.append("year", params.year.toString());
    if (params?.month) query.append("month", params.month.toString());
    return apiFetch(`/attendance?${query.toString()}`);
  },

  correctAttendance: (
    id: string,
    data: {
      reason: string;
      workedMinutes: number;
      overtimeMinutes: number;
      status: string;
    },
  ): Promise<{ message: string; id: string }> =>
    apiFetch(`/attendance/${id}/correct`, {
      method: "POST",
      body: JSON.stringify(data),
    }),
};

export const leaveApi = {
  getRequests: (status?: string): Promise<LeaveRequestDto[]> => {
    const query = status ? `?status=${status}` : "";
    return apiFetch(`/leave-requests${query}`);
  },

  submitRequest: (data: {
    leaveTypeId: string;
    startDate: string;
    endDate: string;
    reason: string;
  }): Promise<any> =>
    apiFetch("/leave-requests", {
      method: "POST",
      body: JSON.stringify(data),
    }),

  approveRequest: (
    id: string,
    remarks?: string,
  ): Promise<{ message: string; id: string }> =>
    apiFetch(`/leave-requests/${id}/approve`, {
      method: "POST",
      body: JSON.stringify({ remarks }),
    }),

  rejectRequest: (
    id: string,
    remarks: string,
  ): Promise<{ message: string; id: string }> =>
    apiFetch(`/leave-requests/${id}/reject`, {
      method: "POST",
      body: JSON.stringify({ remarks }),
    }),

  getMyBalances: (): Promise<LeaveBalanceDto[]> =>
    apiFetch("/leave-balances/me"),
};

export const reportsApi = {
  getPayrollSummary: (): Promise<any[]> => apiFetch("/reports/payroll-summary"),
  getLaborCost: (
    year?: number,
  ): Promise<{ year: number; departments: any[] }> =>
    apiFetch(`/reports/labor-cost${year ? `?year=${year}` : ""}`),
  getLeave: (): Promise<{ year: number; utilization: any[] }> =>
    apiFetch("/reports/leave"),
  getAttendance: (
    year?: number,
    month?: number,
  ): Promise<{ year: number; month: number; departmentStats: any[] }> => {
    const query = new URLSearchParams();
    if (year) query.append("year", year.toString());
    if (month) query.append("month", month.toString());
    return apiFetch(`/reports/attendance?${query.toString()}`);
  },
};

export const auditApi = {
  getLogs: (params?: {
    action?: string;
    entityType?: string;
    page?: number;
    pageSize?: number;
  }): Promise<{
    total: number;
    page: number;
    pageSize: number;
    items: any[];
  }> => {
    const query = new URLSearchParams();
    if (params?.action) query.append("action", params.action);
    if (params?.entityType) query.append("entityType", params.entityType);
    if (params?.page) query.append("page", params.page.toString());
    if (params?.pageSize) query.append("pageSize", params.pageSize.toString());
    return apiFetch(`/audit-logs?${query.toString()}`);
  },
};

export const orgApi = {
  getDepartments: (): Promise<any[]> => apiFetch("/departments"),
  getDesignations: (): Promise<any[]> => apiFetch("/designations"),
  getSalaryStructures: (): Promise<any[]> => apiFetch("/salary-structures"),
};
