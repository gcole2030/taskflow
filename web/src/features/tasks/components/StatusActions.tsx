"use client";

import { useState } from "react";
import { legalTransitions } from "../legalTransitions";
import { useTransitionTask } from "../useTasks";
import type { TaskStatus } from "../types";

const ALL_STATUSES: TaskStatus[] = ["TODO", "IN_PROGRESS", "BLOCKED", "DONE", "CANCELLED"];

export function StatusActions({ taskId, status }: { taskId: string; status: TaskStatus }) {
  const transition = useTransitionTask(taskId);
  const [reason, setReason] = useState("");
  const legalTargets = legalTransitions[status];

  return (
    <div>
      {legalTargets.includes("BLOCKED") && (
        <input
          aria-label="Reason (for BLOCKED)"
          placeholder="Reason (optional, for BLOCKED)"
          value={reason}
          onChange={(e) => setReason(e.target.value)}
        />
      )}
      <div style={{ display: "flex", gap: 8 }}>
        {ALL_STATUSES.filter((target) => target !== status).map((target) => {
          const legal = legalTargets.includes(target);
          return (
            <button
              key={target}
              type="button"
              disabled={!legal || transition.isPending}
              onClick={() =>
                transition.mutate({
                  to: target,
                  metadata: target === "BLOCKED" && reason ? { reason } : undefined,
                })
              }
            >
              {target}
            </button>
          );
        })}
      </div>
      {transition.isError && <p role="alert">{transition.error.message}</p>}
    </div>
  );
}
