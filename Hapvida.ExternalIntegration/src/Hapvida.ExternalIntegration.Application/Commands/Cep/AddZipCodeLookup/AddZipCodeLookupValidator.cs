using FluentValidation;
using Hapvida.ExternalIntegration.Application.Validators;

namespace Hapvida.ExternalIntegration.Application.Commands.Cep.AddZipCodeLookup;

public class AddZipCodeLookupValidator : AbstractValidator<AddZipCodeLookupRequest>
{
    public AddZipCodeLookupValidator()
    {
        RuleFor(x => x.ZipCode)
            .ValidateZipCode();
    }
}

