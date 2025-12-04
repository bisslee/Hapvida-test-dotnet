using FluentValidation;
using Hapvida.ExternalIntegration.Application.Validators;

namespace Hapvida.ExternalIntegration.Application.Queries.Cep.GetCep;

public class GetCepValidator : AbstractValidator<GetCepRequest>
{
    public GetCepValidator()
    {
        RuleFor(x => x.ZipCode)
            .ValidateZipCode();
    }
}

