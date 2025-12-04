using FluentAssertions;
using Hapvida.ExternalIntegration.Api.Controllers;
using Hapvida.ExternalIntegration.Application.Commands.Cep.AddZipCodeLookup;
using Hapvida.ExternalIntegration.Application.Models;
using Hapvida.ExternalIntegration.Application.Queries.Cep.GetCep;
using Hapvida.ExternalIntegration.Domain.Resources;
using Hapvida.ExternalIntegration.UnitTest;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Hapvida.ExternalIntegration.UnitTest.Api;

public class CepControllerTests : BaseTest
{
    private readonly Mock<IMediator> MediatorMock;
    private readonly Mock<ILogger<CepController>> LoggerMock;
    private readonly CepController Controller;

    public CepControllerTests()
    {
        MediatorMock = new Mock<IMediator>();
        LoggerMock = new Mock<ILogger<CepController>>();
        Controller = new CepController(
            MediatorMock.Object,
            LoggerMock.Object);

        Controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task Get_Should_Return_200_When_Cep_Found()
    {
        // Arrange
        var zipCode = "01306001";
        var response = new GetCepResponse
        {
            Success = true,
            StatusCode = 200,
            Data = new CepResponseDto
            {
                ZipCode = zipCode,
                City = "São Paulo",
                State = "SP",
                Provider = "brasilapi"
            }
        };

        MediatorMock.Setup(m => m.Send(It.IsAny<GetCepRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await Controller.Get(zipCode, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        (result as ObjectResult)!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Get_Should_Return_404_When_Cep_Not_Found()
    {
        // Arrange
        var zipCode = "99999999";
        var response = new GetCepResponse
        {
            Success = false,
            StatusCode = 404,
            Data = null
        };

        MediatorMock.Setup(m => m.Send(It.IsAny<GetCepRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await Controller.Get(zipCode, CancellationToken.None);

        // Assert
        // BaseControllerHandle retorna ObjectResult com StatusCode 404 quando StatusCode é 404
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Get_Should_Return_400_When_Validation_Fails()
    {
        // Arrange
        var zipCode = "123"; // CEP inválido
        var response = new GetCepResponse
        {
            Success = false,
            StatusCode = 400,
            Data = null,
            Errors = new List<string> { "CEP inválido" }
        };

        MediatorMock.Setup(m => m.Send(It.IsAny<GetCepRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await Controller.Get(zipCode, CancellationToken.None);

        // Assert
        // BaseControllerHandle retorna ObjectResult com ProblemDetails para erros 400
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Get_Should_Handle_Exception()
    {
        // Arrange
        var zipCode = "01306001";

        MediatorMock.Setup(m => m.Send(It.IsAny<GetCepRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Erro interno"));

        // Act & Assert
        // O controller re-lança a exceção para o middleware tratar
        await Assert.ThrowsAsync<Exception>(
            () => Controller.Get(zipCode, CancellationToken.None));
    }

    [Fact]
    public async Task Post_Should_Return_201_When_Cep_Persisted_Successfully()
    {
        // Arrange
        var request = new AddZipCodeLookupRequest { ZipCode = "01306001" };
        var zipCodeLookupDto = new ZipCodeLookupDto
        {
            Id = Guid.NewGuid(),
            ZipCode = "01306001",
            City = "São Paulo",
            State = "SP",
            Provider = "brasilapi",
            CreatedAtUtc = DateTime.UtcNow
        };
        var response = new AddZipCodeLookupResponse
        {
            Data = zipCodeLookupDto,
            Success = true,
            StatusCode = 201,
            Message = "CEP persistido com sucesso"
        };

        MediatorMock.Setup(m => m.Send(It.IsAny<AddZipCodeLookupRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        Controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await Controller.Post(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(201);
        ((objectResult.Value as AddZipCodeLookupResponse)!.Data)!.ZipCode.Should().Be(request.ZipCode);
    }

    [Fact]
    public async Task Post_Should_Return_400_When_Validation_Fails()
    {
        // Arrange
        var request = new AddZipCodeLookupRequest { ZipCode = "123" }; // CEP inválido
        var response = new AddZipCodeLookupResponse
        {
            Data = null,
            Success = false,
            StatusCode = 400,
            Message = HapvidaExternalIntegrationResource.Cep_InvalidTitle,
            Errors = new List<string> { "CEP inválido" }
        };

        MediatorMock.Setup(m => m.Send(It.IsAny<AddZipCodeLookupRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        Controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await Controller.Post(request, CancellationToken.None);

        // Assert
        // BaseControllerHandle retorna ObjectResult com ProblemDetails para erros 400
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Post_Should_Return_404_When_Cep_Not_Found()
    {
        // Arrange
        var request = new AddZipCodeLookupRequest { ZipCode = "99999999" };
        var response = new AddZipCodeLookupResponse
        {
            Data = null,
            Success = false,
            StatusCode = 404,
            Message = string.Format(HapvidaExternalIntegrationResource.Cep_NotFound, request.ZipCode)
        };

        MediatorMock.Setup(m => m.Send(It.IsAny<AddZipCodeLookupRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        Controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await Controller.Post(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Post_Should_Return_409_When_Cep_Already_Exists()
    {
        // Arrange
        var request = new AddZipCodeLookupRequest { ZipCode = "01306001" };
        var response = new AddZipCodeLookupResponse
        {
            Data = null,
            Success = false,
            StatusCode = 409,
            Message = "CEP já está persistido no banco de dados",
            Errors = new List<string> { "CEP já está persistido no banco de dados" }
        };

        MediatorMock.Setup(m => m.Send(It.IsAny<AddZipCodeLookupRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        Controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await Controller.Post(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Post_Should_Handle_Exception()
    {
        // Arrange
        var request = new AddZipCodeLookupRequest { ZipCode = "01306001" };
        var expectedException = new Exception("Internal server error");

        MediatorMock.Setup(m => m.Send(It.IsAny<AddZipCodeLookupRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        Controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act & Assert
        // O controller re-lança a exceção para o middleware tratar
        await Assert.ThrowsAsync<Exception>(
            () => Controller.Post(request, CancellationToken.None));
    }
}

