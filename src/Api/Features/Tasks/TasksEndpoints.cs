using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.Tasks;

public static class TasksEndpoints
{
    public static IEndpointRouteBuilder MapTasks(this IEndpointRouteBuilder app)
    {
        app.MapPost("/tasks", CreateTaskAsync);
        app.MapGet("/tasks/{id:guid}", GetTaskAsync);
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
}
