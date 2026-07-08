import type { TaskStatus } from "../types";

const STATUSES: TaskStatus[] = ["TODO", "IN_PROGRESS", "BLOCKED", "DONE", "CANCELLED"];

interface StatusFilterTabsProps {
  value: TaskStatus | undefined;
  onChange: (status: TaskStatus | undefined) => void;
}

export function StatusFilterTabs({ value, onChange }: StatusFilterTabsProps) {
  return (
    <div role="tablist" aria-label="Filter by status" style={{ display: "flex", gap: 8 }}>
      <button role="tab" type="button" aria-selected={value === undefined} onClick={() => onChange(undefined)}>
        All
      </button>
      {STATUSES.map((status) => (
        <button
          key={status}
          role="tab"
          type="button"
          aria-selected={value === status}
          onClick={() => onChange(status)}
        >
          {status}
        </button>
      ))}
    </div>
  );
}
