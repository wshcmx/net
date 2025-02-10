using System.Text.Json;
using Datex.Global.refs.xhttp;

namespace wshcmx;

public static class Core
{
    public static string Generate(JsObject data)
    {
        var args = JsonSerializer.Deserialize<Dictionary<string, object>>(data["args"]);
        return Templater.Generate(data["content"], args);
    }
}
