using BoardGameTracker.Common.Enums;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Common.ViewModels.Results;

public class ListResultViewModel<T>
{
    public int Count { get; set; }
    public IEnumerable<T>? List { get; set; }

    public static OkObjectResult CreateResult(ICollection<T> list)
    {
        var data = new ListResultViewModel<T>
        {
            List = list,
            Count = list.Count
        };
        
        return new OkObjectResult(data);
    }
    
    public static OkObjectResult CreateResult(ICollection<T> list, int totalCount)
    {
        var data = new ListResultViewModel<T>
        {
            List = list,
            Count = totalCount
        };
        
        return new OkObjectResult(data);
    }
}