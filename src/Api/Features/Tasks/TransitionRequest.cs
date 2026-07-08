using System.Text.Json;
using TaskStatus = Api.Domain.TaskStatus;

namespace Api.Features.Tasks;

public sealed record TransitionRequest(TaskStatus To, JsonElement? Metadata);
