using FluentAssertions;
using FluentValidation.TestHelper;
using Hapvida.ExternalIntegration.Application.Queries.Weather.GetWeather;
using Hapvida.ExternalIntegration.UnitTest;
using Xunit;

namespace Hapvida.ExternalIntegration.UnitTest.Application.Validators;

public class GetWeatherValidatorTests : BaseTest
{
    private readonly GetWeatherValidator Validator;

    public GetWeatherValidatorTests()
    {
        Validator = new GetWeatherValidator();
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(3, true)]
    [InlineData(7, true)]
    [InlineData(0, false)] // Less than 1
    [InlineData(8, false)] // More than 7
    [InlineData(-1, false)] // Negative
    [InlineData(10, false)] // More than 7
    public void Validate_Days_Should_Return_Correct_Result(int days, bool expectedIsValid)
    {
        var request = new GetWeatherRequest { Days = days };
        var result = Validator.TestValidate(request);

        if (expectedIsValid)
        {
            result.ShouldNotHaveAnyValidationErrors();
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.Days);
        }
    }

    [Fact]
    public void Validate_Should_Pass_When_Days_Is_Default_Value()
    {
        // Arrange
        var request = new GetWeatherRequest(); // Days defaults to 3

        // Act
        var result = Validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}

