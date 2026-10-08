import React, { lazy, Suspense } from "react";
import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AuthProvider } from "./context/AuthContext";
import { MainLayout } from "./components/Layout/MainLayout";
const LoginPage = lazy(() =>
  import("./pages/LoginPage").then((module) => ({ default: module.LoginPage })),
);
const DashboardPage = lazy(() =>
  import("./pages/DashboardPage").then((module) => ({
    default: module.DashboardPage,
  })),
);
const EmployeesPage = lazy(() =>
  import("./pages/EmployeesPage").then((module) => ({
    default: module.EmployeesPage,
  })),
);
const PayrollWorkspacePage = lazy(() =>
  import("./pages/PayrollWorkspacePage").then((module) => ({
    default: module.PayrollWorkspacePage,
  })),
);
const PayslipPage = lazy(() =>
  import("./pages/PayslipPage").then((module) => ({
    default: module.PayslipPage,
  })),
);
const MyPayslipsPage = lazy(() =>
  import("./pages/PayslipPage").then((module) => ({
    default: module.MyPayslipsPage,
  })),
);
const AttendancePage = lazy(() =>
  import("./pages/AttendanceAndLeavesPages").then((module) => ({
    default: module.AttendancePage,
  })),
);
const LeavesPage = lazy(() =>
  import("./pages/AttendanceAndLeavesPages").then((module) => ({
    default: module.LeavesPage,
  })),
);
const OrganizationPage = lazy(() =>
  import("./pages/OrgAndReportsPages").then((module) => ({
    default: module.OrganizationPage,
  })),
);
const ReportsPage = lazy(() =>
  import("./pages/OrgAndReportsPages").then((module) => ({
    default: module.ReportsPage,
  })),
);
const AuditLogsPage = lazy(() =>
  import("./pages/AuditLogsPage").then((module) => ({
    default: module.AuditLogsPage,
  })),
);

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: 1,
    },
  },
});

export const App: React.FC = () => {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <BrowserRouter>
          <Suspense
            fallback={
              <div className="loading-state" role="status">
                Loading page…
              </div>
            }
          >
            <Routes>
              <Route path="/login" element={<LoginPage />} />

              <Route element={<MainLayout />}>
                <Route
                  path="/"
                  element={<Navigate to="/dashboard" replace />}
                />
                <Route path="/dashboard" element={<DashboardPage />} />
                <Route path="/employees" element={<EmployeesPage />} />
                <Route path="/organization" element={<OrganizationPage />} />
                <Route
                  path="/compensation-plans"
                  element={<OrganizationPage />}
                />
                <Route path="/payroll" element={<PayrollWorkspacePage />} />
                <Route path="/attendance" element={<AttendancePage />} />
                <Route path="/leaves" element={<LeavesPage />} />
                <Route path="/my-payslips" element={<MyPayslipsPage />} />
                <Route path="/payslip/:id" element={<PayslipPage />} />
                <Route path="/reports" element={<ReportsPage />} />
                <Route path="/audit-logs" element={<AuditLogsPage />} />
              </Route>

              <Route path="*" element={<Navigate to="/dashboard" replace />} />
            </Routes>
          </Suspense>
        </BrowserRouter>
      </AuthProvider>
    </QueryClientProvider>
  );
};

export default App;
