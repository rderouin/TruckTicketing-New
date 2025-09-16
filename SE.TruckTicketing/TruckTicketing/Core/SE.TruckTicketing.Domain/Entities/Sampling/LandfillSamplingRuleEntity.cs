using System.Collections.Generic;

using SE.Shared.Domain;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Data;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Domain.Entities.Sampling;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Operations, nameof(DocumentType), nameof(LandfillSamplingRuleEntity), PartitionKeyType.WellKnown)]
[Discriminator(nameof(EntityType), Databases.Discriminators.LandfillSamplingRule)]
[GenerateManager]
[GenerateProvider]
[GenerateRepository]
public class LandfillSamplingRuleEntity : TTEntityBase
{
    public SamplingRuleType SamplingRuleType { get; set; }

    public StateProvince Province { get; set; }

    public CountryCode CountryCode { get; set; }

    public string Threshold { get; set; }

    public string WarningThreshold { get; set; }

    public string ProductNumberFilter { get; set; }

    public List<string> WellClassificationFilters { get; set; } = new();
}

