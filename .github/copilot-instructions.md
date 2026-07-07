# Copilot instructions — Task Management System
Role in this repo: SECOND reviewer. Claude Code is the primary implementation agent.
When reviewing PRs, check specifically:
1. Every changed endpoint maps to an acceptance criterion in docs/spec-taskmanager.md §5.
2. Task writes and event writes share one transaction.
3. Illegal state transitions return 409; validation failures 400 problem+json.
4. Tests use Testcontainers real Postgres — flag any mocked repository in integration tests.
5. Migrations are additive; no edits to applied migration files.
6. No EF Core, no repository-of-repositories abstraction, no scope creep beyond spec §6/§9.
Style: parameterized SQL only; TypedResults; conventional commits.
