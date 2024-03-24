using BoardGameTracker.Common.Enums;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Common.ViewModels.Results;

public class ResultViewModel<T> where T : class
{
    public T? Model { get; set; }
    public ResultState State { get; set; }

    public static OkObjectResult CreateDuplicateResult(T model)
    {
        return CreateResult(model, ResultState.Duplicate);
    }

    public static OkObjectResult CreateFoundResult(T model)
    {
        return CreateResult(model, ResultState.Found);
    }
    
    public static OkObjectResult CreateCreatedResult(T model)
    {
        return CreateResult(model, ResultState.Success);
    }
    
    public static OkObjectResult CreateUpdatedResult(T model)
    {
        return CreateResult(model, ResultState.Updated);
    }

    private static OkObjectResult CreateResult(T model, ResultState state)
    {
        var data = new ResultViewModel<T>
        {
            Model = model,
            State = state
        };
        
        return new OkObjectResult(data);
    }
}