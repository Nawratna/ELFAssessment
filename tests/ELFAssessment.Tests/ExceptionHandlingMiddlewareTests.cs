using ELFAssessment.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace ELFAssessment.Tests;

public class ExceptionHandlingMiddlewareTests
{
    private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _loggerMock;

    public ExceptionHandlingMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<ExceptionHandlingMiddleware>>();
    }

    [Fact]
    public async Task InvokeAsync_NoException_PassesThrough()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(_ => Task.CompletedTask, _loggerMock.Object);

        await middleware.InvokeAsync(context);

        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ArgumentException_Returns400()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new ArgumentException("Bad input"),
            _loggerMock.Object);

        await middleware.InvokeAsync(context);

        Assert.Equal(400, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_KeyNotFoundException_Returns404()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new KeyNotFoundException("Not found"),
            _loggerMock.Object);

        await middleware.InvokeAsync(context);

        Assert.Equal(404, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_GenericException_Returns500()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("Server error"),
            _loggerMock.Object);

        await middleware.InvokeAsync(context);

        Assert.Equal(500, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WritesJsonBody()
    {
        var context = new DefaultHttpContext();
        var bodyStream = new MemoryStream();
        context.Response.Body = bodyStream;
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new ArgumentException("Test error"),
            _loggerMock.Object);

        await middleware.InvokeAsync(context);

        bodyStream.Seek(0, SeekOrigin.Begin);
        var body = new StreamReader(bodyStream).ReadToEnd();
        Assert.Contains("Test error", body);
        Assert.Contains("traceId", body);
    }
}
