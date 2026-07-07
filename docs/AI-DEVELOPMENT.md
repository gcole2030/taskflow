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
