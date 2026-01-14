using BoardGameTracker.Common.Models.DockerHub;
using Refit;

namespace BoardGameTracker.Core.DockerHub;

public interface IDockerHubApi
{
    [Get("/v2/repositories/{owner}/{repository}/tags?page_size=100&ordering=-last_updated")]
    Task<ApiResponse<DockerHubTagsResponse>> GetTags(string owner, string repository);

    [Get("/v2/repositories/{owner}/{repository}/tags/{tag}")]
    Task<ApiResponse<DockerHubManifestResponse>> GetTagManifest(string owner, string repository, string tag);
}
