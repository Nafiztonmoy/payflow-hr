import { useEffect } from "react";
export function useDialogAccessibility() {
  useEffect(() => {
    const active = new Map<HTMLElement, HTMLElement | null>();
    let counter = 0;
    const selector = ".workspace .fixed.inset-0 > div";
    const scan = () => {
      for (const [dialog, previous] of active)
        if (!dialog.isConnected) {
          active.delete(dialog);
          if (previous?.isConnected) previous.focus();
        }
      document.querySelectorAll<HTMLElement>(selector).forEach((dialog) => {
        if (active.has(dialog)) return;
        active.set(dialog, document.activeElement as HTMLElement);
        dialog.setAttribute("role", "dialog");
        dialog.setAttribute("aria-modal", "true");
        const heading = dialog.querySelector("h3");
        if (heading) {
          heading.id ||= `dialog-title-${++counter}`;
          dialog.setAttribute("aria-labelledby", heading.id);
        }
        dialog.tabIndex = -1;
        (
          dialog.querySelector<HTMLElement>("button,input,select,textarea") ??
          dialog
        ).focus();
      });
      document.body.style.overflow = active.size ? "hidden" : "";
    };
    const observer = new MutationObserver(scan);
    observer.observe(document.body, { childList: true, subtree: true });
    scan();
    const key = (e: KeyboardEvent) => {
      const dialog = [...active.keys()].at(-1);
      if (!dialog) return;
      if (e.key === "Escape") {
        const close = dialog.querySelector<HTMLButtonElement>(
          'button[aria-label="Close dialog"]',
        );
        if (close) {
          e.preventDefault();
          close.click();
        }
        return;
      }
      if (e.key !== "Tab") return;
      const nodes = [
        ...dialog.querySelectorAll<HTMLElement>(
          'button:not(:disabled),a[href],input:not(:disabled),select:not(:disabled),textarea:not(:disabled),[tabindex="0"]',
        ),
      ].filter((n) => n.getClientRects().length > 0);
      if (!nodes.length) {
        e.preventDefault();
        dialog.focus();
        return;
      }
      const first = nodes[0],
        last = nodes[nodes.length - 1];
      if (
        e.shiftKey &&
        (document.activeElement === first || document.activeElement === dialog)
      ) {
        e.preventDefault();
        last.focus();
      } else if (
        !e.shiftKey &&
        (document.activeElement === last ||
          !dialog.contains(document.activeElement))
      ) {
        e.preventDefault();
        first.focus();
      }
    };
    document.addEventListener("keydown", key);
    return () => {
      observer.disconnect();
      document.removeEventListener("keydown", key);
      document.body.style.overflow = "";
    };
  }, []);
}
