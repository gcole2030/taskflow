namespace Api.IntegrationTests.Infrastructure;

[Collection(ApiCollection.Name)]
public abstract class IntegrationTestBase(PostgresFixture fixture) : IAsyncLifetime
{
    protected HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();
        Client = fixture.Factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
