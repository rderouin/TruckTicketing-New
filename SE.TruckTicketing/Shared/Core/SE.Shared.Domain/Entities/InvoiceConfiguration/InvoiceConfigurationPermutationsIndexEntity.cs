using System;

using Trident.Data;
using Trident.SourceGeneration.Attributes;

namespace SE.Shared.Domain.Entities.InvoiceConfiguration;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Billing, nameof(DocumentType), Databases.DocumentTypes.InvoiceConfiguration, PartitionKeyType.Composite)]
[Discriminator(nameof(EntityType), Databases.Discriminators.InvoiceConfigurationPermutations)]
[GenerateManager]
[GenerateProvider]
[GenerateRepository]
public class InvoiceConfigurationPermutationsIndexEntity : TTEntityBase, IHaveCompositePartitionKey
{
    public Guid InvoiceConfigurationId { get; set; }

    public Guid CustomerId { get; set; }

    public string Name { get; set; }

    public string Number { get; set; }

    public string SourceLocation { get; set; }

    public string ServiceType { get; set; }

    public string WellClassification { get; set; }

    public string Substance { get; set; }

    public string Facility { get; set; }

    public void InitPartitionKey(string customPartitionKey = null)
    {
        DocumentType ??= customPartitionKey ?? GetPartitionKey(CustomerId);
    }

    public static string GetPartitionKey(Guid customerId)
    {
        return $"{Databases.DocumentTypes.InvoiceConfiguration}|{customerId}";
    }
}
