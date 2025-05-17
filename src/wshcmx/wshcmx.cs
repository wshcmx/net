using System.Text.Json;

namespace wshcmx;

public static class Core
{
    public static string Generate(string content, string serializedArgs)
    {
        var args = JsonSerializer.Deserialize<Dictionary<string, object>>(serializedArgs);
        return Internals.Templater.Generate(content, args);
    }
}
