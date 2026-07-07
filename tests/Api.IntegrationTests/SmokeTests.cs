using System.Net;
using Api.IntegrationTests.Infrastructure;

namespace Api.IntegrationTests;

public class SmokeTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task GetReadyz_Returns200()
    {
        var response = await Client.GetAsync("/readyz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
