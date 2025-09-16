using System;
using System.Collections.Generic;

using SE.Shared.Common.Extensions;
using SE.Shared.Common.Lookups;

namespace SE.TruckTicketing.Contracts.Models.Operations;

public class AdditionalServicesConfiguration : GuidApiModelBase, IFacilityRelatedModel
{
    public string Name { get; set; }

    public string SiteId { get; set; }

    public Guid LegalEntityId { get; set; }

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

    public bool IsActive { get; set; } = true;

    public List<AdditionalServicesConfigurationMatchPredicate> MatchCriteria { get; set; } = new();

    public List<AdditionalServicesConfigurationAdditionalService> AdditionalServices { get; set; } = new();

    public Guid FacilityId { get; set; }
}

public class AdditionalServicesConfigurationMatchPredicate : GuidApiModelBase
{
    public string ReferenceId => Id.ToReferenceId();

    public MatchPredicateValueState WellClassificationState { get; set; } = MatchPredicateValueState.Any;

    public WellClassifications WellClassification { get; set; }

    public MatchPredicateValueState SourceIdentifierValueState { get; set; } = MatchPredicateValueState.Any;

    public Guid? SourceLocationId { get; set; }

    public string SourceIdentifier { get; set; }

    public Guid? FacilityServiceSubstanceId { get; set; }

    public MatchPredicateValueState SubstanceValueState { get; set; } = MatchPredicateValueState.Any;

    public Guid? SubstanceId { get; set; }

    public string SubstanceName { get; set; }

    public bool IsEnabled { get; set; } = true;
}

public class AdditionalServicesConfigurationAdditionalService : GuidApiModelBase
{
    public string ReferenceId => Id.ToReferenceId();

    public Guid ProductId { get; set; }

    public string Name { get; set; }

    public string Number { get; set; }

    public double Quantity { get; set; }

    public string UnitOfMeasure { get; set; }

    public bool PullQuantityFromTicket { get; set; }
}
