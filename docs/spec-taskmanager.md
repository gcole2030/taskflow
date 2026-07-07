# SPEC — Task Management System
### Version: FINAL (matched to written assignment, July 2026)
### Stack (as assigned): Docker · .NET 10 · React (Next.js) · PostgreSQL
### This document is the contract. Agents implement against it; nothing outside it gets built.

## 1. Problem statement
A simple task management system demonstrating an AI-native development process.
The product is deliberately small; the process is the deliverable.

## 2. Domain model
**Task**
- `id` UUID (server-generated, v7)
- `title` string, 1–200 chars, required
- `description` string, 0–2000 chars, optional
- `status` enum: `TODO | IN_PROGRESS | BLOCKED | DONE | CANCELLED`
- `priority` enum: `LOW | MEDIUM | HIGH` (default `MEDIUM`)
- `due_date` date, optional, must not be in the past on create
- `created_at`, `updated_at` timestamptz (server-managed)

**TaskEvent** (append-only audit trail)
- `id` bigserial
- `task_id` FK
- `event_type` enum: `CREATED | UPDATED | STATUS_CHANGED`
- `from_status`, `to_status` nullable
- `metadata` jsonb (e.g. `{"reason": "waiting on vendor"}` for BLOCKED)
- `occurred_at` timestamptz

## 3. State machine
```
TODO ──► IN_PROGRESS ──► DONE
 │  ▲        │  ▲
 │  └────────┤  │
 ▼           ▼  │
CANCELLED   BLOCKED
```
Legal transitions:
- TODO → IN_PROGRESS, TODO → CANCELLED
- IN_PROGRESS → BLOCKED, IN_PROGRESS → DONE, IN_PROGRESS → CANCELLED
- BLOCKED → IN_PROGRESS, BLOCKED → CANCELLED
- DONE and CANCELLED are terminal. Everything else is 409.
- Direct TODO → DONE is illegal (must pass through IN_PROGRESS).

## 4. API surface (minimal APIs, /api/v1)
- `POST   /tasks`                    create (supports `Idempotency-Key` header)
- `GET    /tasks`                    list; filters: `status`, `priority`, `overdue=true`;
                                     pagination `page`/`pageSize` (default 1/20, max 100);
                                     `X-Total-Count` response header
- `GET    /tasks/{id}`               fetch one (404 if absent)
- `PATCH  /tasks/{id}`               partial update of title/description/priority/due_date
- `POST   /tasks/{id}/transitions`   body `{ "to": "IN_PROGRESS", "metadata": {...} }`
- `GET    /tasks/{id}/events`        audit trail, chronological
- `GET    /healthz`                  liveness (no DB), `GET /readyz` readiness (DB ping)

Errors: RFC 9457 problem+json, field-level errors under `errors`.

## 5. Acceptance criteria (Given/When/Then — each becomes one integration test)
- **AC1**  Given a valid payload, When POST /tasks, Then 201 with Location header, body echoes task with server-set id/timestamps, and a CREATED event exists.
- **AC2**  Given the same POST retried with the same Idempotency-Key, Then 200/201 with the original task and no duplicate task or event.
- **AC3**  Given empty title or past due_date, When POST, Then 400 problem+json with field-level errors.
- **AC4**  Given TODO, When transition to IN_PROGRESS, Then 200 and a STATUS_CHANGED event appended with from/to.
- **AC5**  Given DONE, When any transition, Then 409 and zero new events.
- **AC6**  Given TODO, When transition directly to DONE, Then 409.
- **AC7**  Given a transition to BLOCKED with `metadata.reason`, When GET /events, Then the reason is present on the event.
- **AC8**  Given 3 status changes, When GET /events, Then 4 events (CREATED + 3) in chronological order.
- **AC9**  Given PATCH changing priority, Then 200, `updated_at` advances, and an UPDATED event is appended.
- **AC10** Given 25 tasks, When GET /tasks?status=TODO&page=2&pageSize=10, Then the correct slice and accurate X-Total-Count.
- **AC11** Given a task due yesterday in IN_PROGRESS, When GET /tasks?overdue=true, Then it appears; a DONE task due yesterday does not.

## 6. Frontend scope (required by assignment — keep lean)
Next.js (App Router) + TanStack Query, containerized in compose:
- Task board/list with status filter and priority badge
- Create-task form with inline validation errors from the API
- Status-change actions honoring the state machine (illegal buttons disabled)
- Task detail with audit timeline (events)
No auth, no design heroics; clean and functional.

## 7. Architecture decisions (pre-made, recorded as ADRs)
- Vertical slice architecture; one folder per feature (see .claude/skills/vertical-slice)
- Dapper + Npgsql over EF Core (ADR-0001)
- DbUp for versioned SQL migrations, run on API startup
- xUnit + Testcontainers-for-.NET against real postgres:16 (no in-memory fakes)
- Serilog structured logging, request logging middleware

## 8. Definition of done
- `docker compose up` from a clean clone yields working web + api + db in < 2 min
- All 11 AC tests green in CI against real Postgres
- ≥ 3 merged PRs, each with AI code-review trail
- CLAUDE.md, skill, commands visibly used; docs/AI-DEVELOPMENT.md narrates the process
- README with quickstart + Mermaid architecture diagram + generated CHANGELOG

## 9. Deliberately cut (say so in the README — this is the "efficient" evidence)
AuthN/Z · multi-tenancy · projects/boards · comments/attachments · transactional outbox ·
websockets/live updates · rate limiting · cloud IaC (assignment names Docker only —
compose is the entire deployment story)
