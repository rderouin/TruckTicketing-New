using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Trident.IoC;

namespace SE.Shared.Domain.EmailTemplates;

public class EmailTemplateSender : IEmailTemplateSender
{
    private readonly ILogger<EmailTemplateSender> _logger;

    private readonly IIoCServiceLocator _serviceLocator;

    private readonly ISmtpClientProvider _smtpClientProvider;

    public EmailTemplateSender(IIoCServiceLocator serviceLocator, ISmtpClientProvider smtpClientProvider, ILogger<EmailTemplateSender> logger)
    {
        _serviceLocator = serviceLocator;
        _smtpClientProvider = smtpClientProvider;
        _logger = logger;
    }

    public async ValueTask SendEmail(EmailTemplateDeliveryRequest request)
    {
        var processor = _serviceLocator.GetNamed<IEmailTemplateProcessor>(request.TemplateKey);
        var context = new EmailTemplateProcessingContext(request);
        context.EmailTemplate = await processor.ResolveEmailTemplate(context);

        await processor.BeforeSend(context);

        try
        {
            await _smtpClientProvider.Send(context.MailMessage);
            context.IsMessageSent = true;
        }
        catch (Exception e)
        {
            context.Exception = e;
            _logger.LogError(e, "An error occured while trying to send MailMessage");
        }

        await processor.AfterSend(context);
    }

    public ValueTask Dispatch(EmailTemplateDeliveryRequest request)
    {
        return SendEmail(request);
        // if (request.IsSynchronous)
        // {
        //     
        // }
        //
        // // TODO: Service bus dispatch
        // return ValueTask.CompletedTask;
    }
}

public interface IEmailTemplateSender
{
    ValueTask SendEmail(EmailTemplateDeliveryRequest request);

    ValueTask Dispatch(EmailTemplateDeliveryRequest request);
}
