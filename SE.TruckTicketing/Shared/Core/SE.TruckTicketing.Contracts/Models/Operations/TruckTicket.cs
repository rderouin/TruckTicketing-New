using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using SE.Shared.Common.Extensions;
using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Contracts.Constants.SpartanProductParameters;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Sampling;

namespace SE.TruckTicketing.Contracts.Models.Operations;

public class TruckTicket : GuidApiModelBase, INotifyPropertyChanged, IFacilityRelatedModel
{
    private DateTimeOffset? _loadDate;

    private double _netWeight;

    private double _totalVolume;

    private WellClassifications _wellClassification;

    public bool IsBillingInfoOverridden { get; set; }

    public Guid? BillingConfigurationId { get; set; }

    public Guid BillingCustomerId { get; set; }

    public string LegalEntity { get; set; }

    public Guid LegalEntityId { get; set; }

    public string UnitOfMeasure { get; set; }

    public string FacilityLocationCode { get; set; }

    public string FacilityStreamRegulatoryCode { get; set; }

    public string SourceLocationCode { get; set; }

    public double SalesTotalValue { get; set; }

    public string BillingCustomerName { get; set; }

    public string BillOfLading { get; set; }

    public string ClassNumber { get; set; }

    public CountryCode CountryCode { get; set; }

    public string Destination { get; set; }

    public string FacilityName { get; set; }

    public string SiteId { get; set; }

    public FacilityType? FacilityType { get; set; }

    public Guid? FacilityServiceSubstanceId { get; set; }

    public Guid GeneratorId { get; set; }

    public string GeneratorName { get; set; }

    public double GrossWeight { get; set; }

    public bool IsDeleted { get; set; }

    public bool? IsDow { get; set; }

    public string Level { get; set; }

    public DateTimeOffset? LoadDate { get => _loadDate; set => SetField(ref _loadDate, value); }

    public LocationOperatingStatus LocationOperatingStatus { get; set; }

    public string ManifestNumber { get; set; }

    public Guid? MaterialApprovalId { get; set; }

    public string MaterialApprovalNumber { get; set; }

    public double NetWeight { get => _netWeight; set => SetField(ref _netWeight, value); }

    public double OilVolume { get; set; }

    public CutEntryMethod CutEntryMethod { get; set; }

    public double? LoadVolume { get; set; }

    public double OilVolumePercent { get; set; }

    public string Quadrant { get; set; }

    public bool RequireSample { get; set; }

    public Guid SaleOrderId { get; set; }

    public string SaleOrderNumber { get; set; }

    public bool? IsServiceOnlyTicket { get; set; }

    public Guid? FacilityServiceId { get; set; }

    public Guid? ServiceTypeId { get; set; }

    public string ServiceType { get; set; }

    public Stream Stream { get; set; }

    public LoadConfirmationFrequency? LoadConfirmationFrequency { get; set; }

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

    public Guid SubstanceId { get; set; }

    public string SubstanceName { get; set; }

    public double TareWeight { get; set; }

    public string TicketNumber { get; set; }

    public string WiqNumber { get; set; }

    public bool? EnforceSpartanFacilityServiceLock { get; set; }

    public Guid? LockedSpartanFacilityServiceId { get; set; }

    public string LockedSpartanFacilityServiceName { get; set; }

    public DateTimeOffset? TimeIn { get; set; }

    public DateTimeOffset? TimeOut { get; set; }

    public string Tnorms { get; set; }

    public double TotalVolume { get => _totalVolume; set => SetField(ref _totalVolume, value); }

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

    public WellClassifications WellClassification { get => _wellClassification; set => SetField(ref _wellClassification, value); }

    public bool AdditionalServicesEnabled { get; set; }

    public List<TruckTicketAdditionalService> AdditionalServices { get; set; }

    public List<Signatory> Signatories { get; set; } = new();

    public BillingContact BillingContact { get; set; } = new();

    public List<EDIFieldValue> EdiFieldValues { get; set; } = new();

    public bool UploadFieldTicket { get; set; }

