using FluentAssertions;
using Hapvida.ExternalIntegration.Api.Middleware;
using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Hapvida.ExternalIntegration.UnitTest.Api.Middleware;

public class GlobalExceptionHandlerMiddlewareTests : BaseTest
{
    private readonly Mock<ILogger<GlobalExceptionHandlerMiddleware>> _loggerMock;
    private readonly Mock<IWebHostEnvironment> _environmentMock;
    private readonly RequestDelegate _next;
    private readonly GlobalExceptionHandlerMiddleware _middleware;

    public GlobalExceptionHandlerMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        _environmentMock = new Mock<IWebHostEnvironment>();
        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        _next = context => Task.CompletedTask;
        _middleware = new GlobalExceptionHandlerMiddleware(_next, _loggerMock.Object, _environmentMock.Object);
    }

    [Fact]
    public async Task InvokeAsync_Should_Handle_ValidationException()
    {
        // Arrange
        var context = CreateHttpContext();
        var validationException = new ValidationException("Validation failed");
        var middleware = new GlobalExceptionHandlerMiddleware(
            _ => throw validationException,
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(400);
        context.Response.ContentType.Should().Be("application/problem+json");
        var body = await GetResponseBody(context);
        body.Should().Contain("validation-error");
        body.Should().Contain("valida");
    }

    [Fact]
    public async Task InvokeAsync_Should_Handle_ArgumentException()
    {
        // Arrange
        var context = CreateHttpContext();
        var argumentException = new ArgumentException("Invalid argument");
        var middleware = new GlobalExceptionHandlerMiddleware(
            _ => throw argumentException,
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(400);
        context.Response.ContentType.Should().Be("application/problem+json");
        var body = await GetResponseBody(context);
        body.Should().Contain("invalid-argument");
    }

    [Fact]
    public async Task InvokeAsync_Should_Handle_UnauthorizedAccessException()
    {
        // Arrange
        var context = CreateHttpContext();
        var unauthorizedException = new UnauthorizedAccessException("Unauthorized");
        var middleware = new GlobalExceptionHandlerMiddleware(
            _ => throw unauthorizedException,
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(401);
        context.Response.ContentType.Should().Be("application/problem+json");
        var body = await GetResponseBody(context);
        body.Should().Contain("unauthorized");
    }

    [Fact]
    public async Task InvokeAsync_Should_Handle_TimeoutException()
    {
        // Arrange
        var context = CreateHttpContext();
        var timeoutException = new TimeoutException("Request timeout");
        var middleware = new GlobalExceptionHandlerMiddleware(
            _ => throw timeoutException,
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(504);
        context.Response.ContentType.Should().Be("application/problem+json");
        var body = await GetResponseBody(context);
        body.Should().Contain("timeout");
    }

    [Fact]
    public async Task InvokeAsync_Should_Handle_HttpRequestException_With_Timeout()
    {
        // Arrange
        var context = CreateHttpContext();
        var httpException = new HttpRequestException("Request timeout occurred");
        var middleware = new GlobalExceptionHandlerMiddleware(
            _ => throw httpException,
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(504);
        context.Response.ContentType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task InvokeAsync_Should_Handle_Generic_Exception()
    {
        // Arrange
        var context = CreateHttpContext();
        var genericException = new Exception("Generic error");
        var middleware = new GlobalExceptionHandlerMiddleware(
            _ => throw genericException,
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(500);
        context.Response.ContentType.Should().Be("application/problem+json");
        var body = await GetResponseBody(context);
        body.Should().Contain("internal-server-error");
    }

    [Fact]
    public async Task InvokeAsync_Should_Include_TraceId_In_Response()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new Exception("Test error");
        var middleware = new GlobalExceptionHandlerMiddleware(
            _ => throw exception,
            _loggerMock.Object,
            _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var body = await GetResponseBody(context);
        body.Should().Contain("traceId");
        body.Should().Contain(context.TraceIdentifier);
    }

    [Fact]
    public async Task InvokeAsync_Should_Not_Throw_When_Response_Already_Started()
    {
        // Arrange
        var context = CreateHttpContext();
        // Não podemos definir HasStarted diretamente, mas podemos simular escrevendo no stream
        await context.Response.WriteAsync("test");
        var exception = new Exception("Test error");
        var middleware = new GlobalExceptionHandlerMiddleware(
            _ => throw exception,
            _loggerMock.Object,
            _environmentMock.Object);

        // Act & Assert
        await middleware.InvokeAsync(context);
        // Não deve lançar exceção mesmo com response já iniciado
    }

    private HttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/test";
        context.Request.Method = "GET";
        context.TraceIdentifier = "test-trace-id-123";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private async Task<string> GetResponseBody(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }
}

