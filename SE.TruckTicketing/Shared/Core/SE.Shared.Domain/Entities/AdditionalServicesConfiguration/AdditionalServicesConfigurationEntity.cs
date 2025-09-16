using System;
using System.Collections.Generic;

using SE.Shared.Common.Lookups;

using Trident.Data;
using Trident.Domain;
using Trident.SourceGeneration.Attributes;

namespace SE.Shared.Domain.Entities.AdditionalServicesConfiguration;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Operations, nameof(DocumentType), Databases.DocumentTypes.AdditionalServicesConfiguration, PartitionKeyType.WellKnown)]
[Discriminator(nameof(EntityType), Databases.Discriminators.AdditionalServicesConfiguration)]
[GenerateManager]
[GenerateProvider]
public class AdditionalServicesConfigurationEntity : TTAuditableEntityBase, IFacilityRelatedEntity
{
    public string Name { get; set; }

    public string SiteId { get; set; }

    public string FacilityName { get; set; }

    public FacilityType FacilityType { get; set; }

    public Guid CustomerId { get; set; }

    public string CustomerName { get; set; }

    public bool ApplyZeroDollarPrimarySalesLine { get; set; }

    public bool ApplyZeroTotalVolume { get; set; }

    public bool ApplyZeroOilVolume { get; set; }

    public bool ApplyZeroWaterVolume { get; set; }

    public bool ApplyZeroSolidsVolume { get; set; }

    public bool PullVolumeQty { get; set; }

    public bool IsActive { get; set; }

    [OwnedHierarchy]
    public List<AdditionalServicesConfigurationMatchPredicateEntity> MatchCriteria { get; set; } = new();

    [OwnedHierarchy]
    public List<AdditionalServicesConfigurationAdditionalServiceEntity> AdditionalServices { get; set; } = new();

    public Guid FacilityId { get; set; }
}

public class AdditionalServicesConfigurationMatchPredicateEntity : OwnedEntityBase<Guid>
{
    public MatchPredicateValueState WellClassificationState { get; set; }

    public WellClassifications WellClassification { get; set; }

    public MatchPredicateValueState SourceIdentifierValueState { get; set; }

    public Guid? SourceLocationId { get; set; }

    public string SourceIdentifier { get; set; }

    public Guid? FacilityServiceSubstanceId { get; set; }

    public MatchPredicateValueState SubstanceValueState { get; set; }

    public Guid? SubstanceId { get; set; }

    public string SubstanceName { get; set; }

    public bool IsEnabled { get; set; } = true;
}

public class AdditionalServicesConfigurationAdditionalServiceEntity : OwnedEntityBase<Guid>
{
    public Guid ProductId { get; set; }

    public string Name { get; set; }

    public string Number { get; set; }

    public double Quantity { get; set; }

    public string UnitOfMeasure { get; set; }

    public bool? PullQuantityFromTicket { get; set; }
}
