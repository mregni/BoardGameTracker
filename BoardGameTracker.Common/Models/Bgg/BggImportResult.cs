using System.Net;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Models.Bgg;

public class BggImportResult
{
    public HttpStatusCode StatusCode { get; set; }
    public List<BggImportGame> Games { get; set; }

    public BggImportResult()
    {
        Games = [];
    }
}

public class BggImportGame
{
    public string Title { get; set; }
    public int BggId { get; set; }
    public string ImageUrl { get; set; }
    public GameState State { get; set; }
    public DateTime LastModified { get; set; }
    public bool IsExpansion { get; set; }
}