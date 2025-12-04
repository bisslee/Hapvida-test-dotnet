using FluentAssertions;
using Hapvida.ExternalIntegration.Application.Queries.Cep.GetCep;
using Hapvida.ExternalIntegration.UnitTest;
using FluentValidation.TestHelper;

namespace Hapvida.ExternalIntegration.UnitTest.Application.Validators;

public class GetCepValidatorTests : BaseTest
{
    private readonly GetCepValidator Validator;

    public GetCepValidatorTests()
    {
        Validator = new GetCepValidator();
    }

    [Fact]
    public void Validate_Should_Pass_When_ZipCode_Is_Valid_With_8_Digits()
    {
        // Arrange
        var request = new GetCepRequest { ZipCode = "01306001" };

        // Act
        var result = Validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Pass_When_ZipCode_Is_Valid_With_Hyphen()
    {
        // Arrange
        var request = new GetCepRequest { ZipCode = "01306-001" };

        // Act
        var result = Validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_When_ZipCode_Is_Empty()
    {
        // Arrange
        var request = new GetCepRequest { ZipCode = string.Empty };

        // Act
        var result = Validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ZipCode);
    }

    [Fact]
    public void Validate_Should_Fail_When_ZipCode_Is_Null()
    {
        // Arrange
        var request = new GetCepRequest { ZipCode = null! };

        // Act
        var result = Validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ZipCode);
    }

    [Fact]
    public void Validate_Should_Fail_When_ZipCode_Has_Less_Than_8_Digits()
    {
        // Arrange
        var request = new GetCepRequest { ZipCode = "1234567" };

        // Act
        var result = Validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ZipCode);
    }

    [Fact]
    public void Validate_Should_Fail_When_ZipCode_Has_More_Than_8_Digits()
    {
        // Arrange
        var request = new GetCepRequest { ZipCode = "123456789" };

        // Act
        var result = Validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ZipCode);
    }

    [Fact]
    public void Validate_Should_Fail_When_ZipCode_Contains_Letters()
    {
        // Arrange
        var request = new GetCepRequest { ZipCode = "01306ABC" };

        // Act
        var result = Validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ZipCode);
    }

    [Fact]
    public void Validate_Should_Fail_When_ZipCode_Contains_Special_Characters()
    {
        // Arrange
        var request = new GetCepRequest { ZipCode = "01306@01" };

        // Act
        var result = Validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ZipCode);
    }

    [Fact]
    public void Validate_Should_Pass_When_ZipCode_Has_Spaces_But_Valid_After_Normalization()
    {
        // Arrange
        var request = new GetCepRequest { ZipCode = "01306 001" };

        // Act
        var result = Validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}