    public CreditStatus? CustomerCreditStatus { get; set; }

    public WatchListStatus? CustomerWatchListStatus { get; set; }

    public string CreatedBy { get; set; }

    public string CreatedById { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string UpdatedBy { get; set; }

    public string UpdatedById { get; set; }

    public string WasteCode { get; set; }

    public List<TruckTicketAttachment> Attachments { get; set; } = new();

    public string Acknowledgement { get; set; }

    public string LastScaleOperatorAcknowledgementNote { get; set; }

    public bool? IsRedFlagged { get; set; }

    public DowNonDow DowNonDow { get; set; }

    public string VoidReason { get; set; }

    public string HoldReason { get; set; }

    public string OtherReason { get; set; }

    public bool LandfillSampled { get; set; }

    public DateTimeOffset? LandfillSampledTime { get; set; }

    public LandfillSamplingStatusCheckDto LandfillSamplingStatus { get; set; }

    public TruckTicketDensityConversionParams ConversionParameters { get; set; }

    public bool IsEdiValid { get; set; }

    public VolumeChangeReason VolumeChangeReason { get; set; }

    public string VolumeChangeReasonText { get; set; }

    public TruckTicketType TruckTicketType { get; set; }

    public bool IsTrackManualVolumeChangesType => TruckTicketType == TruckTicketType.SP || (TruckTicketType == TruckTicketType.WT && Status == TruckTicketStatus.Approved);

    public DateTime? EffectiveDate { get; set; }

    public string BillingConfigurationName { get; set; }

    public bool ResetVolumeFields { get; set; } = true;

    public Guid? ParentTicketID { get; set; }

    public List<string> SalesLineIds { get; set; } = new();

    public string VersionTag { get; set; }

    public bool IsReadyToTakeSample { get; set; }

    public Class ServiceTypeClass { get; set; }

    public Guid? LoadSamplingId { get; set; }

    public DateTime? TimeOfLastSampleCountdownUpdate { get; set; }

    public Guid FacilityId { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class TruckTicketAdditionalService : GuidApiModelBase
{
    public Guid ProductId { get; set; }

    public bool IsPrimarySalesLine { get; set; }

    public string AdditionalServiceNumber { get; set; }

    public string AdditionalServiceName { get; set; }

    public double AdditionalServiceQuantity { get; set; }
}

public class Signatory : GuidApiModelBase
{
    public Guid AccountId { get; set; }

    public Guid AccountContactId { get; set; }

    public bool IsAuthorized { get; set; }

    public string ContactName { get; set; }

    public string ContactAddress { get; set; }

    public string ContactEmail { get; set; }

    public string ContactPhoneNumber { get; set; }
}

public class BillingContact : GuidApiModelBase
{
    public Guid? AccountContactId { get; set; }

    public string Name { get; set; }

    public string Address { get; set; }

    public string PhoneNumber { get; set; }

    public string Email { get; set; }
}

public class TruckTicketAttachment : GuidApiModelBase
{
    public string ReferenceId => Id.ToReferenceId();

    public string Container { get; set; }

    public string File { get; set; }

    public string Path { get; set; }

    public string ContentType { get; set; }

    public bool IsUploaded { get; set; }

    public AttachmentType AttachmentType { get; set; }
}

public class TruckTicketAttachmentUpload
{
    public TruckTicketAttachment Attachment { get; set; }

    public string Uri { get; set; }
}

public class TruckTicketDensityConversionParams
{
    public double? GrossWeight { get; set; }

    public double? TareWeight { get; set; }

    public double? MidWeight { get; set; }

    public double? OilCutPercentage { get; set; }

    public double? WaterCutPercentage { get; set; }

    public double? SolidsCutPercentage { get; set; }

    public double? OilWeight { get; set; }

    public double? WaterWeight { get; set; }

    public double? SolidsWeight { get; set; }

    public double? OilConversionFactor { get; set; }

    public double? WaterConversionFactor { get; set; }

    public CutEntryMethod CutEntryMethod { get; set; }

    public double? SolidsConversionFactor { get; set; }
}
