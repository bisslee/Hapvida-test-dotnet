using AutoMapper;
using FluentAssertions;
using FluentValidation;
using Hapvida.ExternalIntegration.Application.Helpers;
using Hapvida.ExternalIntegration.Application.Interfaces;
using Hapvida.ExternalIntegration.Application.Queries.Cep.GetCep;
using Hapvida.ExternalIntegration.Application.Validators;
using Hapvida.ExternalIntegration.Domain.ValueObjects;
using Hapvida.ExternalIntegration.UnitTest;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Hapvida.ExternalIntegration.UnitTest.Application.Queries;

public class GetCepEdgeCasesTests : BaseTest
{
    private readonly Mock<ICepService> CepServiceMock;
    private readonly Mock<IValidator<GetCepRequest>> ValidatorMock;
    private readonly Mock<ILogger<GetCepHandler>> LoggerMock;
    private readonly IResponseBuilder ResponseBuilder;
    private readonly Mock<AutoMapper.IMapper> MapperMock;
    private readonly GetCepHandler Handler;

    public GetCepEdgeCasesTests()
    {
        CepServiceMock = new Mock<ICepService>();
        ValidatorMock = new Mock<IValidator<GetCepRequest>>();
        LoggerMock = new Mock<ILogger<GetCepHandler>>();
        ResponseBuilder = new ResponseBuilder();
        MapperMock = new Mock<AutoMapper.IMapper>();

        Handler = new GetCepHandler(
            CepServiceMock.Object,
            ValidatorMock.Object,
            LoggerMock.Object,
            ResponseBuilder,
            MapperMock.Object
        );
    }

    [Fact]
    public async Task Handle_Should_Normalize_Cep_With_Hyphen()
    {
        // Arrange
        var request = new GetCepRequest { ZipCode = "01306-001" };
        var zipCode = ZipCode.Create("01306001");
        var cepResult = new Hapvida.ExternalIntegration.Domain.Entities.CepResult
        {
            ZipCode = zipCode,
            City = "São Paulo",
            State = "SP",
            Provider = "brasilapi"
        };

        ValidatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        CepServiceMock.Setup(s => s.GetCepAsync(It.Is<ZipCode>(z => z.Value == "01306001"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cepResult);

        MapperMock.Setup(m => m.Map<Hapvida.ExternalIntegration.Application.Models.CepResponseDto>(cepResult))
            .Returns(new Hapvida.ExternalIntegration.Application.Models.CepResponseDto
            {
                ZipCode = "01306001",
                City = "São Paulo",
                State = "SP",
                Provider = "brasilapi"
            });

        // Act
        var response = await Handler.Handle(request, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.ZipCode.Should().Be("01306001"); // Normalizado
    }

    [Fact]
    public async Task Handle_Should_Handle_Empty_Provider_Response()
    {
        // Arrange
        var request = new GetCepRequest { ZipCode = "01306001" };
        var zipCode = ZipCode.Create("01306001");
        var cepResult = new Hapvida.ExternalIntegration.Domain.Entities.CepResult
        {
            ZipCode = zipCode,
            City = "São Paulo",
            State = "SP",
            Provider = "" // Provider vazio
        };

        ValidatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        CepServiceMock.Setup(s => s.GetCepAsync(It.IsAny<ZipCode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cepResult);

        MapperMock.Setup(m => m.Map<Hapvida.ExternalIntegration.Application.Models.CepResponseDto>(cepResult))
            .Returns(new Hapvida.ExternalIntegration.Application.Models.CepResponseDto
            {
                ZipCode = "01306001",
                City = "São Paulo",
                State = "SP",
                Provider = ""
            });

        // Act
        var response = await Handler.Handle(request, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
    }
}

