-- 001: tasks + append-only audit trail
CREATE TABLE tasks (
    id          uuid PRIMARY KEY,
    title       varchar(200) NOT NULL CHECK (length(trim(title)) > 0),
    description varchar(2000),
    status      text NOT NULL DEFAULT 'TODO'
                CHECK (status IN ('TODO','IN_PROGRESS','BLOCKED','DONE','CANCELLED')),
    priority    text NOT NULL DEFAULT 'MEDIUM'
                CHECK (priority IN ('LOW','MEDIUM','HIGH')),
    due_date    date,
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE task_events (
    id          bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    task_id     uuid NOT NULL REFERENCES tasks(id) ON DELETE CASCADE,
    event_type  text NOT NULL CHECK (event_type IN ('CREATED','UPDATED','STATUS_CHANGED')),
    from_status text,
    to_status   text,
    metadata    jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE idempotency_keys (
    key         text PRIMARY KEY,
    task_id     uuid NOT NULL REFERENCES tasks(id),
    created_at  timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX idx_tasks_status    ON tasks(status);
CREATE INDEX idx_tasks_due_date  ON tasks(due_date) WHERE due_date IS NOT NULL;
CREATE INDEX idx_events_task     ON task_events(task_id, occurred_at);
