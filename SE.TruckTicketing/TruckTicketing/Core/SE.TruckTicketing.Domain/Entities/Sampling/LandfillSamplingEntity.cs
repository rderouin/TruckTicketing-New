using System;
using System.Collections.Generic;

using SE.Shared.Domain;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Data;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Domain.Entities.Sampling;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Operations, nameof(DocumentType), nameof(LandfillSamplingEntity), PartitionKeyType.WellKnown)]
[Discriminator(nameof(EntityType), Databases.Discriminators.LandfillSampling)]
[GenerateManager]
[GenerateProvider]
[GenerateRepository]
public class LandfillSamplingEntity : TTEntityBase
{
    public Guid FacilityId { get; set; }
    
    public Guid SamplingRuleId { get; set; }
    
    public SamplingRuleType SamplingRuleType { get; set; }
    
    public string Threshold { get; set; }
    
    public string WarningThreshold { get; set; }
    
    public string Value { get; set; }
    
    public string ProductNumberFilter { get; set; }
    
    public List<string> WellClassificationFilters { get; set; } = new();

    public DateTime? TimeOfLastLoadSample { get; set; }
}
