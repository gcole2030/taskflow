# AI-DEVELOPMENT.md — process log
> Append-only. One entry per agent session/slice. This file is half the deliverable:
> it is the narrative evidence of how the system was built.

Format per entry:

## <date time> — <slice / task>
- **Agent & entry point:** Claude Code, `/implement-slice <args>` (or ad-hoc prompt)
- **Spec reference:** ACs covered
- **What the agent did:** (branch, red tests shown, migration, green run)
- **Human corrections:** what I rejected/redirected and why
- **Review:** AI review findings, resolution
- **Elapsed:** wall-clock for the slice

---
<!-- entries begin below -->

## 2026-07-07 15:25 — slice/bootstrap
- **Agent & entry point:** Claude Code, `/implement-slice bootstrap — solution skeleton`
- **Spec reference:** No AC is owned by this slice (infrastructure only). Built against
  §2 (domain model), §3 (state machine), §4 (`/healthz`, `/readyz`), §7 (Dapper/DbUp/
  Testcontainers/Serilog architecture decisions), §8 (`docker compose up` DoD).
- **What the agent did:**
  - Branch `slice/bootstrap` (pre-existing, as instructed).
  - Scaffolded `taskman.sln` + `src/Api` (minimal API), `tests/Api.UnitTests`,
    `tests/Api.IntegrationTests` via `dotnet new`; wired project + package references
    (Serilog.AspNetCore, Npgsql, Dapper, dbup-postgresql; Testcontainers.PostgreSql +
    Microsoft.AspNetCore.Mvc.Testing for integration tests).
  - Implemented pure `Domain/StateMachine` (`CanTransition`/`LegalTargets`) plus
    `TaskStatus`/`Priority` enums, and wrote exhaustive table-driven unit tests (all 25
    from/to pairs + terminal-state and legal-count assertions) — 30 unit tests, green.
  - Built the integration harness (`PostgresFixture` w/ Testcontainers postgres:16-alpine,
    `ApiWebApplicationFactory`, truncation reset per test class) and one smoke test,
    `GetReadyz_Returns200`.
  - **Red run shown**: with a bare `Program.cs` (no DbUp/no endpoints), the smoke test
    failed with `42P01: relation "task_events" does not exist` (30 unit / 0 integration
    passing, 1 integration failing) — committed at that state as the `test(bootstrap)`
    commit.
  - Implemented `Program.cs` (Serilog console logging, DbUp running
    `db/migrations/*.sql` as embedded resources on startup via a Serilog-backed
    `IUpgradeLog`, `NpgsqlDataSource` DI registration, Dapper enum-to-text type handler,
    `ProblemDetailsHelpers`) plus `/healthz` and `/readyz`. **Green run**: 30 unit + 1
    integration test passing — committed as the `feat(bootstrap)` commit.
  - Verified `docker compose up -d --build db api`: both containers reach `healthy`,
    `/healthz` and `/readyz` return 200, DbUp migration log appears via Serilog (not
    raw console). Web service intentionally excluded — no AC/slice has built `web/` yet.
  - `dotnet format` clean, `dotnet build -warnaserror` clean, `scripts/guardrails.sh` clean.
