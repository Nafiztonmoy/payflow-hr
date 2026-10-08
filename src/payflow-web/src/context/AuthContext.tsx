import React, { useState, useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { AuthContext } from "./auth";
import { DEMO_PERSONAS } from "./demoPersonas";
import { authApi, type User } from "../api/authApi";

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const queryClient = useQueryClient();
  const [user, setUser] = useState<User | null>(() => {
    if (!localStorage.getItem("payflow_token")) return null;
    try {
      return JSON.parse(localStorage.getItem("payflow_user") || "null");
    } catch {
      return null;
    }
  });
  const [token, setToken] = useState<string | null>(() =>
    localStorage.getItem("payflow_token"),
  );
  const isLoading = false;

  useEffect(() => {
    const expire = () => {
      setToken(null);
      setUser(null);
      queryClient.clear();
    };
    window.addEventListener("payflow-session-expired", expire);
    return () => window.removeEventListener("payflow-session-expired", expire);
  }, [queryClient]);

  const login = async (emailOrUsername: string, password: string) => {
    const response = await authApi.login(emailOrUsername, password);
    await queryClient.cancelQueries();
    queryClient.clear();
    setToken(response.accessToken);
    setUser(response.user);
    localStorage.setItem("payflow_token", response.accessToken);
    localStorage.setItem("payflow_user", JSON.stringify(response.user));
  };

  const logout = async () => {
    try {
      await authApi.logout();
    } catch {
      // Ignore
    } finally {
      await queryClient.cancelQueries();
      queryClient.clear();
      setToken(null);
      setUser(null);
      localStorage.removeItem("payflow_token");
      localStorage.removeItem("payflow_user");
    }
  };

  const switchPersona = async (
    personaKey: "admin" | "hr" | "manager" | "accountant" | "employee",
  ) => {
    const persona = DEMO_PERSONAS[personaKey];
    await login(persona.email, persona.password);
  };

  return (
    <AuthContext.Provider
      value={{ user, token, isLoading, login, logout, switchPersona }}
    >
      {children}
    </AuthContext.Provider>
  );
};
