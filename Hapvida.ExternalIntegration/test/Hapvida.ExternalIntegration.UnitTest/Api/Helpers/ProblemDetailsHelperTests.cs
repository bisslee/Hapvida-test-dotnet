using FluentAssertions;
using Hapvida.ExternalIntegration.Api.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Hapvida.ExternalIntegration.UnitTest.Api.Helpers;

public class ProblemDetailsHelperTests : BaseTest
{
    private readonly DefaultHttpContext _httpContext;

    public ProblemDetailsHelperTests()
    {
        _httpContext = new DefaultHttpContext();
        _httpContext.Request.Path = "/api/v1/cep/123";
        _httpContext.TraceIdentifier = "test-trace-id";
    }

    [Fact]
    public void CreateInvalidCepProblemDetails_Should_Return_Correct_ProblemDetails()
    {
        // Arrange
        var detail = "CEP deve conter 8 dígitos.";

        // Act
        var result = ProblemDetailsHelper.CreateInvalidCepProblemDetails(_httpContext, detail);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be("https://errors.hapvida.externalintegration/invalid-cep");
        result.Title.Should().Be("CEP inválido");
        result.Status.Should().Be(400);
        result.Detail.Should().Be(detail);
        result.Instance.Should().Be(_httpContext.Request.Path);
        result.Extensions.Should().ContainKey("traceId");
        result.Extensions["traceId"].Should().Be("test-trace-id");
    }

    [Fact]
    public void CreateCepNotFoundProblemDetails_Should_Return_Correct_ProblemDetails()
    {
        // Arrange
        var zipCode = "99999999";

        // Act
        var result = ProblemDetailsHelper.CreateCepNotFoundProblemDetails(_httpContext, zipCode);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be("https://errors.hapvida.externalintegration/cep-not-found");
        result.Title.Should().Be("CEP não encontrado");
        result.Status.Should().Be(404);
        result.Detail.Should().Contain(zipCode);
        result.Instance.Should().Be(_httpContext.Request.Path);
        result.Extensions.Should().ContainKey("traceId");
        result.Extensions.Should().ContainKey("zipCode");
        result.Extensions["zipCode"].Should().Be(zipCode);
    }

    [Fact]
    public void CreateNoSavedCepProblemDetails_Should_Return_Correct_ProblemDetails()
    {
        // Act
        var result = ProblemDetailsHelper.CreateNoSavedCepProblemDetails(_httpContext);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be("https://errors.hapvida.externalintegration/no-saved-cep");
        result.Title.Should().Be("Nenhum CEP salvo");
        result.Status.Should().Be(404);
        result.Instance.Should().Be(_httpContext.Request.Path);
        result.Extensions.Should().ContainKey("traceId");
    }

    [Fact]
    public void CreateInvalidDaysParameterProblemDetails_Should_Return_Correct_ProblemDetails()
    {
        // Arrange
        int? days = 10;

        // Act
        var result = ProblemDetailsHelper.CreateInvalidDaysParameterProblemDetails(_httpContext, days);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be("https://errors.hapvida.externalintegration/invalid-days-parameter");
        result.Title.Should().Be("Parâmetro days inválido");
        result.Status.Should().Be(400);
        result.Detail.Should().Contain("1 e 7");
        result.Detail.Should().Contain("10");
        result.Extensions.Should().ContainKey("days");
        result.Extensions["days"].Should().Be(10);
    }

    [Fact]
    public void CreateTimeoutProblemDetails_Should_Return_Correct_ProblemDetails()
    {
        // Arrange
        var provider = "brasilapi";

        // Act
        var result = ProblemDetailsHelper.CreateTimeoutProblemDetails(_httpContext, provider);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be("https://errors.hapvida.externalintegration/timeout");
        result.Title.Should().Be("Timeout");
        result.Status.Should().Be(504);
        result.Detail.Should().Contain(provider);
        result.Extensions.Should().ContainKey("provider");
        result.Extensions["provider"].Should().Be(provider);
    }

    [Fact]
    public void CreateConflictProblemDetails_Should_Return_Correct_ProblemDetails()
    {
        // Arrange
        var detail = "CEP já está persistido no banco de dados.";

        // Act
        var result = ProblemDetailsHelper.CreateConflictProblemDetails(_httpContext, detail);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be("https://errors.hapvida.externalintegration/conflict");
        result.Title.Should().Be("Conflito");
        result.Status.Should().Be(409);
        result.Detail.Should().Be(detail);
        result.Extensions.Should().ContainKey("traceId");
    }
}

