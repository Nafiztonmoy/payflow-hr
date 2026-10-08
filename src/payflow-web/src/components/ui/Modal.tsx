import type { ReactNode } from "react";
import { X } from "lucide-react";
export function Modal({
  title,
  description,
  close,
  children,
}: {
  title: string;
  description?: string;
  close: () => void;
  children: ReactNode;
}) {
  return (
    <div className="fixed inset-0 z-50 bg-slate-950/60 flex items-center justify-center p-4">
      <div className="bg-white rounded-xl border border-slate-200 max-w-lg w-full p-6">
        <div className="flex items-center justify-between gap-4 border-b border-slate-100 pb-4">
          <h3 className="text-lg font-semibold">{title}</h3>
          <button
            className="icon-button"
            onClick={close}
            aria-label="Close dialog"
          >
            <X size={18} />
          </button>
        </div>
        {description && (
          <p className="text-xs text-slate-500 leading-relaxed mt-4">
            {description}
          </p>
        )}
        {children}
      </div>
    </div>
  );
}
