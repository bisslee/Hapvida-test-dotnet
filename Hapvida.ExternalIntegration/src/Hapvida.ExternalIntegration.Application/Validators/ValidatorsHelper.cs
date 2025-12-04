using FluentValidation;
using Hapvida.ExternalIntegration.Application.Helpers;
using System.Text.RegularExpressions;

namespace Hapvida.ExternalIntegration.Application.Validators;

public static class ValidatorsHelper
{
    public static bool IsZipCodeValid(string zipCode)
    {
        if (string.IsNullOrWhiteSpace(zipCode))
            return false;

        var normalized = zipCode.Replace("-", "").Replace(" ", "");
        return Regex.IsMatch(normalized, @"^\d{8}$");
    }

    public static IRuleBuilderOptions<T, string> ValidateZipCode<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .WithMessage(_ => ResourceHelper.GetResource(
                "REQUIRED_ZIPCODE", "CEP é obrigatório!"))
            .Must(IsZipCodeValid)
            .WithMessage(_ => ResourceHelper.GetResource(
                "INVALID_ZIPCODE", "CEP inválido. Deve conter 8 dígitos."));
    }
}

