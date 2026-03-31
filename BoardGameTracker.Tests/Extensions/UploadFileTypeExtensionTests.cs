using System;
using System.Collections.Generic;
using System.Linq;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Helpers;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Extensions;

public class UploadFileTypeExtensionTests
{
    [Theory]
    [InlineData(UploadFileType.Game)]
    public void ConvertToPath_ShouldReturnEmptyString_WhenTypeIsNotProfile(UploadFileType type)
    {
        var result = type.ConvertToPath();

        result.Should().Be(string.Empty);
    }

    [Fact]
    public void ConvertToPath_ShouldReturnEmptyString_WhenTypeIsUndefinedEnumValue()
    {
        const UploadFileType type = (UploadFileType) 999;

        var result = type.ConvertToPath();

        result.Should().Be(string.Empty);
    }

    [Fact]
    public void ConvertToPath_ShouldBeConsistent_WhenCalledMultipleTimes()
    {
        const UploadFileType type = UploadFileType.Profile;

        var result1 = type.ConvertToPath();
        var result2 = type.ConvertToPath();
        var result3 = type.ConvertToPath();

        result1.Should().Be(PathHelper.FullProfileImagePath);
        result2.Should().Be(PathHelper.FullProfileImagePath);
        result3.Should().Be(PathHelper.FullProfileImagePath);
        result1.Should().Be(result2);
        result2.Should().Be(result3);
    }

    [Fact]
    public void ConvertToPath_ShouldHandleAllDefinedEnumValues()
    {
        var allEnumValues = Enum.GetValues<UploadFileType>();
        var results = new Dictionary<UploadFileType, string>();

        foreach (var enumValue in allEnumValues)
        {
            results[enumValue] = enumValue.ConvertToPath();
        }

        results[UploadFileType.Profile].Should().Be(PathHelper.FullProfileImagePath);

        foreach (var kvp in results.Where(x => x.Key != UploadFileType.Profile))
        {
            kvp.Value.Should().Be(string.Empty, $"because {kvp.Key} should return empty string");
        }
    }
}