- **Human corrections:** none — self-corrected several issues found during the build:
  1. `Api.Domain.TaskStatus` collides with `System.Threading.Tasks.TaskStatus` (global
     implicit using in Sdk.Web); resolved by fully-qualifying/aliasing instead of a
     blanket `using Api.Domain;`.
  2. First `docker compose up` build failed (`NuGet fallback package folder` error) —
     the Windows-restored local `bin/`/`obj/` were being copied into the Linux build
     image, clobbering the container's own restore; fixed by adding `.dockerignore`.
  3. `dotnet new sln` on this SDK defaults to the new `.slnx` format, which the CI
     workflow's `ls ./*.sln` detection glob would silently miss; regenerated with
     `--format sln` to get `taskman.sln` as CLAUDE.md and CI both expect.
  4. Initial `PostgresFixture.ResetAsync()` ran before the app (and its DbUp migrations)
     had ever booted, since `WebApplicationFactory` starts the host lazily; fixed by
     forcing `Factory.Server` in `InitializeAsync` before returning.
  5. Reading `ConnectionStrings:Db` from `builder.Configuration` before `Build()` missed
     the test factory's injected connection string; moved resolution to `app.Configuration`
     (and a lazy DI factory for `NpgsqlDataSource`) so it reads post-merge configuration.
  6. `Testcontainers.PostgreSql`'s parameterless `PostgreSqlBuilder()` ctor is obsolete
     (would fail CI's `-warnaserror`); switched to `PostgreSqlBuilder("postgres:16-alpine")`.
- **Review:** No separate AI-reviewer pass run yet for this slice (infra-only, no AC
  surface); `scripts/guardrails.sh` mechanized checks are clean.
- **Elapsed:** ~1h (started 15:25).

## 2026-07-07 16:36 — slice/create-and-get
- **Agent & entry point:** Claude Code, `/implement-slice create-and-get` (via
  `prompts/20-create-and-get.md`, driven manually rather than through
  `scripts/next-slice.sh`/`make next` — that script launches an interactive
  nested `claude` session, which can't be driven from inside an existing
  session, so the same branch → tests-first → gate → PR steps were run directly).
- **Spec reference:** AC1, AC2, AC3 (§5); §2 domain model (Task fields, TaskEvent);
  §4 `POST /tasks` (Idempotency-Key), `GET /tasks/{id}` (404 when absent); §7
  (Dapper/Npgsql, single transaction for task+event writes).
- **What the agent did:**
  - Branched `slice/create-and-get` off `main` (after `slice/bootstrap` PR #1
    was squash-merged) and appended the log stub via `scripts/log-entry.sh`.
  - Wrote `CreateAndGetTests` (AC1: 201+Location+CREATED event; AC2:
    Idempotency-Key replay with no duplicate task/event, verified via direct
    DB assertions since there's no `/events` endpoint yet; AC3: empty title
    and past due_date → 400 with field errors) plus supplementary GET
    200/404 tests, and `CreateTaskValidatorTests` (11 cases) using a fake
    `IClock` for the due-date-in-the-past rule.
  - **Red run shown**: with `Program.cs` reverted to its pre-slice state (no
    `/tasks` routes registered), all 5 integration tests failed with 404
    (41 unit tests passing) — committed at that state as `test(create-and-get)`.
  - Implemented `TasksRepository` (task insert + CREATED event in one
    `NpgsqlTransaction`, UUIDv7 via `Guid.CreateVersion7()`, Idempotency-Key
    lookup/insert), `TasksEndpoints` (`POST`/`GET` under `/api/v1`, `TypedResults`
    throughout, `Results<Created<TaskDto>, Ok<TaskDto>, ValidationProblem>` for
    the create path), wired into `Program.cs` (JSON enum string conversion,
    DI registrations). **Green run**: 41 unit + 7 integration passing —
    committed as `feat(create-and-get)`.
  - `make gate` clean (guardrails now report AC1/AC2/AC3 test present;
    `dotnet format`/`-warnaserror` clean).
- **Human corrections:** none — self-corrected three Dapper issues discovered
  via the red→green cycle (all fixed before the feat commit, not left as
  follow-ups):
  1. Dapper has no built-in parameter support for `DateOnly` (`due_date`) —
     threw `NotSupportedException` on insert. Added `DateOnlyTypeHandler`.
  2. Dapper's `LookupDbType` converts enum parameters to their underlying int
     *before* consulting custom `ITypeHandler`s registered via `AddTypeHandler`
     — silently sent `"2"` instead of `"HIGH"` for `priority`, tripping the
     `tasks_priority_check` constraint. Worked around by calling `.ToString()`
     on enum values at the Dapper call site rather than relying on the handler
     for writes (the handler still works fine for reads/`Parse`).
  3. Dapper's "constructor-matching" materialization (used for record types
     without a parameterless constructor) bypasses custom type handlers
     entirely and requires the constructor's parameter types to exactly match
     the raw ADO column types — failed with `InvalidCastException` trying to
     read a `DateOnly` due_date and a `string` status/priority into a
     positional record. Fixed by rewriting `TaskDto` with `required ... { get; init; }`
     properties (no positional constructor), forcing Dapper's standard
     property-setter materialization path, which does respect custom handlers.
  4. Also normalized `TaskDto.CreatedAt`/`UpdatedAt` to `DateTime` (Npgsql's
     native mapping for `timestamptz`) instead of `DateTimeOffset`, sidestepping
     a second, unrelated type-matching gap in the same materialization path.
