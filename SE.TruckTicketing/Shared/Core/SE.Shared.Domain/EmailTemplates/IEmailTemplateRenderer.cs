using System.Threading.Tasks;

namespace SE.Shared.Domain.EmailTemplates;

public interface IEmailTemplateRenderer
{
    ValueTask<string> RenderTemplate<T>(string templateKey, string templateSource, T model);
}
