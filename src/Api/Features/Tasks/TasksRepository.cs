using System.Text.Json;
using Api.Common;
using Api.Domain;
using Dapper;
using Npgsql;
using Priority = Api.Domain.Priority;
using TaskStatus = Api.Domain.TaskStatus;

namespace Api.Features.Tasks;

public sealed class TasksRepository(NpgsqlDataSource dataSource, IClock clock)
{
    private const string SelectColumns =
        "id AS Id, title AS Title, description AS Description, status AS Status, " +
        "priority AS Priority, due_date AS DueDate, created_at AS CreatedAt, updated_at AS UpdatedAt";

    public async Task<TaskDto?> FindByIdempotencyKeyAsync(string idempotencyKey)
    {
        await using var connection = await dataSource.OpenConnectionAsync();

        var taskId = await connection.QuerySingleOrDefaultAsync<Guid?>(
            "SELECT task_id FROM idempotency_keys WHERE key = @Key", new { Key = idempotencyKey });

        if (taskId is null)
            return null;

        return await connection.QuerySingleOrDefaultAsync<TaskDto>(
            "SELECT " + SelectColumns + " FROM tasks WHERE id = @Id", new { Id = taskId.Value });
    }

    public async Task<TaskDto> CreateAsync(CreateTaskRequest request, string? idempotencyKey)
    {
        var id = Guid.CreateVersion7();
        var priority = request.Priority ?? Priority.MEDIUM;

        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        // Priority/Status are passed as .ToString() rather than the enum itself: Dapper's
        // LookupDbType coerces enum parameters to their underlying int *before* consulting
        // custom ITypeHandlers, so the registered EnumTypeHandler<T> is never reached on
        // writes (it still works fine for reads/Parse). Passing the string directly sidesteps
        // this — see AI-DEVELOPMENT.md for the failure this caused (tasks_priority_check).
        var task = await connection.QuerySingleAsync<TaskDto>(
            "INSERT INTO tasks (id, title, description, priority, due_date) " +
            "VALUES (@Id, @Title, @Description, @Priority, @DueDate) " +
            "RETURNING " + SelectColumns,
            new { Id = id, request.Title, request.Description, Priority = priority.ToString(), request.DueDate },
            transaction);

        await connection.ExecuteAsync(
            """
            INSERT INTO task_events (task_id, event_type, from_status, to_status, metadata, occurred_at)
            VALUES (@TaskId, 'CREATED', NULL, @Status, '{}'::jsonb, @OccurredAt)
            """,
            new { TaskId = id, Status = task.Status.ToString(), OccurredAt = clock.UtcNow },
            transaction);

        if (idempotencyKey is not null)
        {
            try
            {
                await connection.ExecuteAsync(
                    "INSERT INTO idempotency_keys (key, task_id) VALUES (@Key, @TaskId)",
                    new { Key = idempotencyKey, TaskId = id },
                    transaction);
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                // Lost a race against a concurrent request using the same Idempotency-Key:
                // roll back this attempt's task+event and return whichever one actually won.
                await transaction.RollbackAsync();
                return await FindByIdempotencyKeyAsync(idempotencyKey)
                    ?? throw new InvalidOperationException(
                        $"Idempotency key '{idempotencyKey}' conflicted but no task could be found.");
            }
        }

        await transaction.CommitAsync();
        return task;
    }

    public async Task<TaskDto?> GetByIdAsync(Guid id)
    {
        await using var connection = await dataSource.OpenConnectionAsync();

        return await connection.QuerySingleOrDefaultAsync<TaskDto>(
            "SELECT " + SelectColumns + " FROM tasks WHERE id = @Id", new { Id = id });
    }

    public async Task<TransitionResult> TransitionAsync(Guid id, TaskStatus to, JsonElement metadata)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        // FOR UPDATE serializes concurrent transitions on the same task: without it, two
        // requests could both read the same current status and both consider their
        // transition legal, letting an illegal (or duplicate) transition slip through.
        var current = await connection.QuerySingleOrDefaultAsync<TaskDto>(
            "SELECT " + SelectColumns + " FROM tasks WHERE id = @Id FOR UPDATE", new { Id = id }, transaction);

        if (current is null)
        {
            await transaction.RollbackAsync();
            return new TransitionResult(TransitionOutcome.NotFound);
        }

        if (!StateMachine.CanTransition(current.Status, to))
        {
            await transaction.RollbackAsync();
            return new TransitionResult(TransitionOutcome.IllegalTransition, CurrentStatus: current.Status);
        }

