using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.Tasks;

public static class TasksEndpoints
{
    private static readonly JsonElement EmptyMetadata = JsonDocument.Parse("{}").RootElement;

    public static IEndpointRouteBuilder MapTasks(this IEndpointRouteBuilder app)
    {
        app.MapPost("/tasks", CreateTaskAsync);
        app.MapGet("/tasks/{id:guid}", GetTaskAsync);
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
}
