using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;

using SE.Shared.Common.Extensions;

namespace SE.BillingService.Domain.InvoiceDelivery.Shared;

public class MailMessageSurrogate
{
    public string To { get; set; }

    public string Cc { get; set; }

    public string Bcc { get; set; }

    public string ReplyTo { get; set; }

    public string Subject { get; set; }

    public string Body { get; set; }

    public List<MailMessageSurrogateAttachment> Attachments { get; set; }

    public MailMessage CreateMailMessage()
    {
        var mailMessage = new MailMessage
        {
            Subject = Subject ?? string.Empty,
            Body = Body ?? string.Empty,
        };

        if (To.HasText())
        {
            mailMessage.To.Add(To);
        }

        if (Cc.HasText())
        {
            mailMessage.CC.Add(Cc);
        }

        if (Bcc.HasText())
        {
            mailMessage.Bcc.Add(Bcc);
        }

        if (ReplyTo.HasText())
        {
            mailMessage.ReplyToList.Add(ReplyTo);
        }

        if (Attachments?.Any() == true)
        {
            Attachments.ForEach(a => mailMessage.Attachments.Add(a.CreateAttachment()));
        }

        return mailMessage;
    }
}

public class MailMessageSurrogateAttachment
{
    public byte[] Data { get; set; }

    public string Name { get; set; }

    public string MediaType { get; set; }

    public Attachment CreateAttachment()
    {
        return new(new MemoryStream(Data), Name, MediaType);
    }
}
