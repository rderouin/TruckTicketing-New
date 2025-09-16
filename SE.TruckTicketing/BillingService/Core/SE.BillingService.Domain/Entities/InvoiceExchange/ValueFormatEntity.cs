using SE.Shared.Domain;

using Trident.Data;
using Trident.SourceGeneration.Attributes;

namespace SE.BillingService.Domain.Entities.InvoiceExchange;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Billing, nameof(DocumentType), Databases.DocumentTypes.ValueFormat, PartitionKeyType.WellKnown)]
[Discriminator(nameof(EntityType), Databases.Discriminators.ValueFormat)]
[GenerateManager]
[GenerateProvider]
[GenerateRepository]
public class ValueFormatEntity : TTEntityBase
{
    public string Name { get; set; }

    public string ValueExpression { get; set; }
    
    public string SourceType { get; set; }
}
