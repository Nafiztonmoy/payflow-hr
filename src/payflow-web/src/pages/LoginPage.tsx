import { useState } from "react";
import type { FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../context/auth";
import { DEMO_PERSONAS } from "../context/demoPersonas";
import {
  ArrowRight,
  Eye,
  EyeOff,
  Sprout,
  Users,
  CalendarDays,
  Receipt,
} from "lucide-react";
export function LoginPage() {
  const { login, switchPersona } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [show, setShow] = useState(false);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const signIn = async (action: () => Promise<void>) => {
    setError("");
    setBusy(true);
    try {
      await action();
      navigate("/dashboard");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not sign in.");
    } finally {
      setBusy(false);
    }
  };
  const submit = (e: FormEvent) => {
    e.preventDefault();
    void signIn(() => login(email, password));
  };
  return (
    <div className="login-screen">
      <section className="login-story">
        <div className="brand">
          <span className="brand-mark">
            <Sprout size={24} />
          </span>
          PayFlow <small>HR</small>
        </div>
        <div>
          <p className="eyebrow text-emerald-200">
            A better everyday workspace
          </p>
          <h1>
            Good people.
            <br />
            Great work.
            <br />
            <span className="text-emerald-200">One clear view.</span>
          </h1>
          <p>
            Bring your workforce, time off and payroll together. More clarity
            for every person. More confidence in every cycle.
          </p>
          <div className="login-preview">
            <div>
              <Users size={21} />
              Your people, connected.
            </div>
            <div>
              <CalendarDays size={21} />
              Time off, kept simple.
            </div>
            <div>
              <Receipt size={21} />
              Payroll, with a clear trail.
            </div>
          </div>
        </div>
        <footer className="text-xs text-slate-400">
          Northstar Technologies · Enterprise workspace
        </footer>
      </section>
      <section className="login-form-area">
        <div className="login-form">
          <p className="eyebrow">Welcome to PayFlow HR</p>
          <h2>Make yourself at work.</h2>
          <p>Sign in to your workspace to pick up where you left off.</p>
          {error && (
            <div className="notice notice-error" role="alert">
              {error}
            </div>
          )}
          <form onSubmit={submit}>
            <div className="field">
              <label htmlFor="email">Work email or username</label>
              <input
                id="email"
                autoComplete="username"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                placeholder="you@company.com"
              />
            </div>
            <div className="field">
              <label htmlFor="password">Password</label>
              <div className="relative">
                <input
                  id="password"
                  autoComplete="current-password"
                  className="pr-12"
                  type={show ? "text" : "password"}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                />
                <button
                  className="absolute right-2 top-1 icon-button border-0"
                  type="button"
                  onClick={() => setShow(!show)}
                  aria-label={show ? "Hide password" : "Show password"}
                >
                  {show ? <EyeOff size={17} /> : <Eye size={17} />}
                </button>
              </div>
            </div>
            <button
              type="submit"
              disabled={busy}
              className="btn btn-primary w-full"
            >
              {busy ? "Signing in…" : "Sign in to workspace"}
              <ArrowRight size={17} />
            </button>
          </form>
          <div className="border-t border-slate-200 mt-7 pt-6">
            <p className="text-xs font-semibold">Explore the demo workspace</p>
            <p className="text-xs text-slate-500 mt-2 leading-relaxed">
              Use a seeded demo account to explore each role.
            </p>
            <div className="persona-grid">
              {Object.entries(DEMO_PERSONAS).map(([key, p]) => (
                <button
                  disabled={busy}
                  key={key}
                  onClick={() =>
                    void signIn(() =>
                      switchPersona(key as keyof typeof DEMO_PERSONAS),
                    )
                  }
                >
                  <strong>{p.role}</strong>
                  <small>{p.name}</small>
                </button>
              ))}
            </div>
          </div>
          <p className="text-xs text-slate-500 mt-7">
            Access and actions depend on your assigned role.
          </p>
        </div>
      </section>
    </div>
  );
}
