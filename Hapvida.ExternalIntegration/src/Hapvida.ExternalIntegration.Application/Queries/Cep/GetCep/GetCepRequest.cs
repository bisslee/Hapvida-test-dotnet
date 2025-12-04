using MediatR;

namespace Hapvida.ExternalIntegration.Application.Queries.Cep.GetCep;

public class GetCepRequest : IRequest<GetCepResponse>
{
    public string ZipCode { get; set; } = string.Empty;
}

