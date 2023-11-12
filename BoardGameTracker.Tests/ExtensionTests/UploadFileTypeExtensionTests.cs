using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Extensions;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.ExtensionTests;

public class UploadFileTypeExtensionTests
{
    [Theory]
    [InlineData(UploadFileType.Profile, "images\\profile")]
    [InlineData(null, "")]
    public void ConvertToPath_Should_Return_Correct_Path(UploadFileType input, string end)
    {
        var result = input.ConvertToPath();
        result.Should().EndWith(end);
    }
}