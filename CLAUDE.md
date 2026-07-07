# CLAUDE.md — Task Management System

## What this project is
A take-home exercise. The evaluated artifact is the AI-native process, not the app.
Every piece of work must be traceable: spec → acceptance criterion → test → code → PR.

## Source of truth
- `docs/spec-taskmanager.md` is the contract. If a request conflicts with it, stop and ask.
- Never implement anything not covered by an acceptance criterion or explicitly in scope §6.
- Scope cuts live in spec §9. Do not "helpfully" add them.

## Stack & conventions
- .NET 10, C#, minimal APIs, vertical slice architecture (see skill: vertical-slice)
- Data access: Dapper + Npgsql. No EF Core. SQL lives next to the slice.
- Migrations: DbUp, plain SQL files in `db/migrations/`, numbered `NNN_description.sql`,
  embedded resources, run on API startup. Never edit an applied migration; add a new one.
- Tests: xUnit + Testcontainers (postgres:16). Integration tests hit the real HTTP pipeline
  via WebApplicationFactory. One test class per acceptance criterion group. Test names:
  `AC4_Todo_To_InProgress_Returns200_And_AppendsEvent`.
- Frontend: Next.js App Router, TypeScript, TanStack Query. API base URL from env.
- Logging: Serilog, structured. No Console.WriteLine.
- IDs: UUIDv7. Timestamps: timestamptz, UTC, server-set.

## Workflow rules
- Trunk-based: short-lived branch per slice → PR → squash merge. Branch names `slice/<name>`.
- Write the failing AC test(s) FIRST, then implement until green. Show me the red run.
- Run `dotnet test` before declaring any slice done. Never claim green without running it.
- Conventional commits: `feat(tasks): ...`, `test(tasks): ...`, `chore(ci): ...`.
- After each slice, append a dated entry to `docs/AI-DEVELOPMENT.md`: what was asked,
  what the agent did, what was corrected, elapsed time.

## Commands you may run freely
`dotnet build`, `dotnet test`, `dotnet format`, `docker compose up/down`, `npm run lint`,
`npm run build`, `git status/diff/log`. Anything destructive (push, reset, rm) — ask first.

## Definition of done for any slice
1. AC test(s) exist and are green against real Postgres
2. `dotnet format` clean; web `npm run lint` clean
3. Spec section referenced in the PR body
4. AI-DEVELOPMENT.md entry appended
