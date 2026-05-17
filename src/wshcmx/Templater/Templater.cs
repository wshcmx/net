using Mustache;

namespace wshcmx.Net;

public static class Templater
{
    public static string Generate(string template, object[] data)
    {
        if (File.Exists(template))
        {
            template = File.ReadAllText(template);
        }

        return Template.Compile(template).Render(data);
    }
}
