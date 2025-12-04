using Hapvida.ExternalIntegration.Domain.Entities;
using Hapvida.ExternalIntegration.Domain.ValueObjects;

namespace Hapvida.ExternalIntegration.Application.Interfaces;

public interface ICepProvider
{
    Task<CepResult?> GetCepAsync(ZipCode zipCode, CancellationToken cancellationToken = default);
}

