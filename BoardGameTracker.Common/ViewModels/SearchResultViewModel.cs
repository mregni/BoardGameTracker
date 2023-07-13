using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.ViewModels;

public class SearchResultViewModel<T> where T : class
{
    public T? Model { get; set; }
    public SearchResult Result { get; set; }

    public static SearchResultViewModel<T> CreateDuplicateResult(T? model)
    {
        return new SearchResultViewModel<T>()
        {
            Result = SearchResult.Duplicate,
            Model = model
        };
    }

    public static SearchResultViewModel<T> CreateSearchResult(T? model)
    {
        return new SearchResultViewModel<T>
        {
            Model = model,
            Result = model != null ? SearchResult.Found : SearchResult.NotFound
        };
    }
}