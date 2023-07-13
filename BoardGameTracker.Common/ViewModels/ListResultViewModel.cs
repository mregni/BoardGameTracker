namespace BoardGameTracker.Common.ViewModels;

public class ListResultViewModel<T>
{
    public int Count { get; set; }
    public IEnumerable<T> List { get; set; }

    public ListResultViewModel(ICollection<T> list)
    {
        List = list;
        Count = list.Count;
    }
}