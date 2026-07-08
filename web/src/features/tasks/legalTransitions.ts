import type { TaskStatus } from "./types";

// Mirrors backend Domain.StateMachine (spec §3) — kept identical by hand, verified
// against the same table in legalTransitions.test.ts.
export const legalTransitions: Record<TaskStatus, TaskStatus[]> = {
  TODO: ["IN_PROGRESS", "CANCELLED"],
  IN_PROGRESS: ["BLOCKED", "DONE", "CANCELLED"],
  BLOCKED: ["IN_PROGRESS", "CANCELLED"],
  DONE: [],
  CANCELLED: [],
};

export function canTransition(from: TaskStatus, to: TaskStatus): boolean {
  return legalTransitions[from].includes(to);
}
