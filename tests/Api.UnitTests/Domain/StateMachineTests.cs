using TaskStatus = Api.Domain.TaskStatus;
using StateMachine = Api.Domain.StateMachine;

namespace Api.UnitTests.Domain;

public class StateMachineTests
{
    private static readonly HashSet<(TaskStatus From, TaskStatus To)> LegalTransitions =
    [
        (TaskStatus.TODO, TaskStatus.IN_PROGRESS),
        (TaskStatus.TODO, TaskStatus.CANCELLED),
        (TaskStatus.IN_PROGRESS, TaskStatus.BLOCKED),
        (TaskStatus.IN_PROGRESS, TaskStatus.DONE),
        (TaskStatus.IN_PROGRESS, TaskStatus.CANCELLED),
        (TaskStatus.BLOCKED, TaskStatus.IN_PROGRESS),
        (TaskStatus.BLOCKED, TaskStatus.CANCELLED),
    ];

    public static IEnumerable<object[]> AllPairs()
    {
        foreach (TaskStatus from in Enum.GetValues<TaskStatus>())
            foreach (TaskStatus to in Enum.GetValues<TaskStatus>())
                yield return [from, to];
    }

    [Theory]
    [MemberData(nameof(AllPairs))]
    public void CanTransition_MatchesSpecTable(TaskStatus from, TaskStatus to)
    {
        var expected = LegalTransitions.Contains((from, to));

        Assert.Equal(expected, StateMachine.CanTransition(from, to));
    }

    [Theory]
    [InlineData(TaskStatus.DONE)]
    [InlineData(TaskStatus.CANCELLED)]
    public void LegalTargets_TerminalStates_AreEmpty(TaskStatus terminal)
    {
        Assert.Empty(StateMachine.LegalTargets(terminal));
    }

    [Theory]
    [InlineData(TaskStatus.TODO, 2)]
    [InlineData(TaskStatus.IN_PROGRESS, 3)]
    [InlineData(TaskStatus.BLOCKED, 2)]
    public void LegalTargets_NonTerminalStates_MatchSpecCount(TaskStatus from, int expectedCount)
    {
        Assert.Equal(expectedCount, StateMachine.LegalTargets(from).Count);
    }
}
