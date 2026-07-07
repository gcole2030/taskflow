using Api.Common;
using Dapper;
using Npgsql;
using Priority = Api.Domain.Priority;

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
}
