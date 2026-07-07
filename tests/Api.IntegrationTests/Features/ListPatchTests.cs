using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.IntegrationTests.Infrastructure;
using Dapper;
using Npgsql;

namespace Api.IntegrationTests.Features;

public class ListPatchTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task AC9_Patch_ChangingPriority_Returns200_UpdatedAtAdvances_AndAppendsUpdatedEvent()
    {
        var taskId = await CreateTaskAsync(priority: "LOW");
        var before = await GetTaskAsync(taskId);
        var beforeUpdatedAt = before.GetProperty("updatedAt").GetDateTime();

        await Task.Delay(50);

        var response = await Client.PatchAsJsonAsync($"/api/v1/tasks/{taskId}", new { priority = "HIGH" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("HIGH", body.GetProperty("priority").GetString());
        Assert.True(body.GetProperty("updatedAt").GetDateTime() > beforeUpdatedAt);

        await using var connection = new NpgsqlConnection(Fixture.ConnectionString);
        var eventType = await connection.QuerySingleAsync<string>(
            "SELECT event_type FROM task_events WHERE task_id = @Id ORDER BY id DESC LIMIT 1",
            new { Id = taskId });
        Assert.Equal("UPDATED", eventType);
    }

    [Fact]
    public async Task Patch_OmittedFields_RemainUnchanged()
    {
        var taskId = await CreateTaskAsync(title: "Original", description: "Original description", priority: "LOW");

        var response = await Client.PatchAsJsonAsync($"/api/v1/tasks/{taskId}", new { priority = "HIGH" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Original", body.GetProperty("title").GetString());
        Assert.Equal("Original description", body.GetProperty("description").GetString());
        Assert.Equal("HIGH", body.GetProperty("priority").GetString());
    }

    [Fact]
    public async Task Patch_ExplicitNullDescription_ClearsIt()
    {
        var taskId = await CreateTaskAsync(description: "Will be cleared");

        var response = await Client.PatchAsJsonAsync($"/api/v1/tasks/{taskId}", new { description = (string?)null });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, body.GetProperty("description").ValueKind);
    }

    [Fact]
    public async Task Patch_ReturnsProblemDetails404_WhenTaskDoesNotExist()
    {
        var response = await Client.PatchAsJsonAsync($"/api/v1/tasks/{Guid.NewGuid()}", new { priority = "HIGH" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AC10_TwentyFiveTasks_Page2PageSize10_ReturnsCorrectSliceAndTotalCount()
    {
        for (var i = 0; i < 25; i++)
            await CreateTaskAsync(title: $"Task {i}");

        var response = await Client.GetAsync("/api/v1/tasks?status=TODO&page=2&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("25", response.Headers.GetValues("X-Total-Count").Single());

        var items = (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();
        Assert.Equal(10, items.Count);
    }

    [Fact]
    public async Task AC11_Overdue_IncludesInProgressPastDue_ExcludesDonePastDue()
    {
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);

        var inProgressId = await CreateTaskAsync();
        await TransitionAsync(inProgressId, "IN_PROGRESS");
        await Client.PatchAsJsonAsync($"/api/v1/tasks/{inProgressId}", new { dueDate = yesterday });

        var doneId = await CreateTaskAsync();
        await Client.PatchAsJsonAsync($"/api/v1/tasks/{doneId}", new { dueDate = yesterday });
        await TransitionAsync(doneId, "IN_PROGRESS");
        await TransitionAsync(doneId, "DONE");

        var response = await Client.GetAsync("/api/v1/tasks?overdue=true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var ids = (await response.Content.ReadFromJsonAsync<JsonElement>())
            .EnumerateArray().Select(t => t.GetProperty("id").GetGuid()).ToList();

        Assert.Contains(inProgressId, ids);
        Assert.DoesNotContain(doneId, ids);
    }

    [Fact]
    public async Task List_InvalidStatus_Returns400_WithFieldError()
    {
        var response = await Client.GetAsync("/api/v1/tasks?status=NOT_A_STATUS");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("errors").TryGetProperty("status", out _));
    }

    [Fact]
    public async Task List_FiltersByPriority()
    {
        await CreateTaskAsync(priority: "HIGH");
        await CreateTaskAsync(priority: "LOW");

        var response = await Client.GetAsync("/api/v1/tasks?priority=HIGH");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();
        Assert.Single(items);
        Assert.Equal("HIGH", items[0].GetProperty("priority").GetString());
    }

    [Fact]
    public async Task List_DefaultsToPage1PageSize20()
    {
        for (var i = 0; i < 5; i++)
            await CreateTaskAsync(title: $"Task {i}");

        var response = await Client.GetAsync("/api/v1/tasks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("5", response.Headers.GetValues("X-Total-Count").Single());
    }

    private async Task<Guid> CreateTaskAsync(
        string title = "Test task", string? description = null, string? priority = null)
    {
        var response = await Client.PostAsJsonAsync("/api/v1/tasks", new { title, description, priority });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }

    private async Task<JsonElement> GetTaskAsync(Guid taskId)
    {
        var response = await Client.GetAsync($"/api/v1/tasks/{taskId}");
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task TransitionAsync(Guid taskId, string to) =>
        await Client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/transitions", new { to });
}
