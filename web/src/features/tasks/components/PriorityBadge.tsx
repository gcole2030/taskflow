import type { Priority } from "../types";

const COLORS: Record<Priority, string> = {
  LOW: "#4b5563",
  MEDIUM: "#a16207",
  HIGH: "#b91c1c",
};

export function PriorityBadge({ priority }: { priority: Priority }) {
  return (
    <span
      style={{
        color: COLORS[priority],
        border: `1px solid ${COLORS[priority]}`,
        borderRadius: 4,
        padding: "0 6px",
        fontSize: 12,
        fontWeight: 600,
      }}
    >
      {priority}
    </span>
  );
}
