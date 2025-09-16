using System;
using System.Collections.Generic;

using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Constants.SourceLocations;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Data;
using Trident.Domain;
using Trident.SourceGeneration.Attributes;

namespace SE.Shared.Domain.Entities.MaterialApproval;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Operations, nameof(DocumentType), nameof(MaterialApprovalEntity), PartitionKeyType.WellKnown)]
[Discriminator(nameof(EntityType), Containers.Discriminators.MaterialApproval)]
[GenerateProvider]
public class MaterialApprovalEntity : TTAuditableEntityBase, IFacilityRelatedEntity
{
    public bool IsActive { get; set; } = true;

    public DateTimeOffset? EndDate { get; set; }

    public CountryCode CountryCode { get; set; }

    public Guid LegalEntityId { get; set; }

    public string LegalEntity { get; set; }

    public string Description { get; set; }

    public string WLAFNumber { get; set; }

    public Guid SourceLocationId { get; set; }

    public string SourceLocation { get; set; }

    public string SourceLocationFormattedIdentifier { get; set; }

    public string SourceLocationUnformattedIdentifier { get; set; }

    public Guid? FacilityServiceSubstanceIndexId { get; set; }

    public SourceRegionEnum SourceRegion { get; set; }

    public string Facility { get; set; }

    public string SiteId { get; set; }

    public Guid FacilityServiceId { get; set; }

    public Guid? ServiceTypeId { get; set; }

    public string FacilityServiceNumber { get; set; }

    public string FacilityServiceName { get; set; }

    public string Stream { get; set; }

    public Guid SubstanceId { get; set; }

    public string SubstanceName { get; set; }

    public string WasteCodeName { get; set; }

    public Guid GeneratorId { get; set; }

    public string GeneratorName { get; set; }

    public Guid BillingCustomerId { get; set; }

    public string BillingCustomerName { get; set; }

    public Guid? BillingCustomerContactId { get; set; }

    public string BillingCustomerContact { get; set; }

    public string BillingCustomerContactAddress { get; set; }

    public bool BillingCustomerContactReceiveLoadSummary { get; set; }

    public string MaterialApprovalNumber { get; set; }

    public Guid ThirdPartyAnalyticalCompanyId { get; set; }

    public string ThirdPartyAnalyticalCompanyName { get; set; }

    public Guid? ThirdPartyAnalyticalCompanyContactId { get; set; }

    public string ThirdPartyAnalyticalCompanyContact { get; set; }

    public bool ThirdPartyAnalyticalContactReceiveLoadSummary { get; set; }

    public Guid TruckingCompanyId { get; set; }

    public string TruckingCompanyName { get; set; }

    public Guid? TruckingCompanyContactId { get; set; }

    public WellClassifications? WellClassification { get; set; }

    public string TruckingCompanyContact { get; set; }

    public bool TruckingCompanyContactReceiveLoadSummary { get; set; }

    public string RigNumber { get; set; }

    public HazardousClassification HazardousNonhazardous { get; set; }

    public bool LoadSummaryReport { get; set; }

    public bool AnalyticalExpiryAlertActive { get; set; }

    public List<ApplicantSignatoryEntity> ApplicantSignatories { get; set; } = new();

    public List<LoadSummaryReportRecipientEntity> LoadSummaryReportRecipients { get; set; } = new();

    public bool AnalyticalExpiryEmailActive { get; set; }

    public DateTimeOffset AnalyticalExpiryDate { get; set; }

    public DateTimeOffset? AnalyticalExpiryDatePrevious { get; set; }

    public DateTimeOffset? ApproximateDeliveryDate { get; set; }

    public bool AnalyticalFailed { get; set; }

    public bool ActivateAutofillTareWeight { get; set; }

    public double AccumulatedTonnage { get; set; }

    public string DisposalUnits { get; set; }

    public long? DisposalEstimateAmount { get; set; }

    public DateTimeOffset SignatureDate { get; set; }

    public Guid ApplicantSignatoriesInfo { get; set; }

    public string SecureRepresentative { get; set; }

    public string SecureRepresentativeId { get; set; }

    public string ScaleOperatorNotes { get; set; }

    public bool IncludeSupportingDocLink { get; set; }

    public string SupportingDocLink { get; set; }

    public string LabIdNumber { get; set; }

    public bool AdditionalServiceAdded { get; set; }

    public Guid AdditionalService { get; set; }

    public string AdditionalServiceName { get; set; }

    public string TenormWasteHaulerPermitNumber { get; set; }

    public string GeneratorRepresentative { get; set; }

    public Guid GeneratorRepresenativeId { get; set; }

    public LoadSummaryReportFrequency LoadSummaryReportFrequency { get; set; }

    public DayOfWeek LoadSummaryReportFrequencyWeekDay { get; set; }

    public int? LoadSummaryReportFrequencyMonthlyDate { get; set; }

    public string AFE { get; set; }

    public string PO { get; set; }

    public string EDICode { get; set; }

    public DownHoleType DownHoleType { get; set; }

    public bool EnableEndOfJobInvoicing { get; set; }

    public Guid FacilityId { get; set; }
}

public class LoadSummaryReportRecipientEntity : OwnedEntityBase<Guid>
{
    public Guid AccountContactId { get; set; }

    public bool ReceiveLoadSummary { get; set; }

    public string ReportRecipientName { get; set; }

    public string AccountName { get; set; }

    public string JobTitle { get; set; }

    public string PhoneNumber { get; set; }

    public string Email { get; set; }
}

public class ApplicantSignatoryEntity : OwnedEntityBase<Guid>
{
    public Guid AccountContactId { get; set; }

    public bool ReceiveLoadSummary { get; set; }

    public string AccountName { get; set; }

    public string SignatoryName { get; set; }

    public string JobTitle { get; set; }

    public string PhoneNumber { get; set; }

    public string Email { get; set; }

    public bool? IsViewOnly { get; set; }
}
