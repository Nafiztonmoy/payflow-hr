import {
  LayoutDashboard,
  Users,
  Briefcase,
  Coins,
  Calculator,
  CalendarCheck,
  CalendarDays,
  Receipt,
  BarChart3,
  ShieldCheck,
} from "lucide-react";
export const navigation = [
  {
    title: "Overview",
    items: [
      {
        to: "/dashboard",
        label: "Dashboard",
        icon: LayoutDashboard,
        roles: ["Admin", "HR", "Manager", "Accountant", "Employee"],
      },
    ],
  },
  {
    title: "Workforce",
    items: [
      {
        to: "/employees",
        label: "Employees",
        icon: Users,
        roles: ["Admin", "HR", "Manager"],
      },
      {
        to: "/organization",
        label: "Organization",
        icon: Briefcase,
        roles: ["Admin", "HR"],
      },
      {
        to: "/compensation-plans",
        label: "Salary structures",
        icon: Coins,
        roles: ["Admin", "HR", "Accountant"],
      },
    ],
  },
  {
    title: "Payroll & time",
    items: [
      {
        to: "/payroll",
        label: "Payroll workspace",
        icon: Calculator,
        roles: ["Admin", "Accountant"],
      },
      {
        to: "/attendance",
        label: "Attendance",
        icon: CalendarCheck,
        roles: ["Admin", "HR", "Manager", "Employee"],
      },
      {
        to: "/leaves",
        label: "Time off",
        icon: CalendarDays,
        roles: ["Admin", "HR", "Manager", "Employee"],
      },
      {
        to: "/my-payslips",
        label: "My payslips",
        icon: Receipt,
        roles: ["Employee", "Manager", "Accountant"],
      },
    ],
  },
  {
    title: "Governance",
    items: [
      {
        to: "/reports",
        label: "Reports",
        icon: BarChart3,
        roles: ["Admin", "HR", "Manager", "Accountant"],
      },
      {
        to: "/audit-logs",
        label: "Audit trail",
        icon: ShieldCheck,
        roles: ["Admin"],
      },
    ],
  },
];
