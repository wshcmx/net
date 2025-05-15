using System.Text.Json;

namespace wshcmx;

public static class Template
{
    public static string GenerateTemplate(string content, string serializedArgs)
    {
        var args = JsonSerializer.Deserialize<Dictionary<string, object>>(serializedArgs);
        return Internals.Templater.Generate(content, args);
    }
}
