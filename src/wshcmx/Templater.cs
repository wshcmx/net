using Scriban;

namespace wshcmx;

class Templater
{
    public static string Generate(string content, object? args)
    {
        if (File.Exists(content))
        {
            content = File.ReadAllText(content);
        }

        return Template.Parse(content).Render(args);
    }
}