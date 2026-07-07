using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.IntegrationTests.Infrastructure;
using Dapper;
using Npgsql;

namespace Api.IntegrationTests.Features;

public class TransitionsTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task AC4_Todo_To_InProgress_Returns200_And_AppendsEvent()
    {
        var taskId = await CreateTaskAsync();

        var response = await TransitionAsync(taskId, "IN_PROGRESS");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("IN_PROGRESS", body.GetProperty("status").GetString());

        await using var connection = new NpgsqlConnection(Fixture.ConnectionString);
        var events = (await connection.QueryAsync<(string event_type, string? from_status, string? to_status)>(
            "SELECT event_type, from_status, to_status FROM task_events WHERE task_id = @Id ORDER BY id",
            new { Id = taskId })).ToList();

        Assert.Equal(2, events.Count);
        Assert.Equal("STATUS_CHANGED", events[1].event_type);
        Assert.Equal("TODO", events[1].from_status);
        Assert.Equal("IN_PROGRESS", events[1].to_status);
    }

    [Fact]
    public async Task AC5_Done_AnyTransition_Returns409_AndZeroNewEvents()
    {
        var taskId = await CreateTaskAsync();
        await TransitionAsync(taskId, "IN_PROGRESS");
        await TransitionAsync(taskId, "DONE");

        await using var connection = new NpgsqlConnection(Fixture.ConnectionString);
        var countBefore = await connection.QuerySingleAsync<long>(
            "SELECT count(*) FROM task_events WHERE task_id = @Id", new { Id = taskId });

        var response = await TransitionAsync(taskId, "CANCELLED");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var countAfter = await connection.QuerySingleAsync<long>(
            "SELECT count(*) FROM task_events WHERE task_id = @Id", new { Id = taskId });
        Assert.Equal(countBefore, countAfter);
    }

    [Fact]
    public async Task AC6_Todo_To_Done_Directly_Returns409()
    {
        var taskId = await CreateTaskAsync();

        var response = await TransitionAsync(taskId, "DONE");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AC7_TransitionToBlocked_WithReason_ReasonPresentOnEvent()
    {
        var taskId = await CreateTaskAsync();
        await TransitionAsync(taskId, "IN_PROGRESS");

        var response = await Client.PostAsJsonAsync(
            $"/api/v1/tasks/{taskId}/transitions",
            new { to = "BLOCKED", metadata = new { reason = "waiting on vendor" } });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var events = await GetEventsAsync(taskId);
        var blockedEvent = events.EnumerateArray().Last();
        Assert.Equal("waiting on vendor", blockedEvent.GetProperty("metadata").GetProperty("reason").GetString());
    }

    [Fact]
    public async Task AC8_ThreeStatusChanges_Returns4EventsInChronologicalOrder()
    {
        var taskId = await CreateTaskAsync();
        await TransitionAsync(taskId, "IN_PROGRESS");
        await TransitionAsync(taskId, "BLOCKED");
        await TransitionAsync(taskId, "IN_PROGRESS");

        var events = (await GetEventsAsync(taskId)).EnumerateArray().ToList();

        Assert.Equal(4, events.Count);
        Assert.Equal("CREATED", events[0].GetProperty("eventType").GetString());
        Assert.Equal("STATUS_CHANGED", events[1].GetProperty("eventType").GetString());
        Assert.Equal("STATUS_CHANGED", events[2].GetProperty("eventType").GetString());
        Assert.Equal("STATUS_CHANGED", events[3].GetProperty("eventType").GetString());

        var occurredAts = events.Select(e => e.GetProperty("occurredAt").GetDateTime()).ToList();
        Assert.Equal(occurredAts, occurredAts.OrderBy(x => x));
    }

    [Fact]
    public async Task Transition_ReturnsProblemDetails404_WhenTaskDoesNotExist()
    {
        var response = await TransitionAsync(Guid.NewGuid(), "IN_PROGRESS");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetEvents_ReturnsProblemDetails404_WhenTaskDoesNotExist()
    {
        var response = await Client.GetAsync($"/api/v1/tasks/{Guid.NewGuid()}/events");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<Guid> CreateTaskAsync()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/tasks", new { title = "Transition me" });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }

    private async Task<HttpResponseMessage> TransitionAsync(Guid taskId, string to) =>
        await Client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/transitions", new { to });

    private async Task<JsonElement> GetEventsAsync(Guid taskId)
    {
        var response = await Client.GetAsync($"/api/v1/tasks/{taskId}/events");
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
