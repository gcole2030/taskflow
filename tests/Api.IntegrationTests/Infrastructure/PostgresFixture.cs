using Dapper;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Api.IntegrationTests.Infrastructure;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("taskman")
        .WithUsername("taskman")
        .WithPassword("taskman")
        .Build();

    public ApiWebApplicationFactory Factory { get; private set; } = null!;

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        Factory = new ApiWebApplicationFactory(ConnectionString);
        _ = Factory.Server; // force host startup so DbUp migrations run before any test touches the DB
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _container.DisposeAsync();
    }

    public async Task ResetAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync(
            "TRUNCATE TABLE task_events, idempotency_keys, tasks RESTART IDENTITY CASCADE;");
    }
}
