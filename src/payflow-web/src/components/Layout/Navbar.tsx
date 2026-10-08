import { useIsMutating } from "@tanstack/react-query";
import { useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../../context/auth";
import { DEMO_PERSONAS } from "../../context/demoPersonas";
import { LogOut, Menu, ChevronRight } from "lucide-react";
import { navigation } from "./navigation";
export function Navbar({
  onToggleMobileMenu,
  isMobileMenuOpen,
}: {
  onToggleMobileMenu?: () => void;
  isMobileMenuOpen?: boolean;
}) {
  const mutating = useIsMutating() > 0;
  const { user, logout, switchPersona } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const current = navigation
    .flatMap((s) => s.items.map((i) => ({ ...i, section: s.title })))
    .find((i) => i.to === location.pathname);
  const initials = user?.userName
    .split(/\s+/)
    .map((x) => x[0])
    .slice(0, 2)
    .join("")
    .toUpperCase();
  return (
    <>
      <header className="topbar no-print">
        <div className="flex items-center gap-3">
          <button
            className="icon-button mobile-toggle"
            onClick={onToggleMobileMenu}
            aria-label="Open navigation"
            aria-expanded={isMobileMenuOpen}
          >
            <Menu size={19} />
          </button>
          <div className="breadcrumb">
            <span>{current?.section ?? "Payroll & time"}</span>
            <ChevronRight size={13} />
            <strong>{current?.label ?? "Payslip"}</strong>
          </div>
        </div>
        <div className="topbar-right">
          <label className="sr-only" htmlFor="demo-persona">
            Demo persona
          </label>
          <select
            id="demo-persona"
            className="persona-select rounded-lg border border-slate-200 bg-slate-50 px-2 py-2 text-xs"
            aria-label="Demo persona"
            value={user?.role.toLowerCase() ?? ""}
            disabled={busy || mutating}
            onChange={async (e) => {
              const key = e.target.value as keyof typeof DEMO_PERSONAS;
              setBusy(true);
              setError("");
              try {
                await switchPersona(key);
                navigate("/dashboard");
              } catch (err) {
                setError(
                  err instanceof Error
                    ? err.message
                    : "Could not switch persona.",
                );
              } finally {
                setBusy(false);
              }
            }}
          >
            <option value="" disabled>
              Demo persona
            </option>
            {Object.entries(DEMO_PERSONAS).map(([key, p]) => (
              <option key={key} value={key}>
                Demo: {p.role}
              </option>
            ))}
          </select>
          <div className="user-block">
            <div className="avatar">{initials}</div>
            <div>
              <strong>{user?.userName}</strong>
              <small>{user?.role}</small>
            </div>
          </div>
          <button
            className="icon-button"
            disabled={busy || mutating}
            onClick={() => void logout()}
            aria-label="Sign out"
          >
            <LogOut size={16} />
          </button>
        </div>
      </header>
      {error && (
        <div className="notice notice-error mx-4 mt-3" role="alert">
          {error}
          <button onClick={() => setError("")} aria-label="Dismiss error">
            ×
          </button>
        </div>
      )}
    </>
  );
}
