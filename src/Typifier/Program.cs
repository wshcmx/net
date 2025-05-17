using System.Reflection;
using System.Text;

var types = Assembly.Load("wshcmx")
    .GetTypes()
    .Where(x => !x.Name.EndsWith("Attribute"));

var sb = new StringBuilder();
sb.AppendLine("declare namespace wshcmx {");

var ConvertToWebsoftHCMType = (string data) => data switch
{
    "System.String" => "string",
    "System.Int32" => "number",
    "System.Boolean" => "boolean",
    "System.DateTime" => "Date",
    "System.Object" => "any",
    _ => data
};

foreach (var type in types)
{
    sb.AppendLine($"  export class {type.Name} {{");

    foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Static))
    {
        List<string> parameters = [];

        foreach (var parameter in method.GetParameters())
        {
            parameters.Add($"{parameter.Name}: {ConvertToWebsoftHCMType(parameter.ParameterType.ToString())}");
        }

        sb.AppendLine($"    {method.Name}({string.Join(", ", parameters)}): {ConvertToWebsoftHCMType(method.ReturnType.ToString())};");
    }

    sb.AppendLine("  }");
}

sb.AppendLine("}");
File.WriteAllText("wshcmx.d.ts", sb.ToString());