- **Review:** `/review-slice` → APPROVE with one should-fix (Idempotency-Key
  check-then-insert race) and three nits (enum-workaround comment, hardcoded
  Location header, guardrail-invisible SQL interpolation). All four fixed in
  a follow-up `fix(create-and-get)` commit before merge (see PR #2).
- **Elapsed:** ~40min (started 16:36).

## 2026-07-07 17:09 — slice/transitions
- **Agent & entry point:** Claude Code, `/implement-slice transitions` (via
  `prompts/30-transitions.md`), branched directly off `main` after PR #2
  (create-and-get) was squash-merged.
- **Spec reference:** AC4, AC5, AC6, AC7, AC8 (§5); §3 state machine
  (enforcement only — `Domain.StateMachine` itself was already built in the
  bootstrap slice, untouched here); §4 `POST /tasks/{id}/transitions`,
  `GET /tasks/{id}/events`; §7 (single transaction for status update + event).
- **What the agent did:**
  - Wrote `TransitionsTests` (AC4: TODO→IN_PROGRESS returns 200 + a
    STATUS_CHANGED event with from/to; AC5: DONE + any transition → 409, event
    count unchanged; AC6: TODO→DONE directly → 409; AC7: BLOCKED transition
    with `metadata.reason` surfaces on the event via `GET /events`; AC8: 3
    status changes → 4 events, chronological) plus supplementary 404 coverage
    for both new routes on a nonexistent task.
  - **Red run shown**: with `Program.cs`/`TasksEndpoints.cs`/`TasksRepository.cs`
    reverted to their pre-slice (post-create-and-get) versions, all 5 AC
    tests failed (10 passing: the 8 create-and-get tests plus the two new
    404 tests, which pass coincidentally since an unrouted path also 404s;
    41 unit tests unaffected) — committed at that state as `test(transitions)`.
  - Implemented `TasksRepository.TransitionAsync` (row lock via
    `SELECT ... FOR UPDATE` before checking `StateMachine.CanTransition`, so
    two concurrent transitions on the same task can't both read the same
    stale status; status UPDATE + STATUS_CHANGED event commit in one
    transaction; illegal/missing-task cases roll back before any event is
    written) and `GetEventsAsync` (ordered by the bigserial `id`, not
    `occurred_at`, so ordering is monotonic regardless of clock precision).
    Added `JsonElementTypeHandler` so `jsonb` metadata round-trips as nested
    JSON in responses rather than an escaped string. **Green run**: 41 unit +
    15 integration passing — committed as `feat(transitions)`.
  - `make gate` clean (AC4-AC8 now reported present alongside AC1-AC3).
- **Human corrections:** none — applied the lesson from the create-and-get
  review proactively this time (added the `FOR UPDATE` row lock up front,
  rather than shipping the race and fixing it in a follow-up review round).
- **Review:** `/review-slice` → APPROVE. Verified task+event writes share one
  transaction, transition legality checked only via `Domain.StateMachine`
  (grepped `src/Api/Features/` for status literals — zero matches), no
  untyped `Results.*`, no SQL injection surface. One non-blocking nit (a
  transition request missing the `"to"` field silently defaults to
  `TaskStatus.TODO`, always safely 409ing but with a confusing message) —
  left as-is per the APPROVE verdict, no fix commit needed. Merged as PR #3.
- **Elapsed:** ~35min (started 17:09).

## 2026-07-07 17:22 — slice/list-patch
- **Agent & entry point:** Claude Code, `/implement-slice list-patch` (via
  `prompts/40-list-patch.md`), branched directly off `main` after PR #3
  (transitions) was squash-merged.
- **Spec reference:** AC9, AC10, AC11 (§5); §4 `PATCH /tasks/{id}` (partial
  update of title/description/priority/due_date), `GET /tasks` (status/
  priority/overdue filters, page/pageSize default 1/20 cap 100, `X-Total-Count`);
  §2 due_date "must not be in the past **on create**" — read literally, so
  PATCH does *not* re-apply that rule (also the only way to construct AC11's
  scenario: an already-overdue task, built via PATCH after creation).
  All 11 ACs now covered by this repo.
- **What the agent did:**
  - Wrote `ListPatchTests` (AC9: PATCH changing priority → 200, `updated_at`
    advances, UPDATED event appended; AC10: 25 tasks, page 2/pageSize 10 →
    correct 10-item slice + `X-Total-Count: 25`; AC11: IN_PROGRESS task
    PATCHed to a past due_date appears in `overdue=true`, a DONE one doesn't)
    plus supplementary coverage (absent fields untouched, explicit-null
    clears a field, 404, priority filter, default pagination).
  - Wrote `TaskPatchMergerTests` (8 cases, no DB) for the pure merge/diff
    logic per the prompt's explicit ask ("unit tests: ... the PATCH
    field-merge logic").
  - **Red run shown**: with `Program.cs`/`TasksEndpoints.cs`/`TasksRepository.cs`
    reverted to their pre-slice versions, all 8 `ListPatchTests` failed
    (15 passing: the 8 create-and-get + 7 transitions tests; 49 unit tests
    unaffected) — committed at that state as `test(list-patch)`.
  - Implemented `PatchTaskRequest.FromJson` (parses a raw `JsonElement` body
    to distinguish "field omitted" from "field explicitly null," which a
    strongly-typed record can't do), `TaskPatchMerger` (pure merge + diff —
    only fields that actually changed value end up in the UPDATED event's
    metadata, not just fields that were "set"), `PatchTaskValidator`, and
    `TasksRepository.PatchAsync`/`ListAsync` (row-locked patch, one
    transaction; list filters built from parameterized SQL fragments via
    `DynamicParameters`, ordered by `id` since UUIDv7 is already chronological
    — avoids a `created_at` tie-breaking problem for AC10's page slicing).
    **Green run**: 49 unit + 23 integration passing — committed as
    `feat(list-patch)`.
  - `make gate` clean — **AC1 through AC11 all reported present.**
- **Human corrections:** none — self-corrected one API-surface mistake:
  assumed `JsonElement.GetDateOnly()` existed (it doesn't); fixed with
  `DateOnly.TryParse` on the raw string instead.
- **Review:** Pending — `/review-slice` to run before PR.
- **Elapsed:** ~40min (started 17:22).
