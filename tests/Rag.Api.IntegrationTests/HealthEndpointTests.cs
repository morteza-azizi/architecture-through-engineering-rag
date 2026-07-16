using System.Net;
using FluentAssertions;

namespace Rag.Api.IntegrationTests;

public sealed class HealthEndpointTests : IClassFixture<RagWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(RagWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetLiveness_ReturnsOk()
    {
        var response = await _client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
