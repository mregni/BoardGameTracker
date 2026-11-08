using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BoardGamer.BoardGameGeek.BoardGameGeekXmlApi2;
using BoardGameTracker.Core.Games;
using BoardGameTracker.Core.Settings.Interfaces;
using FluentAssertions;
using Moq;
using Moq.Protected;
using Xunit;

namespace BoardGameTracker.Tests.Games;

public class LazyBoardGameGeekClientTests
{
    private const string EmptyItemsXml =
        """<?xml version="1.0" encoding="utf-8"?><items totalitems="0" pubdate="Sat, 01 Jan 2022 00:00:00 +0000" termsofuse="https://boardgamegeek.com/xmlapi/termsofuse"></items>""";

    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly List<HttpRequestMessage> _sentRequests = [];
    private int _httpClientFactoryCalls;
    private readonly LazyBoardGameGeekClient _client;

    public LazyBoardGameGeekClientTests()
    {
        _settingsServiceMock = new Mock<ISettingsService>();
        _settingsServiceMock.Setup(x => x.GetBggApiKeyAsync()).ReturnsAsync("test-api-key");

        _handlerMock = new Mock<HttpMessageHandler>();
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => _sentRequests.Add(request))
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(EmptyItemsXml, Encoding.UTF8, "application/xml")
            });

        _client = new LazyBoardGameGeekClient(CreateHttpClient, _settingsServiceMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _settingsServiceMock.VerifyNoOtherCalls();
        _handlerMock.VerifyNoOtherCalls();
    }

    private void VerifySentRequests(int count)
    {
        _settingsServiceMock.Verify(x => x.GetBggApiKeyAsync(), Times.Once);
        _handlerMock.Protected().Verify<Task<HttpResponseMessage>>(
            "SendAsync",
            Times.Exactly(count),
            ItExpr.Is<HttpRequestMessage>(r => _sentRequests.Contains(r)),
            ItExpr.IsAny<CancellationToken>());
    }

    private HttpClient CreateHttpClient()
    {
        _httpClientFactoryCalls++;
        return new HttpClient(_handlerMock.Object);
    }

    [Fact]
    public void Constructor_ShouldNotResolveApiKeyOrHttpClient()
    {
        _httpClientFactoryCalls.Should().Be(0);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetThingAsync_ShouldResolveApiKeyAndHttpClientOnlyOnce_AcrossCalls()
    {
        var first = await _client.GetThingAsync(new ThingRequest([1]));
        var second = await _client.GetThingAsync(new ThingRequest([2]));

        first.Succeeded.Should().BeTrue();
        second.Succeeded.Should().BeTrue();
        _httpClientFactoryCalls.Should().Be(1);
        VerifySentRequests(2);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetThingAsync_ShouldSendResolvedApiKeyOnEachRequest()
    {
        await _client.GetThingAsync(new ThingRequest([1]));
        await _client.GetThingAsync(new ThingRequest([2]));

        _sentRequests.Should().HaveCount(2);
        _sentRequests.Select(r => r.Headers.Authorization!.ToString()).Should().Equal("Bearer test-api-key", "Bearer test-api-key");
        VerifySentRequests(2);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetThingAsync_ShouldAskForTheRequestedItemAtTheThingEndpoint()
    {
        await _client.GetThingAsync(new ThingRequest([174430]));

        var uri = _sentRequests.Should().ContainSingle().Which.RequestUri!;
        uri.AbsolutePath.Should().EndWith("/thing");
        uri.Query.Should().Contain("id=174430");
        VerifySentRequests(1);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetCollectionAsync_ShouldAskForTheCollectionOfTheUser()
    {
        await _client.GetCollectionAsync(new CollectionRequest("reggi"));

        var uri = _sentRequests.Should().ContainSingle().Which.RequestUri!;
        uri.AbsolutePath.Should().EndWith("/collection");
        uri.Query.Should().Contain("username=reggi");
        VerifySentRequests(1);
        VerifyNoOtherCalls();
    }

    public static TheoryData<string, string, string> DelegatedEndpoints() => new()
    {
        { nameof(LazyBoardGameGeekClient.GetFamilyAsync), "/family", "id=5" },
        { nameof(LazyBoardGameGeekClient.GetForumListAsync), "/forumlist", "id=5" },
        { nameof(LazyBoardGameGeekClient.GetForumsAsync), "/forum", "id=5" },
        { nameof(LazyBoardGameGeekClient.GetHotItemsAsync), "/hot", "type=boardgame" },
        { nameof(LazyBoardGameGeekClient.GetPlaysAsync), "/plays", "username=reggi" },
        { nameof(LazyBoardGameGeekClient.GetThreadsAsync), "/thread", "id=5" },
        { nameof(LazyBoardGameGeekClient.GetUserAsync), "/user", "name=reggi" },
    };

    [Theory]
    [MemberData(nameof(DelegatedEndpoints))]
    public async Task Get_ShouldSendTheRequestWithTheApiKeyToItsOwnEndpoint(string method, string path, string query)
    {
        Task call = method switch
        {
            nameof(LazyBoardGameGeekClient.GetFamilyAsync) => _client.GetFamilyAsync(new FamilyRequest(5, "boardgamefamily")),
            nameof(LazyBoardGameGeekClient.GetForumListAsync) => _client.GetForumListAsync(new ForumListRequest(5, "thing")),
            nameof(LazyBoardGameGeekClient.GetForumsAsync) => _client.GetForumsAsync(new ForumsRequest(5)),
            nameof(LazyBoardGameGeekClient.GetHotItemsAsync) => _client.GetHotItemsAsync(new HotItemsRequest("boardgame")),
            nameof(LazyBoardGameGeekClient.GetPlaysAsync) => _client.GetPlaysAsync(new PlaysRequest("reggi")),
            nameof(LazyBoardGameGeekClient.GetThreadsAsync) => _client.GetThreadsAsync(new ThreadsRequest(5)),
            _ => _client.GetUserAsync(new UserRequest("reggi")),
        };
        await call;

        var request = _sentRequests.Should().ContainSingle().Subject;
        request.RequestUri!.AbsolutePath.Should().EndWith(path);
        request.RequestUri.Query.Should().Contain(query);
        request.Headers.Authorization!.ToString().Should().Be("Bearer test-api-key");
        _httpClientFactoryCalls.Should().Be(1);
        VerifySentRequests(1);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchAsync_ShouldAskForTheQueryAtTheSearchEndpoint()
    {
        await _client.SearchAsync(new SearchRequest("Root"));

        var uri = _sentRequests.Should().ContainSingle().Which.RequestUri!;
        uri.AbsolutePath.Should().EndWith("/search");
        uri.Query.Should().Contain("query=Root");
        VerifySentRequests(1);
        VerifyNoOtherCalls();
    }
}
