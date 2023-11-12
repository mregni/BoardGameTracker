using BoardGameTracker.Common;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Extensions;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.ExtensionTests;

public class StringExtensionTests
{
    [Theory]
    [InlineData(Constants.Bgg.Artist, PersonType.Artist)]
    [InlineData(Constants.Bgg.Designer, PersonType.Designer)]
    [InlineData("sdfsdf", PersonType.Publisher)]
    public void StringToPersonTypeEnum_Should_Map_String(string input, PersonType output)
    {
        var result = input.ToPersonTypeEnum();
        result.Should().Be(output);
    }

    [Fact]
    public void GenerateUniqueFileName_Should_Generate_FileName()
    {
        var inputName = "file.jpg";
        var result = inputName.GenerateUniqueFileName();

        result.Should().NotBeEmpty();
        result.Should().StartWith("file_");
        result.Should().EndWith(".jpg");
    }

    [Theory]
    [InlineData("input", "Input")]
    [InlineData("sdfsdf", "Sdfsdf")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void FirstCharToUpper_Should_Move_First_Char_To_UpperCase(string? input, string output)
    {
        var result = input.FirstCharToUpper();
        result.Should().Be(output);
    }
} 