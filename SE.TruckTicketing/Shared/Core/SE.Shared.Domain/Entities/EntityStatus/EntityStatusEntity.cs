using System;

using Trident.Contracts.Api;
using Trident.Data;
using Trident.SourceGeneration.Attributes;

namespace SE.Shared.Domain.Entities.EntityStatus;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Operations, nameof(DocumentType), Databases.DocumentTypes.EntityStatus, PartitionKeyType.Composite)]
[Discriminator(nameof(EntityType), Databases.Discriminators.EntityStatus)]
[GenerateRepository]
[GenerateProvider]
[GenerateManager]
public class EntityStatusEntity : TTEntityBase, IHaveCompositePartitionKey
{
    public string ReferenceEntityType { get; set; }

    public CompositeKey<Guid> ReferenceEntityKey { get; set; }

    public string Status { get; set; }

    public void InitPartitionKey(string customPartitionKey = null)
    {
        DocumentType ??= customPartitionKey ?? GetPartitionKey(ReferenceEntityType);
    }

    public static string GetPartitionKey(string referenceEntityType)
    {
        return $"{Databases.DocumentTypes.EntityStatus}|{referenceEntityType}";
    }
}
