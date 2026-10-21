using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Helpers;
using BoardGameTracker.IntegrationTests.Infrastructure;
using FluentAssertions;
using SkiaSharp;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Api;

[Collection(IntegrationCollection.Name)]
public class PlayerAccountImageTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fixture;
    private readonly List<int> _players = [];
    private HttpClient _admin = null!;
    private HttpClient _user = null!;

    public PlayerAccountImageTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        _admin = await _fixture.CreateAdminClientAsync();
        _user = await _fixture.CreateClientAsAsync("user");
    }

    public async ValueTask DisposeAsync()
    {
        await _user.PutAsJsonAsync("/api/auth/profile", new UpdateProfileRequest("user", null, null));
        await _user.DeleteAsync("/api/auth/profile/image");
        foreach (var id in _players)
        {
            await _admin.DeleteAsync($"/api/player/{id}");
        }

        _admin.Dispose();
        _user.Dispose();
    }

    [Fact]
    public async Task LinkedPlayer_ShouldFollowTheAccountPicture_UnlessItHasItsOwn()
    {
        var player = await CreatePlayerAsync("Avatar follower");
        await LinkSelfAsync(player.Id);

        var profile = await UploadAccountPictureAsync(SKColors.OrangeRed);
        var linked = await GetPlayerAsync(player.Id);
        linked.ImageSource.Should().Be(PlayerImageSource.Account);
        linked.Image.Should().StartWith("/images/profile/").And.NotBe(profile.ProfileImage);
        linked.LinkedAccount!.Username.Should().Be("user");
        File.Exists(PathHelper.MapImageWebPathToPhysical(linked.Image!)).Should().BeTrue();

        var own = await UploadPlayerImageAsync();
        var updated = await UpdatePlayerImageAsync(linked, own);
        updated.ImageSource.Should().Be(PlayerImageSource.Upload);
        updated.Image.Should().Be(own);
        File.Exists(PathHelper.MapImageWebPathToPhysical(linked.Image!)).Should().BeFalse("the replaced account copy is cleaned up");

        await UploadAccountPictureAsync(SKColors.SteelBlue);
        (await GetPlayerAsync(player.Id)).Image.Should().Be(own, "the player's own upload wins");

        var reverted = await UpdatePlayerImageAsync(updated, null);
        reverted.ImageSource.Should().Be(PlayerImageSource.Account);
        reverted.Image.Should().StartWith("/images/profile/").And.NotBe(own);
        File.Exists(PathHelper.MapImageWebPathToPhysical(own)).Should().BeFalse();

        await LinkSelfAsync(null);
        var unlinked = await GetPlayerAsync(player.Id);
        unlinked.Image.Should().BeNull();
        unlinked.ImageSource.Should().Be(PlayerImageSource.None);
        unlinked.LinkedAccount.Should().BeNull();
        File.Exists(PathHelper.MapImageWebPathToPhysical(reverted.Image!)).Should().BeFalse();
    }

    [Fact]
    public async Task RemovingTheAccountPicture_ShouldClearTheLinkedPlayer()
    {
        var player = await CreatePlayerAsync("Avatar remover");
        await UploadAccountPictureAsync(SKColors.OrangeRed);
        await LinkSelfAsync(player.Id);
        (await GetPlayerAsync(player.Id)).ImageSource.Should().Be(PlayerImageSource.Account, "linking applies the picture at once");

        var removed = await _user.DeleteAsync("/api/auth/profile/image", TestContext.Current.CancellationToken);
        removed.StatusCode.Should().Be(HttpStatusCode.OK);

        var cleared = await GetPlayerAsync(player.Id);
        cleared.Image.Should().BeNull();
        cleared.ImageSource.Should().Be(PlayerImageSource.None);
    }

    [Fact]
    public async Task PlayerSideLink_ShouldLinkMoveAndUnlinkTheAccount()
    {
        var first = await CreatePlayerAsync("Account target A");
        var second = await CreatePlayerAsync("Account target B");
        var profile = await UploadAccountPictureAsync(SKColors.OrangeRed);

        var linked = await LinkFromPlayerAsync(first.Id, profile.Id);
        linked.LinkedAccount!.Id.Should().Be(profile.Id);
        linked.ImageSource.Should().Be(PlayerImageSource.Account);

        var moved = await LinkFromPlayerAsync(second.Id, profile.Id);
        moved.LinkedAccount!.Id.Should().Be(profile.Id);
        moved.ImageSource.Should().Be(PlayerImageSource.Account);
        var left = await GetPlayerAsync(first.Id);
        left.LinkedAccount.Should().BeNull();
        left.Image.Should().BeNull();
        var me = await _user.GetFromJsonAsync<ProfileResponse>("/api/auth/profile", IntegrationFixture.Json, TestContext.Current.CancellationToken);
        me!.PlayerId.Should().Be(second.Id);

        var unlinked = await LinkFromPlayerAsync(second.Id, null);
        unlinked.LinkedAccount.Should().BeNull();
        unlinked.Image.Should().BeNull();
        unlinked.ImageSource.Should().Be(PlayerImageSource.None);
    }

    [Theory]
    [InlineData("reader")]
    [InlineData("user")]
    public async Task PlayerSideLink_ShouldBeAdminOnly(string username)
    {
        var player = await CreatePlayerAsync($"Account guarded {username}");
        using var client = await _fixture.CreateClientAsAsync(username);

        var response = await client.PutAsJsonAsync($"/api/admin/players/{player.Id}/account", new LinkPlayerAccountRequest { UserId = null }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PlayerSideLink_ShouldAnswer404_ForAnUnknownAccountOrPlayer()
    {
        var player = await CreatePlayerAsync("Account unknown");

        var unknownUser = await _admin.PutAsJsonAsync($"/api/admin/players/{player.Id}/account", new LinkPlayerAccountRequest { UserId = "missing-user" }, TestContext.Current.CancellationToken);
        var unknownPlayer = await _admin.PutAsJsonAsync("/api/admin/players/999999/account", new LinkPlayerAccountRequest { UserId = null }, TestContext.Current.CancellationToken);

        unknownUser.StatusCode.Should().Be(HttpStatusCode.NotFound);
        unknownPlayer.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<PlayerDto> CreatePlayerAsync(string name)
    {
        var response = await _admin.PostAsJsonAsync("/api/player", new CreatePlayerCommand { Name = $"{name} {Guid.NewGuid():N}" }, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var player = (await response.Content.ReadFromJsonAsync<PlayerDto>(IntegrationFixture.Json, TestContext.Current.CancellationToken))!;
        _players.Add(player.Id);
        return player;
    }

    private async Task<PlayerDto> GetPlayerAsync(int id)
    {
        return (await _user.GetFromJsonAsync<PlayerDto>($"/api/player/{id}", IntegrationFixture.Json, TestContext.Current.CancellationToken))!;
    }

    private async Task LinkSelfAsync(int? playerId)
    {
        var response = await _user.PutAsJsonAsync("/api/auth/profile", new UpdateProfileRequest("user", null, playerId), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    private async Task<PlayerDto> LinkFromPlayerAsync(int playerId, string? userId)
    {
        var response = await _admin.PutAsJsonAsync($"/api/admin/players/{playerId}/account", new LinkPlayerAccountRequest { UserId = userId }, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return (await response.Content.ReadFromJsonAsync<PlayerDto>(IntegrationFixture.Json, TestContext.Current.CancellationToken))!;
    }

    private async Task<ProfileResponse> UploadAccountPictureAsync(SKColor color)
    {
        using var form = ImageForm(color);
        var response = await _user.PutAsync("/api/auth/profile/image", form, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return (await response.Content.ReadFromJsonAsync<ProfileResponse>(IntegrationFixture.Json, TestContext.Current.CancellationToken))!;
    }

    private async Task<string> UploadPlayerImageAsync()
    {
        using var form = ImageForm(SKColors.ForestGreen);
        form.Add(new StringContent(nameof(UploadFileType.Profile)), "Type");
        var response = await _admin.PostAsync("/api/image", form, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Trim('"');
    }

    private async Task<PlayerDto> UpdatePlayerImageAsync(PlayerDto player, string? image)
    {
        var response = await _admin.PutAsJsonAsync("/api/player", new UpdatePlayerCommand { Id = player.Id, Name = player.Name, Image = image }, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return (await response.Content.ReadFromJsonAsync<PlayerDto>(IntegrationFixture.Json, TestContext.Current.CancellationToken))!;
    }

    private static MultipartFormDataContent ImageForm(SKColor color)
    {
        using var bitmap = new SKBitmap(64, 64);
        bitmap.Erase(color);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(data.ToArray());
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "File", "picture.png");
        return form;
    }
}
