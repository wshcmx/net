namespace wshcmx.Net;

internal static class GuardHelper
{
    public static object? GetDictionaryValue(Dictionary<string, object>? dictionary, string key)
    {
        ThrowIfNull(key, nameof(key));

        if (dictionary is not null && dictionary.TryGetValue(key, out object? value))
        {
            return value;
        }

        return null;
    }

    public static T GetRequired<T>(T? value, string paramName) where T : class
    {
        ThrowIfNull(value, paramName);
        return value!;
    }

    public static void ThrowIfNull(object? value, string paramName)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(value, paramName);
#else
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }
#endif
    }

    public static void ThrowIfWhiteSpace(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("The value cannot be an empty string or composed entirely of whitespace.", paramName);
        }
    }
}
