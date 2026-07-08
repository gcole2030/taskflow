using Priority = Api.Domain.Priority;
using TaskStatus = Api.Domain.TaskStatus;

namespace Api.Features.Tasks;

public sealed record TaskDto
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required TaskStatus Status { get; init; }
    public required Priority Priority { get; init; }
    public DateOnly? DueDate { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
}
