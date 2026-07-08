using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ImaginArte.Server.Tests;

public sealed class HealthEndpointTests
{
    [Fact]
    public async Task Health_ReturnsOk()
    {
        using var application = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
                builder.UseSetting("Persistence:Provider", "InMemory"));
        using var client = application.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
