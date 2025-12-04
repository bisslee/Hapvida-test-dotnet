using AutoMapper;
using FluentValidation;
using Hapvida.ExternalIntegration.Application.Helpers;
using Hapvida.ExternalIntegration.Application.Interfaces;
using Hapvida.ExternalIntegration.Application.Models;
using Hapvida.ExternalIntegration.Domain.Entities;
using Hapvida.ExternalIntegration.Domain.Repositories;
using Hapvida.ExternalIntegration.Domain.Resources;
using Hapvida.ExternalIntegration.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Hapvida.ExternalIntegration.Application.Commands.Cep.AddZipCodeLookup;

public class AddZipCodeLookupHandler : IRequestHandler<AddZipCodeLookupRequest, AddZipCodeLookupResponse>
{
    private readonly ILogger<AddZipCodeLookupHandler> Logger;
    private readonly IValidator<AddZipCodeLookupRequest> Validator;
    private readonly ICepService CepService;
    private readonly IWriteRepository<ZipCodeLookup> Repository;
    private readonly IReadRepository<ZipCodeLookup> ReadRepository;
    private readonly IResponseBuilder ResponseBuilder;
    private readonly IMapper Mapper;

    public AddZipCodeLookupHandler(
        ICepService cepService,
        IValidator<AddZipCodeLookupRequest> validator,
        ILogger<AddZipCodeLookupHandler> logger,
        IWriteRepository<ZipCodeLookup> repository,
        IReadRepository<ZipCodeLookup> readRepository,
        IResponseBuilder responseBuilder,
        IMapper mapper)
    {
        CepService = cepService;
        Validator = validator;
        Logger = logger;
        Repository = repository;
        ReadRepository = readRepository;
        ResponseBuilder = responseBuilder;
        Mapper = mapper;
    }

    public async Task<AddZipCodeLookupResponse> Handle(AddZipCodeLookupRequest request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Starting AddZipCodeLookupRequest processing for zipCode: {ZipCode}", request.ZipCode);

        var validationResult = await Validator.ValidateAsync(request, cancellationToken);
        if (validationResult != null && !validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage);
            Logger.LogWarning("Validation failed for AddZipCodeLookupRequest. ZipCode: {ZipCode}, Errors: {Errors}",
                request.ZipCode, string.Join(", ", errors));
            return ResponseBuilder.BuildValidationErrorResponse<AddZipCodeLookupResponse, ZipCodeLookupDto>(
                HapvidaExternalIntegrationResource.Cep_InvalidTitle, errors);
        }

        try
        {
            // Normaliza e valida CEP antes de chamar o serviço
            if (!ZipCode.TryCreate(request.ZipCode, out var normalizedZipCode))
            {
                Logger.LogWarning("Invalid CEP format received. ZipCode: {ZipCode}", request.ZipCode);
                return ResponseBuilder.BuildErrorResponse<AddZipCodeLookupResponse, ZipCodeLookupDto>(
                    string.Format(HapvidaExternalIntegrationResource.Cep_InvalidFormat, request.ZipCode),
                    new[] { string.Format(HapvidaExternalIntegrationResource.Cep_InvalidFormat, request.ZipCode) },
                    400);
            }

            // Verificar se já existe no banco
            var existingList = await ReadRepository.Find(x => x.ZipCode == normalizedZipCode!.Value);
            var existing = existingList.FirstOrDefault();
            if (existing != null)
            {
                Logger.LogWarning("ZipCode already exists in database. ZipCode: {ZipCode}", normalizedZipCode.Value);
                var existingDto = Mapper.Map<ZipCodeLookupDto>(existing);
                return ResponseBuilder.BuildErrorResponse<AddZipCodeLookupResponse, ZipCodeLookupDto>(
                    "CEP já está persistido no banco de dados",
                    new[] { "CEP já está persistido no banco de dados" },
                    409);
            }

            // Consultar CEP usando o serviço da US01
            Logger.LogDebug("Consulting CEP service. ZipCode: {ZipCode}", normalizedZipCode.Value);
            var cepResult = await CepService.GetCepAsync(normalizedZipCode, cancellationToken);

            if (cepResult == null)
            {
                Logger.LogWarning("CEP not found. ZipCode: {ZipCode}", normalizedZipCode.Value);
                return ResponseBuilder.BuildNotFoundResponse<AddZipCodeLookupResponse, ZipCodeLookupDto>(
                    string.Format(HapvidaExternalIntegrationResource.Cep_NotFound, request.ZipCode));
            }

            // Mapear CepResult para ZipCodeLookup entity
            var entity = new ZipCodeLookup
            {
                Id = Guid.NewGuid(),
                ZipCode = cepResult.ZipCode.Value,
                Street = cepResult.Street,
                District = cepResult.District,
                City = cepResult.City,
                State = cepResult.State,
                Ibge = cepResult.Ibge,
                Latitude = cepResult.Location?.Latitude,
                Longitude = cepResult.Location?.Longitude,
                Provider = cepResult.Provider,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };

            // Persistir no banco
            var result = await Repository.Add(entity);
            if (!result)
            {
                Logger.LogError("Failed to persist ZipCodeLookup. ZipCode: {ZipCode}", normalizedZipCode.Value);
                return ResponseBuilder.BuildErrorResponse<AddZipCodeLookupResponse, ZipCodeLookupDto>(
                    "Falha ao persistir CEP no banco de dados",
                    new[] { "Falha ao persistir CEP no banco de dados" },
                    500);
            }

            Logger.LogInformation("Successfully persisted ZipCodeLookup. ZipCode: {ZipCode}, Provider: {Provider}",
                entity.ZipCode, entity.Provider);

            var dto = Mapper.Map<ZipCodeLookupDto>(entity);
            return ResponseBuilder.BuildSuccessResponse<AddZipCodeLookupResponse, ZipCodeLookupDto>(dto, "CEP persistido com sucesso", 201);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error occurred while persisting ZipCodeLookup. ZipCode: {ZipCode}", request.ZipCode);
            throw; // Deixar o middleware global tratar a exceção
        }
    }
}

