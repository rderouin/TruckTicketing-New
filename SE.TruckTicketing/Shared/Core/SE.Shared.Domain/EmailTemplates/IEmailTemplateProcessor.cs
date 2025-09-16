using System.Threading.Tasks;

namespace SE.Shared.Domain.EmailTemplates;

public interface IEmailTemplateProcessor
{
    public ValueTask BeforeSend(EmailTemplateProcessingContext context);

    public ValueTask AfterSend(EmailTemplateProcessingContext context);

    public ValueTask<EmailTemplateEntity> ResolveEmailTemplate(EmailTemplateProcessingContext context);
}
