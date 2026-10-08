import { apiFetch } from "./client";

export interface EmployeeListItem {
  id: string;
  employeeNo: string;
  fullName: string;
  workEmail: string;
  departmentName: string;
  designationTitle: string;
  managerName?: string;
  status: string;
  joinDate: string;
}

export interface EmployeeDetail {
  id: string;
  employeeNo: string;
  firstName: string;
  lastName: string;
  fullName: string;
  workEmail: string;
  phone?: string;
  dateOfBirth: string;
  joinDate: string;
  terminationDate?: string;
  status: string;
  departmentId: string;
  departmentName: string;
  designationId: string;
  designationTitle: string;
  managerId?: string;
  managerName?: string;
  paymentMethod: string;
  bankName?: string;
  bankAccountNumber?: string;
  bankRoutingNumber?: string;
}

export interface CompensationRecord {
  id: string;
  salaryStructureId: string;
  salaryStructureName: string;
  baseSalary: number;
  effectiveFrom: string;
  effectiveTo?: string;
  isActive: boolean;
  remarks?: string;
  components?: Array<{
    code: string;
    displayName: string;
    type: string;
    mode: string;
    defaultAmount: number;
    percentageRate: number;
    isTaxable: boolean;
  }>;
}

export const employeesApi = {
  getEmployees: (params?: {
    search?: string;
    departmentId?: string;
    status?: string;
    page?: number;
    pageSize?: number;
  }): Promise<{
    total: number;
    page: number;
    pageSize: number;
    items: EmployeeListItem[];
  }> => {
    const query = new URLSearchParams();
    if (params?.search) query.append("search", params.search);
    if (params?.departmentId) query.append("departmentId", params.departmentId);
    if (params?.status) query.append("status", params.status);
    if (params?.page) query.append("page", params.page.toString());
    if (params?.pageSize) query.append("pageSize", params.pageSize.toString());
    return apiFetch(`/employees?${query.toString()}`);
  },

  getEmployeeById: (id: string): Promise<EmployeeDetail> =>
    apiFetch(`/employees/${id}`),

  updateEmployee: (
    id: string,
    data: Partial<EmployeeDetail>,
  ): Promise<{ message: string; id: string }> =>
    apiFetch(`/employees/${id}`, {
      method: "PATCH",
      body: JSON.stringify(data),
    }),

  getCompensation: (employeeId: string): Promise<CompensationRecord[]> =>
    apiFetch(`/employees/${employeeId}/compensation`),

  addCompensation: (
    employeeId: string,
    data: {
      salaryStructureId: string;
      baseSalary: number;
      effectiveFrom: string;
      remarks?: string;
    },
  ): Promise<{ message: string; id: string }> =>
    apiFetch(`/employees/${employeeId}/compensation`, {
      method: "POST",
      body: JSON.stringify(data),
    }),
};
