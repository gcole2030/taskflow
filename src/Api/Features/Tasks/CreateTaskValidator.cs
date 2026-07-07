using Api.Common;

namespace Api.Features.Tasks;

public sealed class CreateTaskValidator(IClock clock)
{
    public IDictionary<string, string[]> Validate(CreateTaskRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200)
            errors["title"] = ["Title is required and must be 1-200 characters."];

        if (request.Description is { Length: > 2000 })
            errors["description"] = ["Description must be at most 2000 characters."];

        if (request.DueDate is { } dueDate && dueDate < DateOnly.FromDateTime(clock.UtcNow.UtcDateTime))
            errors["dueDate"] = ["Due date must not be in the past."];

        return errors;
    }
}
