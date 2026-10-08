import type { ReactNode } from "react";
import { ArrowRight, Inbox, RefreshCw } from "lucide-react";
import { Link } from "react-router-dom";

export function PageHeader({
  eyebrow,
  title,
  description,
  actions,
}: {
  eyebrow: string;
  title: string;
  description: string;
  actions?: ReactNode;
}) {
  return (
    <header className="page-header">
      <div>
        <p className="eyebrow">{eyebrow}</p>
        <h1>{title}</h1>
        <p className="page-description">{description}</p>
      </div>
      {actions && <div className="page-actions">{actions}</div>}
    </header>
  );
}
export function Metric({
  label,
  value,
  detail,
  icon,
}: {
  label: string;
  value: ReactNode;
  detail: string;
  icon?: ReactNode;
}) {
  return (
    <div className="metric-card">
      <div className="metric-label">
        {label}
        <span className="metric-icon">{icon}</span>
      </div>
      <div className="metric-value">{value}</div>
      <p className="metric-detail">{detail}</p>
    </div>
  );
}
export function EmptyState({
  title,
  description,
}: {
  title: string;
  description: string;
}) {
  return (
    <div className="empty-state">
      <span className="empty-icon">
        <Inbox size={24} />
      </span>
      <h3>{title}</h3>
      <p>{description}</p>
    </div>
  );
}
export function QueryState({
  loading,
  error,
  retry,
}: {
  loading: boolean;
  error: unknown;
  retry?: () => void;
}) {
  if (loading)
    return (
      <div className="loading-state" role="status">
        <RefreshCw size={18} className="animate-spin" /> Loading workspace data…
      </div>
    );
  if (error)
    return (
      <div className="notice notice-error" role="alert">
        <span>
          {error instanceof Error
            ? error.message
            : "This data could not be loaded."}
        </span>
        {retry && (
          <button className="btn btn-secondary" onClick={retry}>
            Try again
          </button>
        )}
      </div>
    );
  return null;
}
export function Panel({
  title,
  subtitle,
  children,
  link,
  label = "View all",
}: {
  title: string;
  subtitle?: string;
  children: ReactNode;
  link?: string;
  label?: string;
}) {
  return (
    <section className="panel">
      <div className="panel-heading">
        <div>
          <h2>{title}</h2>
          {subtitle && <p>{subtitle}</p>}
        </div>
        {link && (
          <Link className="text-link" to={link}>
            {label}
            <ArrowRight size={14} />
          </Link>
        )}
      </div>
      {children}
    </section>
  );
}
