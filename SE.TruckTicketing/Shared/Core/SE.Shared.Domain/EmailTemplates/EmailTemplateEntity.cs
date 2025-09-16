using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Data;
using Trident.Domain;
using Trident.SourceGeneration.Attributes;

namespace SE.Shared.Domain.EmailTemplates;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Operations, nameof(DocumentType), nameof(Databases.DocumentTypes.EmailTemplate), PartitionKeyType.WellKnown)]
[Discriminator(nameof(EntityType), Databases.Discriminators.EmailTemplate)]
[GenerateManager]
[GenerateProvider]
public class EmailTemplateEntity : TTAuditableEntityBase
{
    public string Name { get; set; }
    
    public bool? UseCustomSenderEmail { get; set; }
    
    public string SenderEmail { get; set; }

    public bool EnableReplyTracking { get; set; }

    public string CustomReplyEmail { get; set; }

    public EmailTemplateReplyType ReplyType { get; set; }

    public EmailTemplateBccType BccType { get; set; }

    public EmailTemplateCcType? CcType { get; set; }

    public string CustomBccEmails { get; set; }

    public PrimitiveCollection<string> FacilitySiteIds { get; set; }

    public PrimitiveCollection<Guid> AccountIds { get; set; }

    public string Subject { get; set; }

    public string Body { get; set; }

    public Guid EventId { get; set; }

    public string EventName { get; set; }

    public bool IsActive { get; set; }

    [NotMapped]
    public EmailTemplateEventEntity EmailTemplateEvent { get; set; }

    [NotMapped]
    public bool? HasUniqueName { get; set; }

    [NotMapped]
    public EmailTemplateEntity[] Siblings { get; set; }

    [NotMapped]
    public string[] SplitCustomBccEmails =>
        (CustomBccEmails ?? "").Split(';')
                               .Select(email => email.Trim())
                               .Where(email => email.HasText())
                               .ToArray();
}
