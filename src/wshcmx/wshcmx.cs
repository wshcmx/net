using System.Text.Json;
using Reinforced.Typings.Attributes;

namespace wshcmx;

[TsInterface]
public static class Core
{
    public static string Generate(string content, string serializedArgs)
    {
        var args = JsonSerializer.Deserialize<Dictionary<string, object>>(serializedArgs);
        return Internals.Templater.Generate(content, args);
    }
}
