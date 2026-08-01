using BoardGamer.BoardGameGeek.BoardGameGeekXmlApi2;
using BoardGameTracker.Core.Settings.Interfaces;

namespace BoardGameTracker.Core.Games;

public class LazyBoardGameGeekClient : IBoardGameGeekXmlApi2Client
{
    private readonly Lazy<Task<IBoardGameGeekXmlApi2Client>> _client;

    public LazyBoardGameGeekClient(Func<HttpClient> httpClientFactory, ISettingsService settingsService)
    {
        _client = new Lazy<Task<IBoardGameGeekXmlApi2Client>>(async () =>
        {
            var apiKey = await settingsService.GetBggApiKeyAsync();
            var options = new BoardGameGeekXmlApi2ClientOptions
            {
                AuthorizationToken = apiKey,
                Delay = TimeSpan.FromSeconds(2),
                MaxRetries = 10
            };
            return new BoardGameGeekXmlApi2Client(httpClientFactory(), options);
        });
    }

    public async Task<CollectionResponse> GetCollectionAsync(CollectionRequest request) =>
        await (await _client.Value).GetCollectionAsync(request);

    public async Task<FamilyResponse> GetFamilyAsync(FamilyRequest request) =>
        await (await _client.Value).GetFamilyAsync(request);

    public async Task<ForumListResponse> GetForumListAsync(ForumListRequest request) =>
        await (await _client.Value).GetForumListAsync(request);

    public async Task<ForumsResponse> GetForumsAsync(ForumsRequest request) =>
        await (await _client.Value).GetForumsAsync(request);

    public async Task<GuildResponse> GetGuildAsync(GuildRequest request) =>
        await (await _client.Value).GetGuildAsync(request);

    public async Task<HotItemsResponse> GetHotItemsAsync(HotItemsRequest request) =>
        await (await _client.Value).GetHotItemsAsync(request);

    public async Task<PlaysResponse> GetPlaysAsync(PlaysRequest request) =>
        await (await _client.Value).GetPlaysAsync(request);

    public async Task<ThingResponse> GetThingAsync(ThingRequest request) =>
        await (await _client.Value).GetThingAsync(request);

    public async Task<ThreadsResponse> GetThreadsAsync(ThreadsRequest request) =>
        await (await _client.Value).GetThreadsAsync(request);

    public async Task<UserResponse> GetUserAsync(UserRequest request) =>
        await (await _client.Value).GetUserAsync(request);

    public async Task<SearchResponse> SearchAsync(SearchRequest request) =>
        await (await _client.Value).SearchAsync(request);
}
