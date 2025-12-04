using AutoMapper;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Hapvida.ExternalIntegration.Application.Commands.Cep.AddZipCodeLookup;
using Hapvida.ExternalIntegration.Application.Helpers;
using Hapvida.ExternalIntegration.Application.Interfaces;
using Hapvida.ExternalIntegration.Application.Models;
using Hapvida.ExternalIntegration.Domain.Entities;
using Hapvida.ExternalIntegration.Domain.Repositories;
using Hapvida.ExternalIntegration.Domain.Resources;
using Hapvida.ExternalIntegration.Domain.ValueObjects;
using Hapvida.ExternalIntegration.UnitTest;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Hapvida.ExternalIntegration.UnitTest.Application.Commands;

public class AddZipCodeLookupTests : BaseTest
{
    private readonly Mock<ILogger<AddZipCodeLookupHandler>> LoggerMock;
    private readonly Mock<IValidator<AddZipCodeLookupRequest>> ValidatorMock;
    private readonly Mock<ICepService> CepServiceMock;
    private readonly Mock<IWriteRepository<ZipCodeLookup>> WriteRepositoryMock;
    private readonly Mock<IReadRepository<ZipCodeLookup>> ReadRepositoryMock;
    private readonly Mock<IResponseBuilder> ResponseBuilderMock;
    private readonly Mock<IMapper> MapperMock;
    private readonly AddZipCodeLookupHandler Handler;

    public AddZipCodeLookupTests()
    {
        LoggerMock = new Mock<ILogger<AddZipCodeLookupHandler>>();
        ValidatorMock = new Mock<IValidator<AddZipCodeLookupRequest>>();
        CepServiceMock = new Mock<ICepService>();
        WriteRepositoryMock = new Mock<IWriteRepository<ZipCodeLookup>>();
        ReadRepositoryMock = new Mock<IReadRepository<ZipCodeLookup>>();
        ResponseBuilderMock = new Mock<IResponseBuilder>();
        MapperMock = new Mock<IMapper>();

        Handler = new AddZipCodeLookupHandler(
            CepServiceMock.Object,
            ValidatorMock.Object,
            LoggerMock.Object,
            WriteRepositoryMock.Object,
            ReadRepositoryMock.Object,
            ResponseBuilderMock.Object,
            MapperMock.Object
        );
    }

    private AddZipCodeLookupRequest CreateValidRequest() => new() { ZipCode = "01306001" };

