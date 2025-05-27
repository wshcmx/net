Typifier.Generator.Generate();

// using System.Reflection;

// public class TypeScriptGenerator
// {
//     public static string GenerateInterface(Type type)
//     {
//         var sb = new StringBuilder();
//         sb.AppendLine($"export interface {type.Name} {{");

//         foreach (var prop in type.GetProperties())
//         {
//             sb.AppendLine($"  {prop.Name}: {MapType(prop.PropertyType)};");
//         }

//         sb.AppendLine("}");
//         return sb.ToString();
//     }


// }