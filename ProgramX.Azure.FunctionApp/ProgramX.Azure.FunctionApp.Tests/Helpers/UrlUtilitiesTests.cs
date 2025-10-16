using FluentAssertions;
using ProgramX.Azure.FunctionApp.Helpers;

namespace ProgramX.Azure.FunctionApp.Tests.Helpers;

[TestFixture]
public class UrlUtilitiesTests
{
    [TestCase("123", 123)]
    [TestCase("0", 0)]
    [TestCase("-1", null)]
    [TestCase("Invalid", null)]
    [TestCase("1.1", null)]
    public void GetValidIntegerQueryStringParameterOrNull_ValidInteger_ReturnsInteger(string input, int? expected)
    {
        // Act
        var result = UrlUtilities.GetValidIntegerQueryStringParameterOrNull(input);

        // Assert
        result.Should().Be(expected);
    }

    [TestCase("")]
    [TestCase("abc")]
    [TestCase("12.5")]
    [TestCase("123abc")]
    public void GetValidIntegerQueryStringParameterOrNull_InvalidInput_ReturnsNull(string input)
    {
        // Act
        var result = UrlUtilities.GetValidIntegerQueryStringParameterOrNull(input);

        // Assert
        result.Should().BeNull();
    }

    [TestCase("2147483647",2147483647)] // int.MaxValue
    [TestCase("-2147483648",null)] // int.MinValue
    public void GetValidIntegerQueryStringParameterOrNull_BoundaryValues_ReturnsCorrectValue(string input, int? expected)
    {
        // Act
        var result = UrlUtilities.GetValidIntegerQueryStringParameterOrNull(input);

        // Assert
        result.Should().Be(expected);
    }
}
