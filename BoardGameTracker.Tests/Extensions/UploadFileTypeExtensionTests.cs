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
    public static IEnumerable<object[]> PathCases()
    {
        yield return new object[] { UploadFileType.Profile, PathHelper.FullProfileImagePath };
        yield return new object[] { UploadFileType.Game, string.Empty };
        yield return new object[] { (UploadFileType) 999, string.Empty };
    }

    [Theory]
    [MemberData(nameof(PathCases))]
    public void ConvertToPath_ShouldReturnExpectedPath(UploadFileType type, string expected)
    {
        type.ConvertToPath().Should().Be(expected);
    }

    [Fact]
    public void ConvertToPath_ShouldHandleAllDefinedEnumValues()
    {
        foreach (var enumValue in Enum.GetValues<UploadFileType>())
        {
            var expected = enumValue == UploadFileType.Profile ? PathHelper.FullProfileImagePath : string.Empty;
            enumValue.ConvertToPath().Should().Be(expected);
        }
    }
}
