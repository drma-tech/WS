namespace WS.Shared.Core.Helper;

public static class EnumHelper
{
    public static T[] GetValues<T>() where T : struct, Enum
    {
        return Enum.GetValues<T>();
    }

    public static List<EnumFieldObject<T>> GetList<T>(bool translate = true) where T : struct, Enum
    {
        var values = GetValues<T>();
        var result = new List<EnumFieldObject<T>>(values.Length);

        foreach (var val in values)
        {
            result.Add(val.GetFieldSettings(translate));
        }

        return result;
    }

    public static T ParseToEnum<T>(this string? value, T? fallback = null) where T : struct, Enum
    {
        if (Enum.TryParse<T>(value, true, out var result) && Enum.IsDefined(result))
        {
            return result;
        }
        else if (fallback.HasValue)
        {
            return fallback.Value;
        }
        else
        {
            throw new ArgumentException($"Invalid value for enum type {typeof(T).Name}: {value}");
        }
    }
}
