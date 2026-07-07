namespace Api.Domain;

// Pure — no I/O, no DB. Spec §3.
public static class StateMachine
{
    private static readonly IReadOnlyDictionary<TaskStatus, TaskStatus[]> Transitions =
        new Dictionary<TaskStatus, TaskStatus[]>
        {
            [TaskStatus.TODO] = [TaskStatus.IN_PROGRESS, TaskStatus.CANCELLED],
            [TaskStatus.IN_PROGRESS] = [TaskStatus.BLOCKED, TaskStatus.DONE, TaskStatus.CANCELLED],
            [TaskStatus.BLOCKED] = [TaskStatus.IN_PROGRESS, TaskStatus.CANCELLED],
            [TaskStatus.DONE] = [],
            [TaskStatus.CANCELLED] = [],
        };

    public static bool CanTransition(TaskStatus from, TaskStatus to) =>
        Transitions[from].Contains(to);

    public static IReadOnlyList<TaskStatus> LegalTargets(TaskStatus from) =>
        Transitions[from];
}
