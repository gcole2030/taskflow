# Changelog

Generated from the conventional-commit history on `main`. Each entry below is one
squash-merged PR (see [`docs/AI-DEVELOPMENT.md`](docs/AI-DEVELOPMENT.md) for the full
session narrative behind each).

## [Unreleased]

### Chore
- **packaging**: product README, this changelog, and an
  `docs/AI-DEVELOPMENT.md` process summary (`chore/packaging`)

## 2026-07-07

### Added
- **feat(web)**: Next.js frontend — task board with status filter tabs and priority
  badges, create-task form with inline API field errors, status-transition buttons
  driven by a `legalTransitions` map identical to spec §3, task detail page with an
  audit timeline
  ([#5](https://github.com/gcole2030/taskflow/pull/5))
- **feat(list-patch)**: `PATCH /tasks/{id}` (partial update, absent fields untouched,
  UPDATED event with a jsonb diff) and `GET /tasks` (status/priority/overdue filters,
  pagination, `X-Total-Count`) — AC9, AC10, AC11
  ([#4](https://github.com/gcole2030/taskflow/pull/4))
- **feat(transitions)**: `POST /tasks/{id}/transitions` (enforced by
  `Domain.StateMachine` only, 409 on illegal transitions) and
  `GET /tasks/{id}/events` (chronological audit trail) — AC4, AC5, AC6, AC7, AC8
  ([#3](https://github.com/gcole2030/taskflow/pull/3))
- **feat(create-and-get)**: `POST /tasks` (Idempotency-Key, UUIDv7 ids, CREATED event
  in the same transaction as the insert) and `GET /tasks/{id}` — AC1, AC2, AC3
  ([#2](https://github.com/gcole2030/taskflow/pull/2))
- **feat(bootstrap)**: solution skeleton — minimal API, Serilog, DbUp migrations on
  startup, `/healthz`/`/readyz`, and the pure `Domain.StateMachine` (spec §3)
  ([#1](https://github.com/gcole2030/taskflow/pull/1))

### Chore
- sync harness to final kit (versioned prompts, local review model, CI fixes)
- keep private prep notes out of the repo

---

Every PR above shipped with a red-run-first commit, a green implementation commit, an
independent AI review (`/review-slice`) resolved fix-or-rebut, and a
`docs/AI-DEVELOPMENT.md` entry — see that file for the ACs covered, corrections made,
and elapsed time per slice.
