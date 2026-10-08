import { apiFetch } from "./client";

export interface User {
  id: string;
  userName: string;
  email: string;
  role: "Admin" | "HR" | "Manager" | "Accountant" | "Employee";
  employeeId?: string;
  employeeName?: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  user: User;
}

export const authApi = {
  login: (emailOrUsername: string, password: string): Promise<AuthResponse> => {
    return apiFetch<AuthResponse>("/auth/login", {
      method: "POST",
      body: JSON.stringify({ emailOrUsername, password }),
    });
  },

  getMe: (): Promise<{
    id: string;
    userName: string;
    email: string;
    role: string;
    employeeId?: string;
    employee?: any;
  }> => {
    return apiFetch("/auth/me");
  },

  logout: (): Promise<{ message: string }> => {
    return apiFetch("/auth/logout", { method: "POST" });
  },
};
