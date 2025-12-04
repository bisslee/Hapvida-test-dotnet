using FluentAssertions;
using Hapvida.ExternalIntegration.Application.Queries.Weather.GetWeather;
using Hapvida.ExternalIntegration.Application.Validators;
using Hapvida.ExternalIntegration.UnitTest;
using Xunit;

namespace Hapvida.ExternalIntegration.UnitTest.Application.Validators;

public class GetWeatherValidatorEdgeCasesTests : BaseTest
{
    private readonly GetWeatherValidator Validator;

    public GetWeatherValidatorEdgeCasesTests()
    {
        Validator = new GetWeatherValidator();
    }

    [Theory]
    [InlineData(0, false)] // Menor que mínimo
    [InlineData(1, true)] // Mínimo válido
    [InlineData(3, true)] // Valor padrão
    [InlineData(7, true)] // Máximo válido
    [InlineData(8, false)] // Maior que máximo
    [InlineData(-1, false)] // Negativo
    [InlineData(100, false)] // Muito maior que máximo
    public void Validate_Days_Should_Return_Correct_Result(int days, bool expectedIsValid)
    {
        // Arrange
        var request = new GetWeatherRequest { Days = days };
        var result = Validator.Validate(request);

        // Assert
        result.IsValid.Should().Be(expectedIsValid);

        if (!expectedIsValid)
        {
            result.Errors.Should().NotBeEmpty();
            result.Errors.Should().Contain(e => 
                e.ErrorMessage.Contains("days", StringComparison.OrdinalIgnoreCase) ||
                e.ErrorMessage.Contains("entre", StringComparison.OrdinalIgnoreCase) ||
                e.ErrorMessage.Contains("between", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Validate_Should_Use_Default_Days_Value()
    {
        // Arrange
        // Days tem valor padrão de 3 no GetWeatherRequest
        var request = new GetWeatherRequest(); // Usa valor padrão
        var result = Validator.Validate(request);

        // Assert
        // O valor padrão de 3 deve ser válido
        result.IsValid.Should().BeTrue();
        request.Days.Should().Be(3);
    }
}