    [Fact]
    public async Task Handle_Should_Persist_Cep_Successfully()
    {
        // Arrange
        var request = CreateValidRequest();
        var zipCode = ZipCode.Create(request.ZipCode);
        var cepResult = new CepResult
        {
            ZipCode = zipCode,
            City = "São Paulo",
            State = "SP",
            Street = "Rua Teste",
            District = "Bela Vista",
            Provider = "brasilapi"
        };

        var entity = new ZipCodeLookup
        {
            Id = Guid.NewGuid(),
            ZipCode = zipCode.Value,
            City = "São Paulo",
            State = "SP",
            Street = "Rua Teste",
            District = "Bela Vista",
            Provider = "brasilapi",
            CreatedAt = DateTime.UtcNow
        };

        var dto = new ZipCodeLookupDto
        {
            Id = entity.Id,
            ZipCode = entity.ZipCode,
            City = entity.City,
            State = entity.State,
            Provider = entity.Provider
        };

        var successResponse = new AddZipCodeLookupResponse
        {
            Data = dto,
            StatusCode = 201,
            Success = true,
            Message = "CEP persistido com sucesso"
        };

        ValidatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        ReadRepositoryMock.Setup(r => r.Find(It.IsAny<System.Linq.Expressions.Expression<Func<ZipCodeLookup, bool>>>()))
            .ReturnsAsync(new List<ZipCodeLookup>());
        CepServiceMock.Setup(s => s.GetCepAsync(It.IsAny<ZipCode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cepResult);
        WriteRepositoryMock.Setup(r => r.Add(It.IsAny<ZipCodeLookup>()))
            .ReturnsAsync(true);
        MapperMock.Setup(m => m.Map<ZipCodeLookupDto>(It.IsAny<ZipCodeLookup>()))
            .Returns(dto);
        ResponseBuilderMock.Setup(b => b.BuildSuccessResponse<AddZipCodeLookupResponse, ZipCodeLookupDto>(
            dto, "CEP persistido com sucesso", 201))
            .Returns(successResponse);

        // Act
        var response = await Handler.Handle(request, CancellationToken.None);

        // Assert
        response.Should().Be(successResponse);
        response.Success.Should().BeTrue();
        response.StatusCode.Should().Be(201);
        response.Data.Should().NotBeNull();
        response.Data!.ZipCode.Should().Be(request.ZipCode);
        WriteRepositoryMock.Verify(r => r.Add(It.IsAny<ZipCodeLookup>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Return_409_When_Cep_Already_Exists()
    {
        // Arrange
        var request = CreateValidRequest();
        var existingEntity = new ZipCodeLookup
        {
            Id = Guid.NewGuid(),
            ZipCode = request.ZipCode,
            City = "São Paulo",
            State = "SP"
        };

        var existingDto = new ZipCodeLookupDto
        {
            Id = existingEntity.Id,
            ZipCode = existingEntity.ZipCode
        };

        var conflictResponse = new AddZipCodeLookupResponse
        {
            Data = existingDto,
            StatusCode = 409,
            Success = false,
            Message = "CEP já está persistido no banco de dados",
            Errors = new List<string> { "CEP já está persistido no banco de dados" }
        };

        ValidatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        ReadRepositoryMock.Setup(r => r.Find(It.IsAny<System.Linq.Expressions.Expression<Func<ZipCodeLookup, bool>>>()))
            .ReturnsAsync(new List<ZipCodeLookup> { existingEntity });
        MapperMock.Setup(m => m.Map<ZipCodeLookupDto>(existingEntity))
            .Returns(existingDto);
        ResponseBuilderMock.Setup(b => b.BuildErrorResponse<AddZipCodeLookupResponse, ZipCodeLookupDto>(
            "CEP já está persistido no banco de dados",
            It.IsAny<IEnumerable<string>>(),
            409))
            .Returns(conflictResponse);

        // Act
        var response = await Handler.Handle(request, CancellationToken.None);

        // Assert
        response.Should().Be(conflictResponse);
        response.Success.Should().BeFalse();
        response.StatusCode.Should().Be(409);
        WriteRepositoryMock.Verify(r => r.Add(It.IsAny<ZipCodeLookup>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Return_404_When_Cep_Not_Found()
    {
        // Arrange
        var request = CreateValidRequest();
        var notFoundResponse = new AddZipCodeLookupResponse
        {
            Data = null,
            StatusCode = 404,
            Success = false,
            Message = string.Format(HapvidaExternalIntegrationResource.Cep_NotFound, request.ZipCode)
        };

        ValidatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        ReadRepositoryMock.Setup(r => r.Find(It.IsAny<System.Linq.Expressions.Expression<Func<ZipCodeLookup, bool>>>()))
            .ReturnsAsync(new List<ZipCodeLookup>());
        CepServiceMock.Setup(s => s.GetCepAsync(It.IsAny<ZipCode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CepResult?)null);
        ResponseBuilderMock.Setup(b => b.BuildNotFoundResponse<AddZipCodeLookupResponse, ZipCodeLookupDto>(
            string.Format(HapvidaExternalIntegrationResource.Cep_NotFound, request.ZipCode)))
            .Returns(notFoundResponse);

        // Act
        var response = await Handler.Handle(request, CancellationToken.None);

        // Assert
        response.Should().Be(notFoundResponse);
        response.Success.Should().BeFalse();
        response.StatusCode.Should().Be(404);
        response.Data.Should().BeNull();
        WriteRepositoryMock.Verify(r => r.Add(It.IsAny<ZipCodeLookup>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Return_400_When_Validation_Fails()
    {
        // Arrange
        var request = CreateValidRequest();
        var validationFailures = new List<ValidationFailure> { new("ZipCode", "CEP inválido") };
        var validationResult = new ValidationResult(validationFailures);
        var errorResponse = new AddZipCodeLookupResponse
        {
            Data = null,
            StatusCode = 400,
            Success = false,
            Message = HapvidaExternalIntegrationResource.Cep_InvalidTitle,
            Errors = validationFailures.Select(e => e.ErrorMessage).ToList()
        };

        ValidatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        ResponseBuilderMock.Setup(b => b.BuildValidationErrorResponse<AddZipCodeLookupResponse, ZipCodeLookupDto>(
            HapvidaExternalIntegrationResource.Cep_InvalidTitle, It.IsAny<IEnumerable<string>>()))
            .Returns(errorResponse);

        // Act
        var response = await Handler.Handle(request, CancellationToken.None);

        // Assert
        response.Should().Be(errorResponse);
        response.Success.Should().BeFalse();
        response.StatusCode.Should().Be(400);
        response.Errors.Should().Contain("CEP inválido");
        WriteRepositoryMock.Verify(r => r.Add(It.IsAny<ZipCodeLookup>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Return_500_When_Persistence_Fails()
    {
        // Arrange
        var request = CreateValidRequest();
        var zipCode = ZipCode.Create(request.ZipCode);
        var cepResult = new CepResult
        {
            ZipCode = zipCode,
            City = "São Paulo",
            State = "SP",
            Provider = "brasilapi"
        };

        var errorResponse = new AddZipCodeLookupResponse
        {
            Data = null,
            StatusCode = 500,
            Success = false,
            Message = "Falha ao persistir CEP no banco de dados",
            Errors = new List<string> { "Falha ao persistir CEP no banco de dados" }
        };

        ValidatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        ReadRepositoryMock.Setup(r => r.Find(It.IsAny<System.Linq.Expressions.Expression<Func<ZipCodeLookup, bool>>>()))
            .ReturnsAsync(new List<ZipCodeLookup>());
        CepServiceMock.Setup(s => s.GetCepAsync(It.IsAny<ZipCode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cepResult);
        WriteRepositoryMock.Setup(r => r.Add(It.IsAny<ZipCodeLookup>()))
            .ReturnsAsync(false);
        ResponseBuilderMock.Setup(b => b.BuildErrorResponse<AddZipCodeLookupResponse, ZipCodeLookupDto>(
            "Falha ao persistir CEP no banco de dados",
            It.IsAny<IEnumerable<string>>(),
            500))
            .Returns(errorResponse);

        // Act
        var response = await Handler.Handle(request, CancellationToken.None);

        // Assert
        response.Should().Be(errorResponse);
        response.Success.Should().BeFalse();
        response.StatusCode.Should().Be(500);
        WriteRepositoryMock.Verify(r => r.Add(It.IsAny<ZipCodeLookup>()), Times.Once);
    }
}

