using System;

using Trident.Data;
using Trident.SourceGeneration.Attributes;

namespace SE.Shared.Domain.Entities.Account;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Operations, nameof(DocumentType), Databases.DocumentTypes.AccountContact, PartitionKeyType.Composite)]
[Discriminator(Databases.Discriminators.AccountContactReferenceIndex, Property = nameof(EntityType))]
[GenerateManager]
[GenerateProvider]
[GenerateRepository]
public class AccountContactReferenceIndexEntity : TTEntityBase, IHaveCompositePartitionKey
{
    public Guid ReferenceEntityId { get; set; }

    public string ReferenceEntityName { get; set; }

    public Guid? AccountContactId { get; set; }

    public Guid AccountId { get; set; }

    public bool? IsDisabled { get; set; }

    public void InitPartitionKey(string customPartitionKey = null)
    {
        DocumentType ??= customPartitionKey ?? GetPartitionKey(AccountId);
    }

    public static string GetPartitionKey(Guid accountId)
    {
        return $"{Databases.DocumentTypes.AccountContact}|{accountId}";
    }
}
