namespace BoardGameTracker.Core.Common;

public static class TypeConverter
{
    public static bool TryConvertFromString<T>(string value, out T result)
    {
        var type = typeof(T);

        if (type == typeof(string))
        {
            result = (T)(object)value;
            return true;
        }

        if (type == typeof(int) && int.TryParse(value, out var intResult))
        {
            result = (T)(object)intResult;
            return true;
        }

        if (type == typeof(bool) && bool.TryParse(value, out var boolResult))
        {
            result = (T)(object)boolResult;
            return true;
        }

        if (type.IsEnum && Enum.TryParse(type, value, true, out var enumResult))
        {
            result = (T)enumResult!;
            return true;
        }

        result = default!;
        return false;
    }
}
