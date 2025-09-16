using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Data;
using Trident.Domain;

namespace SE.Shared.Domain.Entities.Facilities;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Operations, nameof(DocumentType), nameof(FacilityEntity), PartitionKeyType.WellKnown)]
[Discriminator(nameof(EntityType), Databases.Discriminators.Facility)]
public class FacilityEntity : TTEntityBase, ITTSearchableIdBase
{
    public string SiteId { get; set; }

    public string Name { get; set; }

    public FacilityType Type { get; set; }

    public string LegalEntity { get; set; }

    public string AdminEmail { get; set; }

    public CountryCode CountryCode { get; set; }

    public Guid LegalEntityId { get; set; }

    public string SourceLocation { get; set; }

    public DateTime? LastIntegrationDateTime { get; set; }

    public string Pipeline { get; set; }

    public string Terminaling { get; set; }

    public string Treating { get; set; }

    public string Waste { get; set; }

    public string Water { get; set; }

    public string LocationCode { get; set; }

    public string TimeZone { get; set; }

    public bool EnableTareWeightMessages { get; set; }

    public StateProvince Province { get; set; }

    public bool IsActive { get; set; }

    public string BusinessUnitId { get; set; }

    public List<TruckTicketing.Contracts.Models.TicketType> TicketTypes { get; set; }

    public bool ShowClassNumber { get; set; }

    public bool ShowUnNumber { get; set; }

    public bool ShowMaterialApproval { get; set; }

    public bool ShowConversionCalculator { get; set; }

    public bool ShowDestination { get; set; }

    public bool? FieldTicketAutoApproval { get; set; }

    public DateTimeOffset? OperatingDayCutOffTime { get; set; }

    public DowNonDow? DowNonDow { get; set; }

    public bool? ShowHazNonHaz { get; set; }

    public bool? ShowSourceRegion { get; set; }

    [OwnedHierarchy]
    public List<PreSetDensityConversionParamsEntity> WeightConversionParameters { get; set; }

    [OwnedHierarchy]
    public List<PreSetDensityConversionParamsEntity> MidWeightConversionParameters { get; set; }

    public int? TareWeightValidityDays { get; set; }

    public string InvoiceContactFirstName { get; set; }

    public string InvoiceContactLastName { get; set; }

    public string InvoiceContactPhoneNumber { get; set; }

    public string InvoiceContactEmailAddress { get; set; }

    public bool? SpartanActive { get; set; }

    public string Division { get; set; }

    public string SearchableId { get; set; }

    public bool? EnableTareWeight { get; set; }

    public Expression<Func<FacilityEntity, bool>> GetFacilityQueryFilterExpression(Guid facilityId)
    {
        return facility => facility.Id == facilityId;
    }
}

public class PreSetDensityConversionParamsEntity : OwnedEntityBase<Guid>
{
    public bool IsDefaultFacilityDefaultDensity { get; set; }

    public bool IsEnabled { get; set; }

    public Guid? SourceLocationId { get; set; }

    public string SourceLocationIdentifier { get; set; }

    public string SourceLocationName { get; set; }

    public string SourceLocationGeneratorName { get; set; }

    public PrimitiveCollection<Guid> FacilityServiceId { get; set; }

    public List<string> FacilityServiceName { get; set; }

    public DateTimeOffset StartDate { get; set; }

    public DateTimeOffset? EndDate { get; set; }

    public double? OilConversionFactor { get; set; }

    public double? WaterConversionFactor { get; set; }

    public double? SolidsConversionFactor { get; set; }
}
