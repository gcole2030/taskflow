using Priority = Api.Domain.Priority;

namespace Api.Features.Tasks;

public sealed record CreateTaskRequest(
    string Title,
    string? Description,
    Priority? Priority,
    DateOnly? DueDate);
