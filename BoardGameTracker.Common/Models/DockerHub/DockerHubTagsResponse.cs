namespace BoardGameTracker.Common.Models.DockerHub;

public class DockerHubTagsResponse
{
    public int Count { get; set; }
    public List<DockerHubTag> Results { get; set; } = new();
}

public class DockerHubTag
{
    public string Name { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}

public class DockerHubManifestResponse
{
    public string Architecture { get; set; } = string.Empty;
    public List<DockerHubManifest> Manifests { get; set; } = new();
}

public class DockerHubManifest
{
    public DockerHubPlatform Platform { get; set; } = new();
}

public class DockerHubPlatform
{
    public string Architecture { get; set; } = string.Empty;
    public string Os { get; set; } = string.Empty;
}
