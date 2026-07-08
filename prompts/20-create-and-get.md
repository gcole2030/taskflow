/implement-slice create-and-get AC1 AC2 AC3

Scope: POST /api/v1/tasks (Idempotency-Key via the idempotency_keys table, UUIDv7 ids,
CREATED event in the SAME transaction as the insert), GET /api/v1/tasks/{id}
(404 problem+json when absent).
Unit tests: CreateTaskValidator — title required/length, description length, due_date
not in the past via an injected clock.
Integration tests: AC1, AC2, AC3 — write them FIRST, show me the red dotnet test
output, then implement to green.
