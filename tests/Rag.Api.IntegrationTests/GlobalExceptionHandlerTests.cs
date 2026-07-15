using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Rag.Api.Infrastructure;

namespace Rag.Api.IntegrationTests;

public sealed class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_UnexpectedException_ReturnsSafeProblemDetails()
    {
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/failing-endpoint";
        await using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var handled = await handler.TryHandleAsync(
            context,
            new InvalidOperationException("sensitive internal detail"),
            CancellationToken.None);

        responseBody.Position = 0;
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(
            responseBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        problem.Should().NotBeNull();
        problem!.Detail.Should().NotContain("sensitive internal detail");
        problem.Instance.Should().Be("/failing-endpoint");
    }
}
