/implement-slice bootstrap — solution skeleton

Create the .NET 10 solution per CLAUDE.md and the vertical-slice skill:

1. taskman.sln with:
   - src/Api (minimal API): Serilog console logging, NpgsqlDataSource from
     ConnectionStrings__Db, DbUp running db/migrations/*.sql as embedded resources
     on startup, /healthz (no DB) and /readyz (SELECT 1), Common/ with a Db helper,
     enum text handler, ProblemDetails helpers, and Domain/ containing TaskStatus +
     Priority enums and a pure static StateMachine exposing CanTransition(from, to)
     and LegalTargets(from) per spec §3.
   - tests/Api.UnitTests (xUnit): exhaustive table-driven tests for StateMachine —
     every legal transition true, every illegal pair false, terminal states have
     zero legal targets.
   - tests/Api.IntegrationTests (xUnit + Testcontainers): a PostgresFixture
     collection fixture starting postgres:16-alpine, running the DbUp migrations,
     a WebApplicationFactory wired to the container, truncation reset between test
     classes, and one smoke test: GET /readyz returns 200.
2. Make `docker compose up` bring up db+api with /readyz healthy.
3. Show me the `dotnet test` output proving both test projects run and pass.
Branch is already created. Conventional commits. AI-DEVELOPMENT.md entry.
