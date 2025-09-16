using System;
using System.Collections.Generic;

using Trident.Data;
using Trident.Domain;
using Trident.SourceGeneration.Attributes;

namespace SE.Shared.Domain.EmailTemplates;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Operations, nameof(DocumentType), nameof(Databases.DocumentTypes.EmailTemplateEvent), PartitionKeyType.WellKnown)]
[Discriminator(nameof(EntityType), Databases.Discriminators.EmailTemplateEvent)]
[GenerateManager]
[GenerateProvider]
[GenerateRepository]
public class EmailTemplateEventEntity : TTEntityBase
{
    public string Name { get; set; }

    [OwnedHierarchy]
    public List<EmailTemplateEventFieldEntity> Fields { get; set; }

    [OwnedHierarchy]
    public List<EmailTemplateEventAttachmentEntity> Attachments { get; set; }
}

public class EmailTemplateEventFieldEntity : OwnedEntityBase<Guid>
{
    public string UiToken { get; set; }

    public string RazorToken { get; set; }

    public string TooltipContent { get; set; }

    public string Key { get; set; }
}

public class EmailTemplateEventAttachmentEntity : OwnedEntityBase<Guid>
{
    public string Name { get; set; }
}
