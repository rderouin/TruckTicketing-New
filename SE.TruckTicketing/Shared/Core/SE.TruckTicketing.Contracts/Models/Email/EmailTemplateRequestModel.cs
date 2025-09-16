using System.Collections.Generic;

namespace SE.TruckTicketing.Contracts.Models.Email;

public class EmailTemplateDeliveryRequestModel
{
    public string Recipients { get; set; }

    public string CcRecipients { get; set; }

    public string BccRecipients { get; set; }

    public string TemplateKey { get; set; }

    public string AdHocNote { get; set; }

    public List<AdHocAttachmentModel> AdHocAttachments { get; set; }

    public Dictionary<string, object> ContextBag { get; set; }
}

public class AdHocAttachmentModel
{
    public string Uri { get; set; }

    public string BlobPath { get; set; }

    public string FileName { get; set; }

    public string ContentType { get; set; }

    public long Size { get; set; }

    public string EventName { get; set; }

    public string RequestId { get; set; }
}
