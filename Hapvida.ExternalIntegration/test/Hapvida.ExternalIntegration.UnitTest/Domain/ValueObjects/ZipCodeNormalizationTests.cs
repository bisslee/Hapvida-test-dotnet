using FluentAssertions;
using Hapvida.ExternalIntegration.Domain.ValueObjects;
using Hapvida.ExternalIntegration.UnitTest;
using Xunit;

namespace Hapvida.ExternalIntegration.UnitTest.Domain.ValueObjects;

public class ZipCodeNormalizationTests : BaseTest
{
    [Theory]
    [InlineData("01001000", "01001000")] // Sem hífen
    [InlineData("01001-000", "01001000")] // Com hífen
    [InlineData("01306-001", "01306001")] // Com hífen
    [InlineData("01306001", "01306001")] // Sem hífen
    [InlineData("01001 000", "01001000")] // Com espaço
    [InlineData("01001- 000", "01001000")] // Com hífen e espaço
    public void Create_Should_Normalize_ZipCode_With_Hyphen_Or_Spaces(string input, string expected)
    {
        // Act
        var zipCode = ZipCode.Create(input);

        // Assert
        zipCode.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("01001000")]
    [InlineData("01306001")]
    [InlineData("12345678")]
    public void Create_Should_Accept_Valid_ZipCode(string input)
    {
        // Act
        var zipCode = ZipCode.Create(input);

        // Assert
        zipCode.Value.Should().Be(input);
        zipCode.Value.Should().HaveLength(8);
    }

    [Theory]
    [InlineData("01001000", true)]
    [InlineData("01001-000", true)]
    [InlineData("01306-001", true)]
    [InlineData("01001 000", true)]
    [InlineData("", false)]
    [InlineData("123", false)] // Menos de 8 dígitos
    [InlineData("123456789", false)] // Mais de 8 dígitos
    [InlineData("12345-67", false)] // Com hífen mas menos de 8 dígitos
    [InlineData("12345-6789", false)] // Com hífen mas mais de 8 dígitos
    [InlineData("12345ABC", false)] // Contém letras
    [InlineData("12345-AB", false)] // Contém letras com hífen
    [InlineData(null, false)]
    public void TryCreate_Should_Return_Correct_Result(string? input, bool expected)
    {
        // Act
        var result = ZipCode.TryCreate(input ?? string.Empty, out var zipCode);

        // Assert
        result.Should().Be(expected);
        if (expected)
        {
            zipCode.Should().NotBeNull();
            zipCode!.Value.Should().HaveLength(8);
            zipCode.Value.Should().MatchRegex(@"^\d{8}$");
        }
        else
        {
            zipCode.Should().BeNull();
        }
    }

    [Fact]
    public void Create_Should_Throw_When_ZipCode_Is_Empty()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => ZipCode.Create(""));
        exception.ParamName.Should().Be("zipCode");
    }

    [Fact]
    public void Create_Should_Throw_When_ZipCode_Is_Null()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => ZipCode.Create(null!));
        exception.ParamName.Should().Be("zipCode");
    }

    [Fact]
    public void Create_Should_Throw_When_ZipCode_Has_Invalid_Length()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => ZipCode.Create("123"));
        exception.ParamName.Should().Be("zipCode");
    }

    [Fact]
    public void Create_Should_Throw_When_ZipCode_Contains_Non_Digits()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => ZipCode.Create("12345ABC"));
        exception.ParamName.Should().Be("zipCode");
    }

    [Fact]
    public void ToString_Should_Return_Normalized_Value()
    {
        // Arrange
        var zipCode = ZipCode.Create("01001-000");

        // Act
        var result = zipCode.ToString();

        // Assert
        result.Should().Be("01001000");
    }
}

