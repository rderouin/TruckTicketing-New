using System;
using System.Collections.Generic;

using JetBrains.Annotations;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.EDIFieldValue;
using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Constants.SpartanProductParameters;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Data;
using Trident.Domain;

namespace SE.Shared.Domain.Entities.TruckTicket;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Operations, nameof(DocumentType), nameof(TruckTicketEntity), PartitionKeyType.Composite)]
[Discriminator(nameof(EntityType), Containers.Discriminators.TruckTicket)]
public class TruckTicketEntity : TTAuditableEntityBase, ISupportOptimisticConcurrentUpdates, IFacilityRelatedEntity, IHaveCompositePartitionKey
{
    public string LegalEntity { get; set; }

    public Guid? LegalEntityId { get; set; }

    public string UnitOfMeasure { get; set; }

    public string FacilityLocationCode { get; set; }

    public string FacilityStreamRegulatoryCode { get; set; }

    public string SourceLocationCode { get; set; }

    public double? SalesTotalValue { get; set; }

    public LoadConfirmationFrequency? LoadConfirmationFrequency { get; set; }

    public Guid? BillingConfigurationId { get; set; }

    public Guid BillingCustomerId { get; set; }

    public string BillingCustomerName { get; set; }

    public string BillOfLading { get; set; }

    public string ClassNumber { get; set; }

    public CountryCode CountryCode { get; set; }

    public Guid CustomerId { get; set; }

    public string CustomerName { get; set; }

    public DateTimeOffset Date { get; set; }

    public string Destination { get; set; }

    public string FacilityName { get; set; }

    public FacilityType? FacilityType { get; set; }

    public Guid FacilityServiceSubstanceId { get; set; }

    public Guid GeneratorId { get; set; }

    public string GeneratorName { get; set; }

    public double GrossWeight { get; set; }

    public bool IsDeleted { get; set; }

    public bool? IsDow { get; set; }

    public DowNonDow DowNonDow { get; set; } = DowNonDow.Undefined;

    public string Level { get; set; }

    public DateTimeOffset? LoadDate { get; set; }

    public LocationOperatingStatus LocationOperatingStatus { get; set; }

    public string ManifestNumber { get; set; }

    public Guid MaterialApprovalId { get; set; }

    public string MaterialApprovalNumber { get; set; }

    public double NetWeight { get; set; }

    public CutEntryMethod? CutEntryMethod { get; set; }

    public double? LoadVolume { get; set; }

    public double OilVolume { get; set; }

    public double OilVolumePercent { get; set; }

    public string Quadrant { get; set; }

    public bool RequireSample { get; set; }

    public Guid SaleOrderId { get; set; }

    public string SaleOrderNumber { get; set; }

    public string ServiceType { get; set; }

    public Stream Stream { get; set; }

    public double SolidVolume { get; set; }

    public double SolidVolumePercent { get; set; }

    public TruckTicketSource Source { get; set; }

    public string SourceLocationFormatted { get; set; }

    public string SourceLocationUnformatted { get; set; }

    public Guid SourceLocationId { get; set; }

    public string SourceLocationName { get; set; }

    public string SpartanProductParameterDisplay { get; set; }

    public Guid SpartanProductParameterId { get; set; }

    public TruckTicketStatus Status { get; set; }

    public bool? IsServiceOnlyTicket { get; set; }

    public Guid? FacilityServiceId { get; set; }

    public Guid? LockedSpartanFacilityServiceId { get; set; }

    public string LockedSpartanFacilityServiceName { get; set; }

    public Guid? ServiceTypeId { get; set; }

    public Guid SubstanceId { get; set; }

    public string SubstanceName { get; set; }

    public string WasteCode { get; set; }

    public double TareWeight { get; set; }

    public string TicketNumber { get; set; }

    public string WiqNumber { get; set; }

    public DateTimeOffset? TimeIn { get; set; }

    public DateTimeOffset? TimeOut { get; set; }

    public string Tnorms { get; set; }

    public double TotalVolume { get; set; }

    public double TotalVolumePercent { get; set; }

    public string TrackingNumber { get; set; }

    public string TrailerNumber { get; set; }

    public Guid TruckingCompanyId { get; set; }

    public string TruckingCompanyName { get; set; }

    public string TruckNumber { get; set; }

    public double UnloadOilDensity { get; set; }

    public string UnNumber { get; set; }

    public TruckTicketValidationStatus ValidationStatus { get; set; }

    public double WaterVolume { get; set; }

    public double WaterVolumePercent { get; set; }

    public WellClassifications WellClassification { get; set; }

    public bool AdditionalServicesEnabled { get; set; }

    public string Acknowledgement { get; set; }

    public List<TruckTicketAdditionalServiceEntity> AdditionalServices { get; set; }

    [OwnedHierarchy]
    public List<SignatoryEntity> Signatories { get; set; } = new();

    [OwnedHierarchy]
    public BillingContactEntity BillingContact { get; set; } = new();

