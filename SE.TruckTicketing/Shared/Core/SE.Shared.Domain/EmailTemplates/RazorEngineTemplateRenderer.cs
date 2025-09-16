using System;
using System.Threading.Tasks;

using RazorEngine.Templating;

namespace SE.Shared.Domain.EmailTemplates;

public class RazorEngineTemplateRenderer : IEmailTemplateRenderer
{
    public ValueTask<string> RenderTemplate<T>(string templateKey, string templateSource, T model)
    {
        var service = RazorEngineService.Create();
        var renderedTemplate = service.RunCompile(templateSource, templateKey ?? String.Empty, model?.GetType(), model);
        return ValueTask.FromResult(renderedTemplate);
    }
}
