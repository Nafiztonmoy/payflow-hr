import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { Outlet, Navigate, useLocation } from "react-router-dom";
import { useAuth } from "../../context/auth";
import { Navbar } from "./Navbar";
import { Sidebar } from "./Sidebar";
import { navigation } from "./navigation";
import { WorkspaceFeedback } from "./WorkspaceFeedback";
import { EmptyState } from "../ui/Workspace";
import { useDialogAccessibility } from "../ui/useDialogAccessibility";
function WorkspaceShell() {
  const { user, isLoading } = useAuth();
  const [openPath, setOpenPath] = useState<string | null>(null);
  const { pathname } = useLocation();
  const open = openPath === pathname;
  const setOpen = (value: boolean) => setOpenPath(value ? pathname : null);
  useEffect(() => {
    window.scrollTo(0, 0);
  }, [pathname]);
  useEffect(() => {
    if (!open) return;
    const close = (e: KeyboardEvent) => {
      if (e.key === "Escape") setOpenPath(null);
    };
    document.addEventListener("keydown", close);
    return () => document.removeEventListener("keydown", close);
  }, [open]);
  useDialogAccessibility();
  if (isLoading)
    return (
      <div className="loading-state min-h-screen" role="status">
        Loading your workspace…
      </div>
    );
  if (!user) return <Navigate to="/login" replace />;
  const route = navigation
    .flatMap((s) => s.items)
    .find((i) => i.to === pathname);
  const forbidden = route && !route.roles.includes(user.role);
  return (
    <div className="app-shell">
      <a className="skip-link" href="#workspace-main">
        Skip to content
      </a>
      <Sidebar isOpen={open} onClose={() => setOpen(false)} />
      <div className="shell-body">
        <Navbar
          onToggleMobileMenu={() => setOpen(!open)}
          isMobileMenuOpen={open}
        />
        <main id="workspace-main" className="workspace" tabIndex={-1}>
          <WorkspaceFeedback key={`${user.id}-${pathname}`} />
          {forbidden ? (
            <EmptyState
              title="This page belongs to another role"
              description="Choose an available page from your workspace navigation."
            />
          ) : (
            <Outlet key={user.id ?? user.email} />
          )}
        </main>
      </div>
    </div>
  );
}

// A new authenticated identity gets a new cache. Unmount cancels old requests.
function WorkspaceSession() {
  const [client] = useState(
    () =>
      new QueryClient({
        defaultOptions: { queries: { refetchOnWindowFocus: false, retry: 1 } },
      }),
  );
  useEffect(
    () => () => {
      void client.cancelQueries();
      client.clear();
    },
    [client],
  );
  return (
    <QueryClientProvider client={client}>
      <WorkspaceShell />
    </QueryClientProvider>
  );
}
export function MainLayout() {
  const { user, isLoading } = useAuth();
  if (isLoading)
    return (
      <div className="loading-state" role="status">
        Loading your workspace…
      </div>
    );
  if (!user) return <Navigate to="/login" replace />;
  return <WorkspaceSession key={`${user.id}-${user.role}`} />;
}
