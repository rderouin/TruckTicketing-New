using System;

using Trident.Data;
using Trident.SourceGeneration.Attributes;

namespace SE.Shared.Domain.Entities.Acknowledgement;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Operations, nameof(DocumentType), Databases.DocumentTypes.Acknowledgement, PartitionKeyType.Composite)]
[Discriminator(nameof(EntityType), Databases.Discriminators.Acknowledgement)]
[GenerateManager]
[GenerateProvider]
[GenerateRepository]
public class AcknowledgementEntity : TTAuditableEntityBase, IHaveCompositePartitionKey
{
    public Guid ReferenceEntityId { get; set; }

    public string Status { get; set; }

    public string AcknowledgedBy { get; set; }

    public DateTimeOffset? AcknowledgeAt { get; set; }

    public void InitPartitionKey(string customPartitionKey = null)
    {
        DocumentType ??= customPartitionKey ?? $"{Databases.DocumentTypes.Acknowledgement}|{ReferenceEntityId}";
    }
}
