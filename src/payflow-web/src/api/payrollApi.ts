import { apiFetch } from "./client";

export type PayrollRunStatus =
  | "Draft"
  | "Calculating"
  | "Calculated"
  | "InReview"
  | "Approved"
  | "Paid"
  | "Failed";
export type ExceptionSeverity = "Warning" | "Blocking";
export type PayrollItemStatus = "Ok" | "HasWarnings" | "HasBlockingExceptions";

export interface PayrollRunSummary {
  id: string;
  year: number;
  month: number;
  runNumber: string;
  status: PayrollRunStatus;
  totalGross: number;
  totalTax: number;
  totalDeductions: number;
  totalNet: number;
  totalEmployees: number;
  warningCount: number;
  blockingCount: number;
  calculatedAtUtc?: string;
  reviewedAtUtc?: string;
  approvedAtUtc?: string;
  paidAtUtc?: string;
  paymentReference?: string;
  concurrencyToken: string;
}

export interface PayrollItemComponent {
  componentCode: string;
  componentName: string;
  componentType: "Earning" | "Deduction";
  amount: number;
  sourceRule: string;
  explanation: string;
}

export interface PayrollException {
  id: string;
  payrollItemId?: string;
  employeeId: string;
  employeeName: string;
  severity: ExceptionSeverity;
  exceptionType: string;
  message: string;
  isResolved: boolean;
  resolvedAtUtc?: string;
  resolutionNotes?: string;
}

export interface PayrollItemDetail {
  id: string;
  employeeId: string;
  employeeNo: string;
  employeeName: string;
  departmentName: string;
  designationTitle: string;
  baseSalary: number;
  grossPay: number;
  taxableAmount: number;
  incomeTax: number;
  totalDeductions: number;
  netPay: number;
  status: PayrollItemStatus;
  calculationHash: string;
  components: PayrollItemComponent[];
  exceptions: PayrollException[];
}

export interface PayslipDetail {
  payslipId: string;
  payrollItemId: string;
  payslipNumber: string;
  organizationName: string;
  currency: string;
  periodYear: number;
  periodMonth: number;
  employeeNo: string;
  employeeName: string;
  department: string;
  designation: string;
  paymentMethod: string;
  maskedBankAccount: string;
  baseSalary: number;
  grossPay: number;
  taxableAmount: number;
  incomeTax: number;
  totalDeductions: number;
  netPay: number;
  earnings: PayrollItemComponent[];
  deductions: PayrollItemComponent[];
  issueDateUtc: string;
  calculationHash: string;
}

export interface MyPayslipSummary {
  id: string;
  payrollItemId: string;
  payslipNumber: string;
  issueDateUtc: string;
  year: number;
  month: number;
  grossPay: number;
  totalDeductions: number;
  netPay: number;
}

export const payrollApi = {
  getRuns: (): Promise<PayrollRunSummary[]> => apiFetch("/payroll-runs"),
  getRunById: (id: string): Promise<PayrollRunSummary> =>
    apiFetch(`/payroll-runs/${id}`),
  createRun: (year: number, month: number): Promise<PayrollRunSummary> =>
    apiFetch("/payroll-runs", {
      method: "POST",
      body: JSON.stringify({ year, month }),
    }),
  calculateRun: (id: string): Promise<PayrollRunSummary> =>
    apiFetch(`/payroll-runs/${id}/calculate`, { method: "POST" }),
  submitReview: (
    id: string,
    concurrencyToken: string,
  ): Promise<PayrollRunSummary> =>
    apiFetch(`/payroll-runs/${id}/submit-review`, {
      method: "POST",
      body: JSON.stringify({ concurrencyToken }),
    }),
  approveRun: (
    id: string,
    concurrencyToken: string,
  ): Promise<PayrollRunSummary> =>
    apiFetch(`/payroll-runs/${id}/approve`, {
      method: "POST",
      body: JSON.stringify({ concurrencyToken }),
    }),
  markPaid: (
    id: string,
    paymentReference: string,
    paidAtUtc: string,
    concurrencyToken: string,
  ): Promise<PayrollRunSummary> =>
    apiFetch(`/payroll-runs/${id}/mark-paid`, {
      method: "POST",
      body: JSON.stringify({ paymentReference, paidAtUtc, concurrencyToken }),
    }),
  getRunItems: (id: string): Promise<PayrollItemDetail[]> =>
    apiFetch(`/payroll-runs/${id}/items`),
  getRunExceptions: (id: string): Promise<PayrollException[]> =>
    apiFetch(`/payroll-runs/${id}/exceptions`),
  resolveException: (
    id: string,
    resolutionNotes: string,
  ): Promise<PayrollException> =>
    apiFetch(`/payroll-runs/exceptions/${id}/resolve`, {
      method: "POST",
      body: JSON.stringify({ resolutionNotes }),
    }),
  exportCsvUrl: (id: string): string => `/api/v1/payroll-runs/${id}/export-csv`,
  getPayslip: (payrollItemId: string): Promise<PayslipDetail> =>
    apiFetch(`/payroll-items/${payrollItemId}/payslip`),
  getMyPayslips: (): Promise<MyPayslipSummary[]> =>
    apiFetch("/payroll-items/my-payslips"),
};
