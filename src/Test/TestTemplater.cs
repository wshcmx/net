using System.Text.Json;

using wshcmx;

namespace Test;

public class TestTemplater
{
    [Fact]
    public void SimpleTest()
    {
        const string content = "Hello {{name}}";

        Dictionary<string, object> data = new()
        {
            { "name", "World" }
        };

        var res = Templater.Generate(content, JsonSerializer.Serialize(data));
        Assert.Equal("Hello World", res);
    }

    [Fact]
    public void ComplexTest()
    {
        var data = new
        {
            list = new[] {
                "John",
                "Doe"
            }
        };

        var res = Templater.Generate(@"<ul>
  {{#list}}
  <li>{{.}}</li>
  {{/list}}
</ul>",
JsonSerializer.Serialize(data));
        Assert.Equal(@"<ul>
  <li>John</li>
  <li>Doe</li>
</ul>", res);
    }
}
