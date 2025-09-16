using System;
using System.Collections.Generic;

using SE.TruckTicketing.Contracts.Lookups;

using Trident.Contracts.Api;

namespace SE.TruckTicketing.Contracts.Models.ContentGeneration;

public class EmailTemplate : GuidApiModelBase
{
    public string Name { get; set; }

    public bool UseCustomSenderEmail { get; set; }

    public string SenderEmail { get; set; }

    public bool EnableReplyTracking { get; set; }

    public EmailTemplateReplyType ReplyType { get; set; }

    public string CustomReplyEmail { get; set; }

    public EmailTemplateBccType BccType { get; set; }

    public EmailTemplateCcType? CcType { get; set; }

    public string CustomBccEmails { get; set; }

    public List<string> FacilitySiteIds { get; set; }

    public List<Guid> AccountIds { get; set; }

    public List<Guid> IncludedAttachmentIds { get; set; }

    public string Subject { get; set; }

    public string Body { get; set; }

    public Guid EventId { get; set; }

    public string EventName { get; set; }

    public bool IsActive { get; set; }
}

public class EmailTemplateEvent : GuidApiModelBase
{
    public string Name { get; set; }

    public List<EmailTemplateEventField> Fields { get; set; }

    public List<EmailTemplateEventAttachment> Attachments { get; set; }
}

public class EmailTemplateEventField
{
    public string UiToken { get; set; }

    public string RazorToken { get; set; }

    public string TooltipContent { get; set; }

    public string Key { get; set; }
}

public class EmailTemplateEventAttachment : ApiModelBase<Guid>
{
    public string Name { get; set; }
}
