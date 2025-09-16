using SE.TruckTicketing.Contracts;

using Trident.Data;
using Trident.SourceGeneration.Attributes;

namespace SE.Shared.Domain.Entities.EDIValidationPatternLookup;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Billing, nameof(DocumentType), nameof(EDIValidationPatternLookupEntity), PartitionKeyType.WellKnown)]
[Discriminator(Containers.Discriminators.EDIValidationPatternLookup, Property = nameof(EntityType))]
[GenerateManager]
[GenerateProvider]
[GenerateRepository]
public class EDIValidationPatternLookupEntity : TTEntityBase
{
    public string Name { get; set; }

    public string Pattern { get; set; }

    public string ErrorMessage { get; set; }
}
