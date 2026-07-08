import { describe, expect, it } from "vitest";
import { canTransition, legalTransitions } from "./legalTransitions";
import type { TaskStatus } from "./types";

const ALL_STATUSES: TaskStatus[] = ["TODO", "IN_PROGRESS", "BLOCKED", "DONE", "CANCELLED"];

// Mirrors spec §3 exactly — same table backend StateMachineTests exercises.
const LEGAL_PAIRS = new Set<string>([
  "TODO->IN_PROGRESS",
  "TODO->CANCELLED",
  "IN_PROGRESS->BLOCKED",
  "IN_PROGRESS->DONE",
  "IN_PROGRESS->CANCELLED",
  "BLOCKED->IN_PROGRESS",
  "BLOCKED->CANCELLED",
]);

describe("legalTransitions", () => {
  const allPairs = ALL_STATUSES.flatMap((from) => ALL_STATUSES.map((to) => [from, to] as const));

  it.each(allPairs)("canTransition(%s, %s) matches the spec table", (from, to) => {
    const expected = LEGAL_PAIRS.has(`${from}->${to}`);
    expect(canTransition(from, to)).toBe(expected);
  });

  it.each(["DONE", "CANCELLED"] as const)("%s has zero legal targets", (status) => {
    expect(legalTransitions[status]).toHaveLength(0);
  });

  it.each([
    ["TODO", 2],
    ["IN_PROGRESS", 3],
    ["BLOCKED", 2],
  ] as const)("%s has %i legal targets", (status, count) => {
    expect(legalTransitions[status]).toHaveLength(count);
  });
});
