using FluentAssertions;
using Hapvida.ExternalIntegration.Application.Commands.Cep.AddZipCodeLookup;
using Hapvida.ExternalIntegration.Domain.Resources;
using System;
using Xunit;

namespace Hapvida.ExternalIntegration.UnitTest.Application.Validators;

public class AddZipCodeLookupValidatorTests : BaseTest
{
    private readonly AddZipCodeLookupValidator Validator;

    public AddZipCodeLookupValidatorTests()
    {
        Validator = new AddZipCodeLookupValidator();
    }

    [Theory]
    [InlineData("01306001", true)]
    [InlineData("01306-001", true)]
    [InlineData("12345678", true)]
    [InlineData("12345-678", true)]
    [InlineData("0130600", false)] // Less than 8 digits
    [InlineData("013060011", false)] // More than 8 digits
    [InlineData("01306-00A", false)] // Contains non-digits
    [InlineData("", false)] // Empty
    [InlineData(null, false)] // Null
    [InlineData("        ", false)] // Whitespace
    public void Validate_ZipCode_Should_Return_Correct_Result(string zipCode, bool expectedIsValid)
    {
        var request = new AddZipCodeLookupRequest { ZipCode = zipCode ?? string.Empty };
        var result = Validator.Validate(request);

        result.IsValid.Should().Be(expectedIsValid);

        if (!expectedIsValid)
        {
            result.Errors.Should().NotBeEmpty();
            if (string.IsNullOrWhiteSpace(zipCode))
            {
                result.Errors.Should().Contain(e => e.ErrorMessage.Contains("obrigatório") || e.ErrorMessage.Contains("required"));
            }
            else
            {
                // A mensagem pode estar em inglês ou português, verificar case-insensitive
                result.Errors.Should().Contain(e => 
                    e.ErrorMessage.Contains("inválido", StringComparison.OrdinalIgnoreCase) || 
                    e.ErrorMessage.Contains("invalid", StringComparison.OrdinalIgnoreCase) ||
                    e.ErrorMessage.Contains("Invalid", StringComparison.OrdinalIgnoreCase));
            }
        }
        else
        {
            result.Errors.Should().BeEmpty();
        }
    }
}

