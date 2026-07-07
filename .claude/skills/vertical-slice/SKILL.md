---
name: vertical-slice
description: Use when implementing any feature/endpoint in this task manager. Encodes the team convention for a vertical slice — folder layout, Dapper patterns, endpoint registration, migration pairing, and the Testcontainers test that must exist before the code. Trigger for any request like "implement AC4", "add the transitions endpoint", "build the list/filter feature".
---

# Vertical Slice Convention

Every feature is one self-contained slice. Never spread a feature across layer folders.

## Folder layout (backend)
```
src/Api/Features/<FeatureName>/
├── <FeatureName>Endpoint.cs      # MapPost/MapGet + handler, request/response records
├── <FeatureName>Repository.cs    # Dapper SQL for this slice only
└── <FeatureName>Validator.cs     # only if the slice accepts input
```
Shared plumbing lives in `src/Api/Common/` (Db connection factory, ProblemDetails helpers,
state machine). Touch Common only when two slices genuinely need the same thing.

## The order of work (non-negotiable)
1. Read the AC(s) for this slice in docs/spec-taskmanager.md §5.
2. Write the integration test(s) in `tests/Api.IntegrationTests/Features/<FeatureName>Tests.cs`
   — Given/When/Then mapped 1:1 to the AC, named `AC<N>_...`. Run them. They must fail.
3. If the slice needs schema, add `db/migrations/NNN_<description>.sql`. Never modify old ones.
4. Implement endpoint + repository until the test is green. Nothing more.
5. `dotnet format` && `dotnet test`. Paste the green summary in your final message.

## Dapper patterns
- `NpgsqlDataSource` injected singleton; open connections per operation with `await using`.
- Parameterized SQL only. Multi-row reads: `QueryAsync<T>`. Writes returning the row:
  `INSERT ... RETURNING *` with `QuerySingleAsync<T>`.
- Task + event writes happen in ONE transaction (`NpgsqlTransaction`). A status change
  without its event is a bug even if all else works.
- Enums map as text via a snake_case type handler in Common. Don't invent per-slice mapping.

## Endpoint patterns
- Static class with `public static IEndpointRouteBuilder Map<FeatureName>(this ...)`,
  registered in Program.cs in one visible list.
- Return `TypedResults` (`Created`, `Ok`, `NotFound`, `Conflict`, `ValidationProblem`).
- State-machine rejections: 409 with problem+json `detail` naming the illegal transition.

## Testcontainers pattern
- One shared `PostgresFixture` (collection fixture) starting `postgres:16-alpine`,
  running DbUp migrations once; each test class gets a clean schema via
  `RESPAWN`-style truncation in `InitializeAsync`. Tests must be order-independent.
- Call endpoints through `WebApplicationFactory<Program>` HttpClient — never invoke
  handlers directly.

## Frontend slice (when the feature has UI)
```
web/src/features/<featureName>/
├── api.ts          # fetch wrappers, typed to the API contracts
├── use<Feature>.ts # TanStack Query hooks (query keys: ['tasks', filters])
└── components/
```
Mutations invalidate `['tasks']` and the affected `['task', id]`. Surface API field
errors inline on forms — do not swallow them into a generic toast.
