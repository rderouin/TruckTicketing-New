using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

using Trident.Contracts.Configuration;

namespace SE.Shared.Domain.EmailTemplates;

public interface ISmtpClientProvider
{
    ValueTask Send(MailMessage message);
}

public class SmtpClientProvider : ISmtpClientProvider
{
    private readonly IAppSettings _appSettings;

    public SmtpClientProvider(IAppSettings appSettings)
    {
        _appSettings = appSettings;
    }

    public async ValueTask Send(MailMessage message)
    {
        var settings = _appSettings.GetSection<SmtpConfiguration>("Smtp");

        // configure the SMTP client
        using var smtpClient = new SmtpClient();
        smtpClient.Host = settings.Hostname;
        smtpClient.Port = settings.Port;
        smtpClient.EnableSsl = settings.EnableSsl;
        smtpClient.Credentials = new NetworkCredential(settings.Username, settings.Password);

        // ensure the mail message has basic parameters
        message.From ??= new(settings.Sender);

        // send it
        await smtpClient.SendMailAsync(message);
    }
}

public class SmtpConfiguration
{
    public string Hostname { get; set; }

    public int Port { get; set; }

    public bool EnableSsl { get; set; }

    public string Username { get; set; }

    public string Password { get; set; }

    public string Sender { get; set; }
}
