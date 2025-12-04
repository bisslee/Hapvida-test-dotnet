using AutoMapper;
using FluentValidation;
using Hapvida.ExternalIntegration.Application.Helpers;
using Hapvida.ExternalIntegration.Application.Interfaces;
using Hapvida.ExternalIntegration.Application.Models;
using Hapvida.ExternalIntegration.Domain.Resources;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Hapvida.ExternalIntegration.Application.Queries.Cep.GetCep;

public class GetCepHandler : IRequestHandler<GetCepRequest, GetCepResponse>
{
    private readonly ILogger<GetCepHandler> Logger;
    private readonly IValidator<GetCepRequest> Validator;
    private readonly ICepService CepService;
    private readonly IResponseBuilder ResponseBuilder;
    private readonly IMapper Mapper;

    public GetCepHandler(
        ICepService cepService,
        IValidator<GetCepRequest> validator,
        ILogger<GetCepHandler> logger,
        IResponseBuilder responseBuilder,
        IMapper mapper)
    {
        CepService = cepService;
        Validator = validator;
        Logger = logger;
        ResponseBuilder = responseBuilder;
        Mapper = mapper;
    }

    public async Task<GetCepResponse> Handle(GetCepRequest request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Starting GetCepRequest processing for zipCode: {ZipCode}", request.ZipCode);

        var validationResult = await Validator.ValidateAsync(request, cancellationToken);
        if (validationResult != null && !validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage);
            Logger.LogWarning("Validation failed for GetCepRequest. ZipCode: {ZipCode}, Errors: {Errors}",
                request.ZipCode, string.Join(", ", errors));
            return ResponseBuilder.BuildValidationErrorResponse<GetCepResponse, CepResponseDto>(
                HapvidaExternalIntegrationResource.Cep_InvalidTitle, errors);
        }

        try
        {
            // Normaliza e valida CEP antes de chamar o serviço
            if (!Domain.ValueObjects.ZipCode.TryCreate(request.ZipCode, out var normalizedZipCode))
            {
                Logger.LogWarning("Invalid CEP format received. ZipCode: {ZipCode}", request.ZipCode);
                return ResponseBuilder.BuildErrorResponse<GetCepResponse, CepResponseDto>(
                    string.Format(HapvidaExternalIntegrationResource.Cep_InvalidFormat, request.ZipCode),
                    new[] { string.Format(HapvidaExternalIntegrationResource.Cep_InvalidFormat, request.ZipCode) },
                    400);
            }

            Logger.LogDebug("Consulting CEP service. ZipCode: {ZipCode}", normalizedZipCode!.Value);
            var result = await CepService.GetCepAsync(normalizedZipCode, cancellationToken);

            if (result == null)
            {
                Logger.LogWarning("CEP not found. ZipCode: {ZipCode}", normalizedZipCode.Value);
                return ResponseBuilder.BuildNotFoundResponse<GetCepResponse, CepResponseDto>(
                    string.Format(HapvidaExternalIntegrationResource.Cep_NotFound, request.ZipCode));
            }

            Logger.LogInformation("Successfully retrieved CEP. ZipCode: {ZipCode}, Provider: {Provider}",
                result.ZipCode.Value, result.Provider);

            var cepDto = Mapper.Map<CepResponseDto>(result);
            return ResponseBuilder.BuildSuccessResponse<GetCepResponse, CepResponseDto>(cepDto);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error occurred while retrieving CEP. ZipCode: {ZipCode}", request.ZipCode);
            throw; // Deixar o middleware global tratar a exceção
        }
    }
}

