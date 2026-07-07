using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.IntegrationTests.Infrastructure;
using Dapper;
using Npgsql;

namespace Api.IntegrationTests.Features;

public class CreateAndGetTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task AC1_Post_ValidPayload_Returns201_WithLocationHeader_AndCreatedEvent()
    {
        var payload = new
        {
            title = "Write the spec",
            description = "Draft v1",
            priority = "HIGH",
            dueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7),
        };

        var response = await Client.PostAsJsonAsync("/api/v1/tasks", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var id = body.GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal("Write the spec", body.GetProperty("title").GetString());
        Assert.Equal("TODO", body.GetProperty("status").GetString());
        Assert.True(body.TryGetProperty("createdAt", out _));
        Assert.True(body.TryGetProperty("updatedAt", out _));

        await using var connection = new NpgsqlConnection(Fixture.ConnectionString);
        var eventType = await connection.QuerySingleAsync<string>(
            "SELECT event_type FROM task_events WHERE task_id = @Id", new { Id = id });
        Assert.Equal("CREATED", eventType);
    }

    [Fact]
    public async Task AC2_Post_SameIdempotencyKey_ReturnsOriginalTask_NoDuplicateTaskOrEvent()
    {
        var key = Guid.NewGuid().ToString();
        var payload = new { title = "Idempotent task" };

        var response1 = await SendCreateAsync(payload, key);
        var response2 = await SendCreateAsync(payload, key);

        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        Assert.True(response2.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created);

        var body1 = await response1.Content.ReadFromJsonAsync<JsonElement>();
        var body2 = await response2.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(body1.GetProperty("id").GetGuid(), body2.GetProperty("id").GetGuid());

        await using var connection = new NpgsqlConnection(Fixture.ConnectionString);
        var taskCount = await connection.QuerySingleAsync<long>("SELECT count(*) FROM tasks");
        var eventCount = await connection.QuerySingleAsync<long>("SELECT count(*) FROM task_events");
        Assert.Equal(1, taskCount);
        Assert.Equal(1, eventCount);
    }

    [Fact]
    public async Task AC3_Post_EmptyTitle_Returns400_WithFieldErrors()
    {
        var payload = new { title = "" };

        var response = await Client.PostAsJsonAsync("/api/v1/tasks", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("errors").TryGetProperty("title", out _));
    }

    [Fact]
    public async Task AC3_Post_PastDueDate_Returns400_WithFieldErrors()
    {
        var payload = new
        {
            title = "Valid title",
            dueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1),
        };

        var response = await Client.PostAsJsonAsync("/api/v1/tasks", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("errors").TryGetProperty("dueDate", out _));
    }

    [Fact]
    public async Task GetById_ReturnsTask_WhenItExists()
    {
        var created = await Client.PostAsJsonAsync("/api/v1/tasks", new { title = "Fetch me" });
        var createdBody = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = createdBody.GetProperty("id").GetGuid();

        var response = await Client.GetAsync($"/api/v1/tasks/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(id, body.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task GetById_Returns404ProblemDetails_WhenTaskDoesNotExist()
    {
        var response = await Client.GetAsync($"/api/v1/tasks/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<HttpResponseMessage> SendCreateAsync(object payload, string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/tasks")
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await Client.SendAsync(request);
    }
}
