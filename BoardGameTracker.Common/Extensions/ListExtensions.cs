namespace BoardGameTracker.Common.Extensions;

public static class ListExtensions
{
    public static void AddIfNotNull<T>(this List<T> list, T? item) where T : class
    {
        if (item != null)
        {
            list.Add(item);
        }
    }
}