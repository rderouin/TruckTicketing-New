using System.Threading.Tasks;

using FluentAssertions;

using SE.Shared.Domain.EmailTemplates;

using Trident.Testing.TestScopes;

namespace SE.Shared.Domain.Tests.Email;

[TestClass]
public class RazorEngineTemplateRendererTests
{
    [DataTestMethod]
    [DataRow("Test template")]
    [DataRow("")]
    [DataRow(" ")]
    public async Task Renderer_ShouldRenderSimpleTemplate_WithNoObject(string templateSource)
    {
        // arrange
        var scope = new DefaultScope();

        // act
        var result = await scope.InstanceUnderTest.RenderTemplate<object>("", templateSource, null);

        // assert
        result.Should().Be(templateSource);
    }

    [DataTestMethod]
    [DataRow("Test template")]
    [DataRow("")]
    [DataRow(" ")]
    public async Task Renderer_ShouldRenderSimpleTemplate_WithRazorItems(string value)
    {
        // arrange
        var scope = new DefaultScope();
        var templateSource = "@(Model.Value)";
        var data = new DataObject { Value = value };

        // act
        var result = await scope.InstanceUnderTest.RenderTemplate<object>("", templateSource, data);

        // assert
        result.Should().Be(value.Trim());
    }

    private class DefaultScope : TestScope<IEmailTemplateRenderer>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new RazorEngineTemplateRenderer();
        }
    }

    public class DataObject
    {
        public string Value { get; set; }
    }
}