        var updated = await connection.QuerySingleAsync<TaskDto>(
            "UPDATE tasks SET status = @To, updated_at = now() WHERE id = @Id RETURNING " + SelectColumns,
            new { Id = id, To = to.ToString() },
            transaction);

        await connection.ExecuteAsync(
            """
            INSERT INTO task_events (task_id, event_type, from_status, to_status, metadata, occurred_at)
            VALUES (@TaskId, 'STATUS_CHANGED', @From, @To, @Metadata, @OccurredAt)
            """,
            new
            {
                TaskId = id,
                From = current.Status.ToString(),
                To = to.ToString(),
                Metadata = metadata,
                OccurredAt = clock.UtcNow,
            },
            transaction);

        await transaction.CommitAsync();
        return new TransitionResult(TransitionOutcome.Success, updated);
    }

    public async Task<IReadOnlyList<EventDto>?> GetEventsAsync(Guid id)
    {
        await using var connection = await dataSource.OpenConnectionAsync();

        var exists = await connection.QuerySingleOrDefaultAsync<Guid?>(
            "SELECT id FROM tasks WHERE id = @Id", new { Id = id });
        if (exists is null)
            return null;

        var events = await connection.QueryAsync<EventDto>(
            """
            SELECT id AS Id, task_id AS TaskId, event_type AS EventType, from_status AS FromStatus,
                   to_status AS ToStatus, metadata AS Metadata, occurred_at AS OccurredAt
            FROM task_events WHERE task_id = @Id ORDER BY id ASC
            """,
            new { Id = id });

        return events.ToList();
    }

    public async Task<TaskDto?> PatchAsync(Guid id, PatchTaskRequest patch)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        var current = await connection.QuerySingleOrDefaultAsync<TaskDto>(
            "SELECT " + SelectColumns + " FROM tasks WHERE id = @Id FOR UPDATE", new { Id = id }, transaction);

        if (current is null)
        {
            await transaction.RollbackAsync();
            return null;
        }

        var merged = TaskPatchMerger.Merge(current, patch);
        var diff = TaskPatchMerger.Diff(current, merged);

        if (diff.Count == 0)
        {
            // Nothing actually changed (fields omitted, or resent with identical values) —
            // a true no-op should not bump updated_at or append a spurious UPDATED event.
            await transaction.RollbackAsync();
            return current;
        }

        var updated = await connection.QuerySingleAsync<TaskDto>(
            "UPDATE tasks SET title = @Title, description = @Description, priority = @Priority, " +
            "due_date = @DueDate, updated_at = now() WHERE id = @Id RETURNING " + SelectColumns,
            new
            {
                Id = id,
                merged.Title,
                merged.Description,
                Priority = merged.Priority.ToString(),
                merged.DueDate,
            },
            transaction);

        await connection.ExecuteAsync(
            """
            INSERT INTO task_events (task_id, event_type, from_status, to_status, metadata, occurred_at)
            VALUES (@TaskId, 'UPDATED', NULL, NULL, @Metadata, @OccurredAt)
            """,
            new
            {
                TaskId = id,
                Metadata = JsonSerializer.SerializeToElement(diff),
                OccurredAt = clock.UtcNow,
            },
            transaction);

        await transaction.CommitAsync();
        return updated;
    }

    public async Task<(IReadOnlyList<TaskDto> Tasks, int TotalCount)> ListAsync(
        TaskStatus? status, Priority? priority, bool overdue, DateOnly today, int page, int pageSize)
    {
        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (status is not null)
        {
            conditions.Add("status = @Status");
            parameters.Add("Status", status.Value.ToString());
        }

        if (priority is not null)
        {
            conditions.Add("priority = @Priority");
            parameters.Add("Priority", priority.Value.ToString());
        }

        if (overdue)
        {
            conditions.Add("due_date < @Today AND status NOT IN ('DONE', 'CANCELLED')");
            parameters.Add("Today", today);
        }

        var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

        await using var connection = await dataSource.OpenConnectionAsync();

        var totalCount = await connection.QuerySingleAsync<int>(
            "SELECT count(*) FROM tasks " + whereClause, parameters);

        parameters.Add("Offset", (page - 1) * pageSize);
        parameters.Add("Limit", pageSize);

        var tasks = await connection.QueryAsync<TaskDto>(
            "SELECT " + SelectColumns + " FROM tasks " + whereClause +
            " ORDER BY id ASC LIMIT @Limit OFFSET @Offset",
            parameters);

        return (tasks.ToList(), totalCount);
    }
}
