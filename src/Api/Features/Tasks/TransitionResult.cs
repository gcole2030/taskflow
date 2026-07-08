using TaskStatus = Api.Domain.TaskStatus;

namespace Api.Features.Tasks;

public enum TransitionOutcome
{
    Success,
    NotFound,
    IllegalTransition,
}

public sealed record TransitionResult(TransitionOutcome Outcome, TaskDto? Task = null, TaskStatus? CurrentStatus = null);
