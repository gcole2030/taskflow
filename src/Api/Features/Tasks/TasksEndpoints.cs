using System.Text.Json;
using Api.Common;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Priority = Api.Domain.Priority;
using TaskStatus = Api.Domain.TaskStatus;

namespace Api.Features.Tasks;

public static class TasksEndpoints
{
    private static readonly JsonElement EmptyMetadata = JsonDocument.Parse("{}").RootElement;

    public static IEndpointRouteBuilder MapTasks(this IEndpointRouteBuilder app)
    {
        app.MapPost("/tasks", CreateTaskAsync);
        app.MapGet("/tasks", ListTasksAsync);
        app.MapGet("/tasks/{id:guid}", GetTaskAsync);
        app.MapPatch("/tasks/{id:guid}", PatchTaskAsync);
        app.MapPost("/tasks/{id:guid}/transitions", TransitionAsync);
        app.MapGet("/tasks/{id:guid}/events", GetEventsAsync);
        return app;
    }

    private static async Task<Results<Created<TaskDto>, Ok<TaskDto>, ValidationProblem>> CreateTaskAsync(
        CreateTaskRequest request,
        HttpRequest httpRequest,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        TasksRepository repository,
        CreateTaskValidator validator)
    {
        var errors = validator.Validate(request);
        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        if (idempotencyKey is not null)
        {
            var existing = await repository.FindByIdempotencyKeyAsync(idempotencyKey);
            if (existing is not null)
                return TypedResults.Ok(existing);
        }

        var task = await repository.CreateAsync(request, idempotencyKey);
        return TypedResults.Created($"{httpRequest.Path}/{task.Id}", task);
    }

    private static async Task<Results<Ok<TaskDto>, ProblemHttpResult>> GetTaskAsync(
        Guid id, TasksRepository repository)
    {
        var task = await repository.GetByIdAsync(id);
        return task is not null
            ? TypedResults.Ok(task)
            : TypedResults.Problem(detail: $"Task '{id}' was not found.", statusCode: StatusCodes.Status404NotFound);
    }

    private static async Task<Results<Ok<TaskDto>, ProblemHttpResult>> TransitionAsync(
        Guid id, TransitionRequest request, TasksRepository repository)
    {
        var result = await repository.TransitionAsync(id, request.To, request.Metadata ?? EmptyMetadata);

        return result.Outcome switch
        {
            TransitionOutcome.Success => TypedResults.Ok(result.Task!),
            TransitionOutcome.NotFound => TypedResults.Problem(
                detail: $"Task '{id}' was not found.", statusCode: StatusCodes.Status404NotFound),
            TransitionOutcome.IllegalTransition => TypedResults.Problem(
                detail: $"Cannot transition from {result.CurrentStatus} to {request.To}.",
                statusCode: StatusCodes.Status409Conflict),
            _ => throw new InvalidOperationException($"Unhandled transition outcome: {result.Outcome}."),
        };
    }

    private static async Task<Results<Ok<IReadOnlyList<EventDto>>, ProblemHttpResult>> GetEventsAsync(
        Guid id, TasksRepository repository)
    {
        var events = await repository.GetEventsAsync(id);
        return events is not null
            ? TypedResults.Ok(events)
            : TypedResults.Problem(detail: $"Task '{id}' was not found.", statusCode: StatusCodes.Status404NotFound);
    }

    private static async Task<Results<Ok<TaskDto>, ValidationProblem, ProblemHttpResult>> PatchTaskAsync(
        Guid id, HttpRequest httpRequest, TasksRepository repository, PatchTaskValidator validator)
    {
        var body = await httpRequest.ReadFromJsonAsync<JsonElement>();
        var patch = PatchTaskRequest.FromJson(body);

        var errors = validator.Validate(patch);
        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        var task = await repository.PatchAsync(id, patch);
        return task is not null
            ? TypedResults.Ok(task)
            : TypedResults.Problem(detail: $"Task '{id}' was not found.", statusCode: StatusCodes.Status404NotFound);
    }

    private static async Task<Results<Ok<IReadOnlyList<TaskDto>>, ValidationProblem>> ListTasksAsync(
        HttpResponse httpResponse,
        TasksRepository repository,
        IClock clock,
        string? status = null,
        string? priority = null,
        bool overdue = false,
        int page = 1,
        int pageSize = 20)
    {
        // Bound as strings (not TaskStatus?/Priority? directly) so an unparseable value is a
        // 400, not a silently-dropped filter — the framework's default nullable-enum query
        // binding treats a failed parse as "not supplied" rather than an error.
        var errors = new Dictionary<string, string[]>();

        TaskStatus? parsedStatus = null;
        if (status is not null)
        {
            if (Enum.TryParse(status, out TaskStatus statusValue))
                parsedStatus = statusValue;
            else
                errors["status"] = [$"Status must be one of {string.Join(", ", Enum.GetNames<TaskStatus>())}."];
        }

        Priority? parsedPriority = null;
        if (priority is not null)
        {
            if (Enum.TryParse(priority, out Priority priorityValue))
                parsedPriority = priorityValue;
            else
                errors["priority"] = [$"Priority must be one of {string.Join(", ", Enum.GetNames<Priority>())}."];
        }

        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

        var (tasks, totalCount) =
            await repository.ListAsync(parsedStatus, parsedPriority, overdue, today, page, pageSize);

        httpResponse.Headers.Append("X-Total-Count", totalCount.ToString());
        return TypedResults.Ok(tasks);
    }
}
