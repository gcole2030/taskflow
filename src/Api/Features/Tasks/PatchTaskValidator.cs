namespace Api.Features.Tasks;

public sealed class PatchTaskValidator
{
    public IDictionary<string, string[]> Validate(PatchTaskRequest patch)
    {
        var errors = new Dictionary<string, string[]>();

        if (patch.TitleSet && (string.IsNullOrWhiteSpace(patch.Title) || patch.Title!.Length > 200))
            errors["title"] = ["Title must be 1-200 characters."];

        if (patch.DescriptionSet && patch.Description is { Length: > 2000 })
            errors["description"] = ["Description must be at most 2000 characters."];

        if (patch.PrioritySet && (patch.Priority is null || patch.PriorityInvalid))
            errors["priority"] = ["Priority must be one of LOW, MEDIUM, HIGH."];

        if (patch.DueDateInvalid)
            errors["dueDate"] = ["Due date must be a valid date."];

        // Deliberately no "must not be in the past" check here, unlike CreateTaskValidator:
        // spec §2 scopes that rule to "on create" only. It's also the only way to construct
        // an already-overdue task for AC11 without waiting in real time — don't add it back.
        return errors;
    }
}
