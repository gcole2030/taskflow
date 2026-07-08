/implement-slice transitions AC4 AC5 AC6 AC7 AC8

Scope: POST /api/v1/tasks/{id}/transitions with body { "to": ..., "metadata": {...} },
enforced by Domain.StateMachine only — no duplicated transition logic in SQL or the
endpoint; 409 problem+json naming the illegal transition; STATUS_CHANGED event with
from/to/metadata in the same transaction; GET /api/v1/tasks/{id}/events chronological.
Red first: AC4–AC8 integration tests, show me the failing run.
