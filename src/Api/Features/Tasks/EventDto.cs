using System.Text.Json;
using TaskStatus = Api.Domain.TaskStatus;

namespace Api.Features.Tasks;

public sealed record EventDto
{
    public required long Id { get; init; }
    public required Guid TaskId { get; init; }
    public required string EventType { get; init; }
    public TaskStatus? FromStatus { get; init; }
    public TaskStatus? ToStatus { get; init; }
    public required JsonElement Metadata { get; init; }
    public required DateTime OccurredAt { get; init; }
}
