# ADR-0003: State machine enforced in domain code, not the database
Date: 2026-07-07 · Status: Accepted

## Context
Spec §3 defines a fixed set of legal task-status transitions; AC4-AC6 require illegal
transitions to fail with 409 and zero side effects, and the same rule has to be mirrored
by the frontend (spec §6) so illegal buttons can be disabled client-side. There are two
places a transition-legality check could live: a Postgres trigger/constraint on the
`tasks` table, or a single pure function the API (and, by hand-kept mirror, the frontend)
calls before writing anything.

## Decision
`Domain.StateMachine.CanTransition(from, to)` is the single source of truth for
transition legality, called by `TasksRepository.TransitionAsync` before any write. The
Postgres `CHECK` constraint on `tasks.status` only validates that the column holds one
of the five known enum values (`db/migrations/001_initial_schema.sql`) — it does not,
and structurally cannot without a trigger, know whether a given *transition* between two
values is legal. Web's `legalTransitions.ts` is a hand-kept, test-verified mirror of the
same table (see `slice/web`'s AI-DEVELOPMENT.md entry) since there's no shared-codegen
mechanism between the .NET backend and the TypeScript frontend in this project's scope.

## Consequences
+ One reviewable function, one exhaustive table-driven test suite (`StateMachineTests.cs`,
  30 cases: every legal pair true, every illegal pair false, terminal states empty).
+ No trigger/procedure logic to review, migrate, or debug inside Postgres.
+ AC5's "zero new events" on an illegal transition falls out naturally: the check happens
  before the transaction writes anything, so there's nothing to roll back.
− The DB alone can't stop an illegal transition if some future code path writes to
  `tasks.status` directly instead of going through `TasksRepository.TransitionAsync` — the
  guardrail today is "only one code path exists," not a database-level backstop.
− The frontend copy is a manually-kept mirror, not derived from the backend at build time;
  it can only drift silently if someone edits one side without checking the other's test.
Revisit with a Postgres trigger (or a shared schema/codegen step for the frontend mirror)
if a second write path to `tasks.status` is ever added, or if the frontend/backend split
grows past this one shared table.
