using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using SE.BillingService.Contracts.Api.Enums;
using SE.BillingService.Domain.InvoiceDelivery.Context;
using SE.BillingService.Domain.InvoiceDelivery.Encoders;
using SE.BillingService.Domain.InvoiceDelivery.Shared;
using SE.Shared.Common.Extensions;
using SE.Shared.Domain.EmailTemplates;

using Trident.Contracts.Configuration;

namespace SE.BillingService.Domain.InvoiceDelivery.Transport;

public class SmtpInvoiceDeliveryTransport : IInvoiceDeliveryTransport
{
    private readonly Lazy<SmtpConfiguration> _smtpConfigurationLazy;

    public SmtpInvoiceDeliveryTransport(IAppSettings appSettings)
    {
        _smtpConfigurationLazy = new(() => appSettings.GetSection<SmtpConfiguration>("Smtp"));
    }

    public InvoiceDeliveryTransportType TransportType => InvoiceDeliveryTransportType.Smtp;

    public async Task Send(EncodedInvoicePart part, InvoiceDeliveryTransportInstructions instructions)
    {
        // cannot send attachments by themselves 
        if (part.IsAttachment)
        {
            return;
        }

        // create an SMTP client
        var smtpClient = await CreateSmtpClient();

        // compose a message
        var mailMessage = JsonConvert.DeserializeObject<MailMessageSurrogate>(Encoding.UTF8.GetString(await part.DataStream.ReadAll()))!.CreateMailMessage();

        // send it
        mailMessage.From = new(_smtpConfigurationLazy.Value.Sender);
        await smtpClient.SendAsync(mailMessage);
    }

    public virtual ValueTask<ICustomSmtpClient> CreateSmtpClient()
    {
        // settings for the SMTP client
        var settings = _smtpConfigurationLazy.Value;

        // the SMTP client
        var smtpClient = new CustomSmtpClient
        {
            Host = settings.Hostname,
            Port = settings.Port,
            EnableSsl = settings.EnableSsl,
            Credentials = new NetworkCredential(settings.Username, settings.Password),
        };

        return ValueTask.FromResult<ICustomSmtpClient>(smtpClient);
    }
}

public interface ICustomSmtpClient
{
    Task SendAsync(MailMessage message);
}

public class CustomSmtpClient : SmtpClient, ICustomSmtpClient
{
    public async Task SendAsync(MailMessage message)
    {
        await SendMailAsync(message);
    }
}
