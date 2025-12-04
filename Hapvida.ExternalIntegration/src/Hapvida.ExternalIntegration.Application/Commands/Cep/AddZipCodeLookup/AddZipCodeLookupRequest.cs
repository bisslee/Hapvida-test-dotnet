using MediatR;

namespace Hapvida.ExternalIntegration.Application.Commands.Cep.AddZipCodeLookup;

public class AddZipCodeLookupRequest : IRequest<AddZipCodeLookupResponse>
{
    public string ZipCode { get; set; } = string.Empty;
}

