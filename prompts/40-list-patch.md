/implement-slice list-patch AC9 AC10 AC11

Scope: PATCH /api/v1/tasks/{id} for title/description/priority/due_date (partial —
absent fields untouched; updated_at advances; UPDATED event carrying a jsonb diff of
changed fields). GET /api/v1/tasks with status/priority/overdue filters, page/pageSize
(defaults 1/20, cap 100), X-Total-Count from a windowed count.
Overdue means due_date < today AND status NOT IN (DONE, CANCELLED).
Unit tests: the filter builder if extracted; the PATCH field-merge logic.
Integration: AC9, AC10, AC11 red-first.
