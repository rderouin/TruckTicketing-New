using System;
using System.Collections.Generic;

namespace SE.Shared.Domain.EmailTemplates;

public class EmailTemplateDeliveryRequest
{
    public bool IsSynchronous { get; set; }

    public string Recipients { get; set; }

    public string CcRecipients { get; set; }

    public string BccRecipients { get; set; }

    public string TemplateKey { get; set; }

    public string AdHocNote { get; set; }

    public List<AdHocAttachment> AdHocAttachments { get; set; }

    public Dictionary<string, object> ContextBag { get; set; } = new();

    public TValue GetValueOrDefaultFromContextBag<TValue>(string key, TValue defaultValue = default)
    {
        if (ContextBag is null)
        {
            return defaultValue;
        }

        ContextBag.TryGetValue(key, out var value);

        try
        {
            return (TValue)value ?? defaultValue;
        }
        catch (InvalidCastException)
        {
            return defaultValue;
        }
    }
}

public class AdHocAttachment
{
    public string Uri { get; set; }
    
    public string ContainerName { get; set; }

    public string BlobPath { get; set; }

    public string FileName { get; set; }

    public string ContentType { get; set; }

    public long Size { get; set; }

    public string RequestId { get; set; }

    public string EventName { get; set; }
}
