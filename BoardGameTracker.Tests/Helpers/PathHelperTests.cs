using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.Helpers;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Helpers;

public class PathHelperTests
{
    private static readonly string CurrentDir = Directory.GetCurrentDirectory();

    [Fact]
    public void Paths_ShouldLiveUnderTheWorkingDirectory()
    {
        PathHelper.CoverImagePath.Should().Be(Path.Combine("images", "cover"));
        PathHelper.ProfileImagePath.Should().Be(Path.Combine("images", "profile"));
        PathHelper.FullRootImagePath.Should().Be(Path.Combine(CurrentDir, "images"));
        PathHelper.FullCoverImagePath.Should().Be(Path.Combine(CurrentDir, "images", "cover"));
        PathHelper.FullProfileImagePath.Should().Be(Path.Combine(CurrentDir, "images", "profile"));
        PathHelper.FullManualsPath.Should().Be(Path.Combine(CurrentDir, "manuals"));
        PathHelper.FullManualFiguresPath.Should().Be(Path.Combine(CurrentDir, "manuals", "figures"));
    }

    [Theory]
    [InlineData("/images/cover/a.png")]
    [InlineData("images/cover/a.png")]
    [InlineData("//images/cover/a.png")]
    [InlineData("/images/profile/../cover/a.png")]
    public void MapImageWebPathToPhysical_ShouldMapAPathInsideTheImageFolder(string webPath)
    {
        PathHelper.MapImageWebPathToPhysical(webPath).Should().Be(Path.Combine(CurrentDir, "images", "cover", "a.png"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/images")]
    [InlineData("/images/../secrets.txt")]
    [InlineData("/images/../../secrets.txt")]
    [InlineData("/imagesother/a.png")]
    [InlineData("/manuals/a.pdf")]
    public void MapImageWebPathToPhysical_ShouldRefuseAnythingOutsideTheImageFolder(string webPath)
    {
        PathHelper.MapImageWebPathToPhysical(webPath).Should().BeNull();
    }

    [Theory]
    [InlineData("a.pdf")]
    [InlineData("figures/../a.pdf")]
    public void MapManualFileToPhysical_ShouldMapAFileInsideTheManualFolder(string storedFileName)
    {
        PathHelper.MapManualFileToPhysical(storedFileName).Should().Be(Path.Combine(CurrentDir, "manuals", "a.pdf"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("../secrets.txt")]
    [InlineData("figures/../../secrets.txt")]
    [InlineData("../manualsother/a.pdf")]
    public void MapManualFileToPhysical_ShouldRefuseAnythingOutsideTheManualFolder(string storedFileName)
    {
        var act = () => PathHelper.MapManualFileToPhysical(storedFileName);

        var thrown = act.Should().Throw<EntityNotFoundException>().Which;
        thrown.EntityType.Should().Be("Manual");
        thrown.EntityId.Should().Be(storedFileName);
    }

    [Fact]
    public void MapManualFileToPhysical_ShouldRefuseAnAbsolutePath()
    {
        var absolute = Path.Combine(CurrentDir, "secrets.txt");

        var act = () => PathHelper.MapManualFileToPhysical(absolute);

        act.Should().Throw<EntityNotFoundException>();
    }
}
