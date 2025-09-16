using SE.Shared.Domain;

using Trident.Data;
using Trident.SourceGeneration.Attributes;

namespace SE.BillingService.Domain.Entities.InvoiceExchange;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Billing, nameof(DocumentType), Databases.DocumentTypes.SourceModelField, PartitionKeyType.WellKnown)]
[Discriminator(nameof(EntityType), Databases.Discriminators.SourceModelField)]
[GenerateManager]
[GenerateProvider]
[GenerateRepository]
public class SourceModelFieldEntity : TTEntityBase
{
    public string JsonPath { get; set; }

    public string DataType { get; set; }

    public string EntityName { get; set; }

    public string FieldName { get; set; }

    public bool IsGlobal { get; set; }
}
