using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.ViewModels;

public class GameSearchResultViewModel
{
    public GameViewModel? Game { get; set; }
    public SearchResult Result { get; set; }

    public static GameSearchResultViewModel CreateDuplicateResult(GameViewModel? model)
    {
        return new GameSearchResultViewModel()
        {
            Result = SearchResult.Duplicate,
            Game = model
        };
    }

    public static GameSearchResultViewModel CreateSearchResult(GameViewModel? model)
    {
        return new GameSearchResultViewModel
        {
            Game = model,
            Result = model != null ? SearchResult.Found : SearchResult.NotFound
        };
    }
}