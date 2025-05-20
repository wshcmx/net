using System.Text.Json;

using Mustache;

namespace wshcmx;

public static class Templater
{
    public static string Generate(string template, string data)
    {
        if (File.Exists(template))
        {
            template = File.ReadAllText(template);
        }

        return Template.Compile(template).Render(JsonSerializer.Deserialize<Dictionary<string, object>>(data));
    }
}
