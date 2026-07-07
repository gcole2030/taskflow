namespace Api.Features.Tasks;

public sealed record MergedTask(string Title, string? Description, Api.Domain.Priority Priority, DateOnly? DueDate);

// Pure — no I/O, no DB. Isolates the "absent fields untouched" rule so it's unit-testable
// without a running Postgres.
public static class TaskPatchMerger
{
    public static MergedTask Merge(TaskDto current, PatchTaskRequest patch) => new(
        patch.TitleSet ? patch.Title! : current.Title,
        patch.DescriptionSet ? patch.Description : current.Description,
        patch.PrioritySet ? patch.Priority!.Value : current.Priority,
        patch.DueDateSet ? patch.DueDate : current.DueDate);

    public static IReadOnlyDictionary<string, object> Diff(TaskDto current, MergedTask merged)
    {
        var changes = new Dictionary<string, object>();

        if (current.Title != merged.Title)
            changes["title"] = new { from = current.Title, to = merged.Title };

        if (current.Description != merged.Description)
            changes["description"] = new { from = current.Description, to = merged.Description };

        if (current.Priority != merged.Priority)
            changes["priority"] = new { from = current.Priority.ToString(), to = merged.Priority.ToString() };

        if (current.DueDate != merged.DueDate)
            changes["dueDate"] = new { from = current.DueDate, to = merged.DueDate };

        return changes;
    }
}
