using Internals;

namespace Test;

public class TestTemplateGenerator
{
    [Fact]
    public void SimpleTest()
    {
        const string content = "Hello {{name}}";

        var data = new Dictionary<string, object>
        {
            { "name", "World" }
        };

        var res = Templater.Generate(content, data);
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
data);
        Assert.Equal(@"<ul>
  <li>John</li>
  <li>Doe</li>
</ul>", res);
    }
}
