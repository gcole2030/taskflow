using Api.Features.Tasks;
using Priority = Api.Domain.Priority;
using TaskStatus = Api.Domain.TaskStatus;

namespace Api.UnitTests.Features;

public class TaskPatchMergerTests
{
    private static readonly TaskDto Current = new()
    {
        Id = Guid.NewGuid(),
        Title = "Original title",
        Description = "Original description",
        Status = TaskStatus.TODO,
        Priority = Priority.LOW,
        DueDate = new DateOnly(2026, 8, 1),
        CreatedAt = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
    };

    [Fact]
    public void Merge_NoFieldsSet_LeavesEverythingUnchanged()
    {
        var merged = TaskPatchMerger.Merge(Current, new PatchTaskRequest());

        Assert.Equal(Current.Title, merged.Title);
        Assert.Equal(Current.Description, merged.Description);
        Assert.Equal(Current.Priority, merged.Priority);
        Assert.Equal(Current.DueDate, merged.DueDate);
    }

    [Fact]
    public void Merge_OnlyPrioritySet_LeavesOtherFieldsUnchanged()
    {
        var patch = new PatchTaskRequest { PrioritySet = true, Priority = Priority.HIGH };

        var merged = TaskPatchMerger.Merge(Current, patch);

        Assert.Equal(Current.Title, merged.Title);
        Assert.Equal(Current.Description, merged.Description);
        Assert.Equal(Priority.HIGH, merged.Priority);
        Assert.Equal(Current.DueDate, merged.DueDate);
    }

    [Fact]
    public void Merge_DescriptionSetToNull_ClearsIt()
    {
        var patch = new PatchTaskRequest { DescriptionSet = true, Description = null };

        var merged = TaskPatchMerger.Merge(Current, patch);

        Assert.Null(merged.Description);
    }

    [Fact]
    public void Merge_DueDateSetToNull_ClearsIt()
    {
        var patch = new PatchTaskRequest { DueDateSet = true, DueDate = null };

        var merged = TaskPatchMerger.Merge(Current, patch);

        Assert.Null(merged.DueDate);
    }

    [Fact]
    public void Diff_NoFieldsChanged_ReturnsEmpty()
    {
        var merged = TaskPatchMerger.Merge(Current, new PatchTaskRequest());

        var diff = TaskPatchMerger.Diff(Current, merged);

        Assert.Empty(diff);
    }

    [Fact]
    public void Diff_PrioritySet_ButSameValue_ReturnsEmpty()
    {
        var patch = new PatchTaskRequest { PrioritySet = true, Priority = Current.Priority };
        var merged = TaskPatchMerger.Merge(Current, patch);

        var diff = TaskPatchMerger.Diff(Current, merged);

        Assert.Empty(diff);
    }

    [Fact]
    public void Diff_PriorityChanged_IncludesOnlyPriority()
    {
        var patch = new PatchTaskRequest { PrioritySet = true, Priority = Priority.HIGH };
        var merged = TaskPatchMerger.Merge(Current, patch);

        var diff = TaskPatchMerger.Diff(Current, merged);

        Assert.Single(diff);
        Assert.True(diff.ContainsKey("priority"));
    }

    [Fact]
    public void Diff_MultipleFieldsChanged_IncludesAllOfThem()
    {
        var patch = new PatchTaskRequest
        {
            TitleSet = true,
            Title = "New title",
            PrioritySet = true,
            Priority = Priority.HIGH,
        };
        var merged = TaskPatchMerger.Merge(Current, patch);

        var diff = TaskPatchMerger.Diff(Current, merged);

        Assert.Equal(2, diff.Count);
        Assert.True(diff.ContainsKey("title"));
        Assert.True(diff.ContainsKey("priority"));
    }
}
