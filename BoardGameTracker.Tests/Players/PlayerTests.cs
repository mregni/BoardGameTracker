using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Players;

public class PlayerTests
{
    [Fact]
    public void Constructor_ShouldStoreTheGivenValues()
    {
        var player = new Player("Jane", "jane.png", "jane@example.com");

        player.Name.Should().Be("Jane");
        player.Image.Should().Be("jane.png");
        player.ImageSource.Should().Be(PlayerImageSource.Upload);
        player.Email.Should().Be("jane@example.com");
        player.PlayerSessions.Should().BeEmpty();
        player.Badges.Should().BeEmpty();
        player.PlayerBadges.Should().BeEmpty();
        player.Loans.Should().BeEmpty();
        player.GameNightRsvps.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Constructor_ShouldStartWithoutImage_WhenNoImageIsGiven(string? image)
    {
        var player = new Player("Jane", image);

        player.Image.Should().BeNull();
        player.ImageSource.Should().Be(PlayerImageSource.None);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenTheNameIsBlank(string? name)
    {
        var act = () => new Player(name!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateName_ShouldStoreTheNewName()
    {
        var player = new Player("Jane");

        player.UpdateName("Janet");

        player.Name.Should().Be("Janet");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateName_ShouldThrow_AndKeepTheName_WhenTheNewNameIsBlank(string? name)
    {
        var player = new Player("Jane");

        var act = () => player.UpdateName(name!);

        act.Should().Throw<ArgumentException>();
        player.Name.Should().Be("Jane");
    }

    [Fact]
    public void UpdateImage_ShouldMarkTheImageAsUploaded()
    {
        var player = new Player("Jane");
        player.UseAccountImage("account.png");

        player.UpdateImage("own.png");

        player.Image.Should().Be("own.png");
        player.ImageSource.Should().Be(PlayerImageSource.Upload);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void UpdateImage_ShouldClearTheImage_WhenTheNewImageIsEmpty(string? image)
    {
        var player = new Player("Jane", "own.png");

        player.UpdateImage(image);

        player.Image.Should().BeNull();
        player.ImageSource.Should().Be(PlayerImageSource.None);
    }

    [Fact]
    public void FollowsAccountImage_ShouldBeTrue_WithoutAnImage()
    {
        new Player("Jane").FollowsAccountImage.Should().BeTrue();
    }

    [Fact]
    public void FollowsAccountImage_ShouldBeFalse_WithAnUploadedImage()
    {
        new Player("Jane", "own.png").FollowsAccountImage.Should().BeFalse();
    }

    [Fact]
    public void UseAccountImage_ShouldMarkTheImageAsComingFromTheAccount()
    {
        var player = new Player("Jane", "own.png");

        player.UseAccountImage("account.png");

        player.Image.Should().Be("account.png");
        player.ImageSource.Should().Be(PlayerImageSource.Account);
        player.FollowsAccountImage.Should().BeTrue();
    }

    [Fact]
    public void ClearAccountImage_ShouldRemoveAnAccountImage()
    {
        var player = new Player("Jane");
        player.UseAccountImage("account.png");

        player.ClearAccountImage();

        player.Image.Should().BeNull();
        player.ImageSource.Should().Be(PlayerImageSource.None);
    }

    [Fact]
    public void ClearAccountImage_ShouldKeepAnUploadedImage()
    {
        var player = new Player("Jane", "own.png");

        player.ClearAccountImage();

        player.Image.Should().Be("own.png");
        player.ImageSource.Should().Be(PlayerImageSource.Upload);
    }

    [Theory]
    [InlineData("new@example.com")]
    [InlineData(null)]
    public void UpdateEmail_ShouldStoreTheNewEmail(string? email)
    {
        var player = new Player("Jane", null, "old@example.com");

        player.UpdateEmail(email);

        player.Email.Should().Be(email);
    }
}