    [OwnedHierarchy]
    public List<EDIFieldValueEntity> EdiFieldValues { get; set; } = new();

    public bool UploadFieldTicket { get; set; }

    public List<TruckTicketAttachmentEntity> Attachments { get; set; } = new();

    public bool? IsRedFlagged { get; set; }

    public bool LandfillSampled { get; set; }

    public DateTimeOffset? LandfillSampledTime { get; set; }

    public string HoldReason { get; set; }

    public string OtherReason { get; set; }

    public string VoidReason { get; set; }

    [CanBeNull]
    public TruckTicketDensityConversionParamsEntity ConversionParameters { get; set; }

    public bool IsEdiValid { get; set; }

    public VolumeChangeReason VolumeChangeReason { get; set; }

    public string VolumeChangeReasonText { get; set; }

    public TruckTicketType TruckTicketType { get; set; }

    public Guid? ParentTicketID { get; set; }

    public string SiteId { get; set; }

    public PrimitiveCollection<string> SalesLineIds { get; set; }

    public CreditStatus? CustomerCreditStatus { get; set; }

    public WatchListStatus? CustomerWatchListStatus { get; set; }

    public bool? EnforceSpartanFacilityServiceLock { get; set; }

    public int? AdditionalServicesQty { get; set; }

    public bool? IsReadyToTakeSample => GrossWeight > 0 && TareWeight > 0 && TareWeight <= GrossWeight;

    public DateTime? EffectiveDate { get; set; }

    public string BillingConfigurationName { get; set; }

    public ReportAsCutTypes? ReportAsCutType { get; set; }

    public Class? ServiceTypeClass { get; set; }

    public Guid FacilityId { get; set; }

    public Guid? LoadSamplingId { get; set; }

    public DateTime? TimeOfLastSampleCountdownUpdate { get; set; }

public void InitPartitionKey(string customPartitionKey = null)
    {
        DocumentType ??= customPartitionKey ?? $"{nameof(TruckTicketEntity)}|{DateTime.Today:MMyyyy}";
    }

    public string VersionTag { get; set; }

    public IEnumerable<object> GetFieldsToCompare()
    {
        return new object[]
        {
            Status,
            FacilityId,
            WellClassification,
            SourceLocationId,
            LoadDate,
            UnloadOilDensity,
            DowNonDow,
            FacilityServiceSubstanceId,
            TruckingCompanyId,
            BillOfLading,
            TruckNumber,
            ManifestNumber,
            LoadVolume,
            OilVolume,
            WaterVolume,
            SolidVolume,
            OilVolumePercent,
            WaterVolumePercent,
            SolidVolumePercent,
            TimeIn,
            TimeOut,
            BillingConfigurationId,
            MaterialApprovalId,
            TrailerNumber,
            GrossWeight,
            TareWeight,
            Quadrant,
            Level,
            SpartanProductParameterId,
        };
    }

    public string GetLockLeaseBlobName()
    {
        return $"truckticket-{Id}.lck";
    }
}

public class TruckTicketAdditionalServiceEntity : OwnedEntityBase<Guid>
{
    public Guid ProductId { get; set; }

    public bool IsPrimarySalesLine { get; set; }

    public string AdditionalServiceNumber { get; set; }

    public string AdditionalServiceName { get; set; }

    public double AdditionalServiceQuantity { get; set; }
}

public class SignatoryEntity : OwnedEntityBase<Guid>
{
    public Guid AccountId { get; set; }

    public Guid AccountContactId { get; set; }

    public bool IsAuthorized { get; set; }

    public string ContactName { get; set; }

    public string ContactAddress { get; set; }

    public string ContactEmail { get; set; }

    public string ContactPhoneNumber { get; set; }
}

public class BillingContactEntity : OwnedEntityBase<Guid>
{
    public Guid? AccountContactId { get; set; }

    public string Name { get; set; }

    public string Address { get; set; }

    public string PhoneNumber { get; set; }

    public string Email { get; set; }
}

public class TruckTicketAttachmentEntity : OwnedEntityBase<Guid>
{
    public string Container { get; set; }

    public string File { get; set; }

    public string Path { get; set; }

    public string ContentType { get; set; }

    public bool IsUploaded { get; set; }

    public AttachmentType AttachmentType { get; set; } = AttachmentType.External;
}

public class TruckTicketDensityConversionParamsEntity
{
    public double? GrossWeight { get; set; }

    public double? TareWeight { get; set; }

    public double? MidWeight { get; set; }

    public double? OilCutPercentage { get; set; }

    public double? WaterCutPercentage { get; set; }

    public double? SolidsCutPercentage { get; set; }

    public double? OilConversionFactor { get; set; }

    public double? WaterConversionFactor { get; set; }

    public double? SolidsConversionFactor { get; set; }

    public CutEntryMethod? CutEntryMethod { get; set; }

    public double? OilWeight { get; set; }

    public double? WaterWeight { get; set; }

    public double? SolidsWeight { get; set; }
}
