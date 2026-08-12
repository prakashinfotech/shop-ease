using System.Text.Json;
using Xunit;
using EBayClone.API.Middleware;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace EBayClone.Tests.Middleware;

public class GlobalExceptionMiddlewareTests
{
    private readonly Mock<ILogger<GlobalExceptionMiddleware>> _logger = new();

    private GlobalExceptionMiddleware CreateSut(RequestDelegate next) =>
        new(next, _logger.Object);

    private static DefaultHttpContext CreateContext()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    [Fact]
    public async Task Invoke_KeyNotFoundException_Returns404WithJson()
    {
        var sut = CreateSut(_ => throw new KeyNotFoundException("not found"));
        var ctx = CreateContext();

        await sut.InvokeAsync(ctx);

        Assert.Equal(404, ctx.Response.StatusCode);
        Assert.Equal("application/json", ctx.Response.ContentType);
    }

    [Fact]
    public async Task Invoke_UnauthorizedAccessException_Returns401()
    {
        var sut = CreateSut(_ => throw new UnauthorizedAccessException("denied"));
        var ctx = CreateContext();

        await sut.InvokeAsync(ctx);

        Assert.Equal(401, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task Invoke_InvalidOperationException_Returns409()
    {
        var sut = CreateSut(_ => throw new InvalidOperationException("conflict"));
        var ctx = CreateContext();

        await sut.InvokeAsync(ctx);

        Assert.Equal(409, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task Invoke_ValidationException_Returns400()
    {
        var failures = new List<ValidationFailure>
        {
            new("Field", "Field is required")
        };
        var sut = CreateSut(_ => throw new ValidationException(failures));
        var ctx = CreateContext();

        await sut.InvokeAsync(ctx);

        Assert.Equal(400, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task Invoke_UnhandledException_Returns500()
    {
        var sut = CreateSut(_ => throw new Exception("unexpected"));
        var ctx = CreateContext();

        await sut.InvokeAsync(ctx);

        Assert.Equal(500, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task Invoke_ResponseBody_IsValidJson()
    {
        var sut = CreateSut(_ => throw new KeyNotFoundException("entity missing"));
        var ctx = CreateContext();

        await sut.InvokeAsync(ctx);

        ctx.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(ctx.Response.Body).ReadToEndAsync();
        var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("success", out _));
    }

    [Fact]
    public async Task Invoke_NoException_PassesThrough()
    {
        var sut = CreateSut(_ => Task.CompletedTask);
        var ctx = CreateContext();

        await sut.InvokeAsync(ctx);

        Assert.Equal(200, ctx.Response.StatusCode);
    }
}
