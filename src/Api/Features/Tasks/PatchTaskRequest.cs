using System.Text.Json;
using Priority = Api.Domain.Priority;

namespace Api.Features.Tasks;

public sealed record PatchTaskRequest
{
    public bool TitleSet { get; init; }
    public string? Title { get; init; }
    public bool DescriptionSet { get; init; }
    public string? Description { get; init; }
    public bool PrioritySet { get; init; }
    public Priority? Priority { get; init; }
    public bool PriorityInvalid { get; init; }
    public bool DueDateSet { get; init; }
    public DateOnly? DueDate { get; init; }
    public bool DueDateInvalid { get; init; }

    public static PatchTaskRequest FromJson(JsonElement body)
    {
        var result = new PatchTaskRequest();

        if (body.TryGetProperty("title", out var titleProp))
        {
            result = result with
            {
                TitleSet = true,
                Title = titleProp.ValueKind == JsonValueKind.Null ? null : titleProp.GetString(),
            };
        }

        if (body.TryGetProperty("description", out var descriptionProp))
        {
            result = result with
            {
                DescriptionSet = true,
                Description = descriptionProp.ValueKind == JsonValueKind.Null ? null : descriptionProp.GetString(),
            };
        }

        if (body.TryGetProperty("priority", out var priorityProp))
        {
            if (priorityProp.ValueKind == JsonValueKind.Null)
                result = result with { PrioritySet = true, Priority = null };
            else if (Enum.TryParse<Priority>(priorityProp.GetString(), out var parsedPriority))
                result = result with { PrioritySet = true, Priority = parsedPriority };
            else
                result = result with { PrioritySet = true, PriorityInvalid = true };
        }

        if (body.TryGetProperty("dueDate", out var dueDateProp))
        {
            if (dueDateProp.ValueKind == JsonValueKind.Null)
                result = result with { DueDateSet = true, DueDate = null };
            else if (DateOnly.TryParse(dueDateProp.GetString(), out var parsedDate))
                result = result with { DueDateSet = true, DueDate = parsedDate };
            else
                result = result with { DueDateSet = true, DueDateInvalid = true };
        }

        return result;
    }
}
