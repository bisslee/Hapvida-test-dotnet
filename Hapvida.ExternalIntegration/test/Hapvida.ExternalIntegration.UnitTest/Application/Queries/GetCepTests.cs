using AutoMapper;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Hapvida.ExternalIntegration.Application.Helpers;
using Hapvida.ExternalIntegration.Application.Interfaces;
using Hapvida.ExternalIntegration.Application.Models;
using Hapvida.ExternalIntegration.Application.Queries.Cep.GetCep;
using Hapvida.ExternalIntegration.Domain.Entities;
using Hapvida.ExternalIntegration.Domain.ValueObjects;
using Hapvida.ExternalIntegration.UnitTest;
using Microsoft.Extensions.Logging;
using Moq;

namespace Hapvida.ExternalIntegration.UnitTest.Application.Queries;

public class GetCepTests : BaseTest
{
    private readonly Mock<ILogger<GetCepHandler>> LoggerMock;
    private readonly Mock<IValidator<GetCepRequest>> ValidatorMock;
    private readonly Mock<ICepService> CepServiceMock;
    private readonly Mock<IMapper> MapperMock;
    private readonly IResponseBuilder ResponseBuilder;
    private readonly GetCepHandler Handler;

    public GetCepTests()
    {
        LoggerMock = new Mock<ILogger<GetCepHandler>>();
        ValidatorMock = new Mock<IValidator<GetCepRequest>>();
        CepServiceMock = new Mock<ICepService>();
        MapperMock = new Mock<IMapper>();
        ResponseBuilder = new ResponseBuilder();

        Handler = new GetCepHandler(
            CepServiceMock.Object,
            ValidatorMock.Object,
            LoggerMock.Object,
            ResponseBuilder,
            MapperMock.Object
        );
    }

    private GetCepRequest CreateValidRequest()
    {
        return new GetCepRequest
        {
            ZipCode = "01306001"
        };
    }

    [Fact]
    public async Task Handle_Should_Return_Cep_Successfully()
    {
        // Arrange
        var request = CreateValidRequest();
        var zipCode = ZipCode.Create(request.ZipCode);
        var cepResult = new CepResult
        {
            ZipCode = zipCode,
            Street = "Rua Avanhandava",
            District = "Bela Vista",
            City = "São Paulo",
            State = "SP",
            Provider = "brasilapi"
        };
        var cepDto = new CepResponseDto
        {
            ZipCode = zipCode.Value,
            Street = cepResult.Street,
            District = cepResult.District,
            City = cepResult.City,
            State = cepResult.State,
            Provider = cepResult.Provider
        };

        ValidatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        CepServiceMock.Setup(s => s.GetCepAsync(It.IsAny<ZipCode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cepResult);

        MapperMock.Setup(m => m.Map<CepResponseDto>(cepResult))
            .Returns(cepDto);

        // Act
        var response = await Handler.Handle(request, CancellationToken.None);

        // Assert
        response.Success.Should().BeTrue();
        response.StatusCode.Should().Be(200);
        response.Data.Should().NotBeNull();
        response.Data!.ZipCode.Should().Be(zipCode.Value);
        response.Data.Provider.Should().Be("brasilapi");
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Cep_Not_Found()
    {
        // Arrange
        var request = CreateValidRequest();

        ValidatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        CepServiceMock.Setup(s => s.GetCepAsync(It.IsAny<ZipCode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CepResult?)null);

        // Act
        var response = await Handler.Handle(request, CancellationToken.None);

        // Assert
        response.Success.Should().BeFalse();
        response.StatusCode.Should().Be(404);
        response.Data.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_Validation_Fails()
    {
        // Arrange
        var request = CreateValidRequest();
        var validationFailures = new List<ValidationFailure> 
        { 
            new ValidationFailure("ZipCode", "CEP inválido") 
        };

        ValidatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // Act
        var response = await Handler.Handle(request, CancellationToken.None);

        // Assert
        response.Success.Should().BeFalse();
        response.StatusCode.Should().Be(400);
        response.Data.Should().BeNull();
        response.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_Invalid_ZipCode_Format()
    {
        // Arrange
        var request = new GetCepRequest
        {
            ZipCode = "123" // CEP inválido
        };

        ValidatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        // Act
        var response = await Handler.Handle(request, CancellationToken.None);

        // Assert
        response.Success.Should().BeFalse();
        response.StatusCode.Should().Be(400);
        response.Data.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_Throw_Exception_When_Internal_Error_Occurs()
    {
        // Arrange
        var request = CreateValidRequest();

        ValidatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        CepServiceMock.Setup(s => s.GetCepAsync(It.IsAny<ZipCode>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Erro interno"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => Handler.Handle(request, CancellationToken.None));

        exception.Message.Should().Be("Erro interno");
    }
}

