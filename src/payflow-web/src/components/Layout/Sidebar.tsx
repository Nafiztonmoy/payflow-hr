import { NavLink } from "react-router-dom";
import { useAuth } from "../../context/auth";
import { navigation } from "./navigation";
import { X, Sprout } from "lucide-react";

export function Sidebar({
  isOpen,
  onClose,
}: {
  isOpen?: boolean;
  onClose?: () => void;
}) {
  const { user } = useAuth();
  if (!user) return null;
  return (
    <>
      {isOpen && (
        <button
          className="drawer-backdrop"
          onClick={onClose}
          aria-label="Close navigation overlay"
        />
      )}
      <aside
        className={`sidebar ${isOpen ? "open" : ""}`}
        aria-label="Main navigation"
      >
        <button
          className="icon-button sidebar-close"
          onClick={onClose}
          aria-label="Close navigation"
        >
          <X size={20} />
        </button>
        <NavLink to="/dashboard" className="brand" onClick={onClose}>
          <span className="brand-mark">
            <Sprout size={23} />
          </span>
          PayFlow <small>HR</small>
        </NavLink>
        <div className="sidebar-org">
          <strong>Northstar Technologies</strong>
          <span>Enterprise workspace</span>
        </div>
        <nav className="sidebar-nav">
          {navigation.map((section) => {
            const items = section.items.filter((i) =>
              i.roles.includes(user.role),
            );
            return (
              items.length > 0 && (
                <div className="nav-group" key={section.title}>
                  <p className="nav-label">{section.title}</p>
                  {items.map(({ to, label, icon: Icon }) => (
                    <NavLink
                      key={to}
                      to={to}
                      onClick={onClose}
                      className={({ isActive }) =>
                        `nav-item ${isActive ? "active" : ""}`
                      }
                    >
                      <Icon size={17} />
                      {label}
                    </NavLink>
                  ))}
                </div>
              )
            );
          })}
        </nav>
        <div className="sidebar-foot">
          <strong>People first. Every cycle.</strong>
          <span>{user.role} workspace · PayFlow HR</span>
        </div>
      </aside>
    </>
  );
}
