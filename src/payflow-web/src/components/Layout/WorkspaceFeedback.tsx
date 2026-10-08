import { useCallback, useState, useSyncExternalStore } from "react";
import {
  notifyManager,
  useQueryClient,
  useMutationState,
} from "@tanstack/react-query";
export function WorkspaceFeedback() {
  const client = useQueryClient();
  const cache = client.getQueryCache();
  const [dismissed, setDismissed] = useState(() => Date.now());
  useSyncExternalStore(
    useCallback(
      (notify) => cache.subscribe(notifyManager.batchCalls(notify)),
      [cache],
    ),
    () =>
      cache
        .getAll()
        .map(
          (q) =>
            `${q.queryHash}:${q.state.status}:${q.state.errorUpdatedAt}:${q.getObserversCount()}`,
        )
        .join("|"),
    () => "",
  );
  const failures = cache
    .getAll()
    .filter((q) => q.getObserversCount() > 0 && q.state.status === "error");
  const mutations = useMutationState({
    filters: { status: "error" },
    select: (m) => ({ error: m.state.error, at: m.state.submittedAt }),
  }).filter((m) => m.at > dismissed);
  if (!failures.length && !mutations.length) return null;
  const err = mutations.at(-1)?.error ?? failures[0]?.state.error;
  return (
    <div role="alert" className="notice notice-error no-print">
      <div>
        <strong>
          {mutations.length
            ? "Action could not be completed. "
            : "Some data could not be loaded. "}
        </strong>
        {err instanceof Error ? err.message : "Please try again."}
      </div>
      <div className="flex gap-2">
        {failures.length > 0 && (
          <button
            className="btn btn-secondary"
            onClick={() =>
              failures.forEach(
                (q) =>
                  void client.refetchQueries({
                    queryKey: q.queryKey,
                    exact: true,
                  }),
              )
            }
          >
            Retry
          </button>
        )}
        {mutations.length > 0 && (
          <button
            className="btn btn-secondary"
            onClick={() => setDismissed(Date.now())}
          >
            Dismiss
          </button>
        )}
      </div>
    </div>
  );
}
