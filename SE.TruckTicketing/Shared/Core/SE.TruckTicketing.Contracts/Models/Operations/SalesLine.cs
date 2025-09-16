using System;
using System.Collections.Generic;

using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Contracts.Lookups;

namespace SE.TruckTicketing.Contracts.Models.Operations;

public class SalesLine : GuidApiModelBase, IFacilityRelatedModel
{
    public string SalesLineNumber { get; set; }

    public Guid TruckTicketId { get; set; }

    public string TruckTicketNumber { get; set; }

    public Guid ProductId { get; set; }

    public string ProductName { get; set; }

    public string ProductNumber { get; set; }

    public string Substance { get; set; }

    public double Quantity { get; set; }

    public double QuantityPercent { get; set; }

    public double GrossWeight { get; set; }

    public double TareWeight { get; set; }

    public string UnitOfMeasure { get; set; }

    public double? OriginalRate { get; set; }

    public double Rate { get; set; }

    public double TotalValue { get; set; }

    public bool IsRateOverridden { get; set; }

    public Guid? LoadConfirmationId { get; set; }

    public string LoadConfirmationNumber { get; set; }

    public Guid? InvoiceId { get; set; }

    public string ProformaInvoiceNumber { get; set; }

    public bool IsReversed { get; set; }

    public bool IsReversal { get; set; }

    public Guid? ReversedSalesLineId { get; set; }

    public Guid? ReversalSalesLineId { get; set; }

    public Guid SourceLocationId { get; set; }

    public string SourceLocationIdentifier { get; set; }

    public string SourceLocationFormattedIdentifier { get; set; }

    public Guid SourceLocationTypeId { get; set; }

    public string SourceLocationTypeName { get; set; }

    public WellClassifications WellClassification { get; set; }

    public Guid CustomerId { get; set; }

    public string CustomerName { get; set; }

    public Guid GeneratorId { get; set; }

    public string GeneratorName { get; set; }

    public Guid? BillingConfigurationId { get; set; }

    public string BillingConfigurationName { get; set; }

    public string FacilitySiteId { get; set; }

    public bool IsAdditionalService { get; set; }

    public bool IsUserAddedAdditionalServices { get; set; }

    public bool IsReadOnlyLine { get; set; }

    public bool IsCutLine { get; set; }

    public SalesLineCutType? CutType { get; set; }

    public Guid? MaterialApprovalId { get; set; }

    public string MaterialApprovalNumber { get; set; }

    public Guid? ServiceTypeId { get; set; }

    public string ServiceTypeName { get; set; }

    public Guid? TruckingCompanyId { get; set; }

    public string TruckingCompanyName { get; set; }

    public SalesLineStatus Status { get; set; }

    public string ManifestNumber { get; set; }

    public string BillOfLading { get; set; }

    public DowNonDow DowNonDow { get; set; }

    public DateTimeOffset TruckTicketDate { get; set; }

    public DateTime? TruckTicketEffectiveDate { get; set; }

    public List<EDIFieldValue> EdiFieldValues { get; set; } = new();

    public List<SalesLineAttachment> Attachments { get; set; }

    public string EDIFieldsValueString { get; set; }

    public AttachmentIndicatorType AttachmentIndicatorType { get; set; }

    public HasAttachments HasAttachments { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public bool IsEdiValid { get; set; }

    public string PriceChangeUserName { get; set; }

    public DateTimeOffset? PriceChangeDate { get; set; }

    public string ChangeComments { get; set; }

    public SalesLinePriceChangeReason ChangeReason { get; set; }

    public string Division { get; set; }

    public string BusinessUnit { get; set; }

    public string LegalEntity { get; set; }

    public string AccountNumber { get; set; }

    public string CustomerNumber { get; set; }

    public bool? AwaitingRemovalAcknowledgment { get; set; }

    public Guid? HistoricalInvoiceId { get; set; }

    public bool CanPriceBeRefreshed { get; set; }

    public Guid FacilityId { get; set; }
}

public class SalesLinePreviewRequest
{
    public Guid FacilityId { get; set; }

    public Guid BillingCustomerId { get; set; }

    public Guid FacilityServiceSubstanceIndexId { get; set; }

    public Guid SourceLocationId { get; set; }

    public WellClassifications WellClassification { get; set; }

    public DateTimeOffset? LoadDate { get; set; }

    public double GrossWeight { get; set; }

    public double TareWeight { get; set; }

    public double TotalVolume { get; set; }

    public double TotalVolumePercent { get; set; }

    public double SolidVolume { get; set; }

    public double SolidVolumePercent { get; set; }

    public double WaterVolume { get; set; }

    public double WaterVolumePercent { get; set; }

    public double OilVolume { get; set; }

    public double OilVolumePercent { get; set; }

    public Guid? MaterialApprovalId { get; set; }

    public string MaterialApprovalNumber { get; set; }

    public Guid? ServiceTypeId { get; set; }

    public string ServiceTypeName { get; set; }

    public Guid? TruckingCompanyId { get; set; }

    public string TruckingCompanyName { get; set; }

    public TruckTicket TruckTicket { get; set; }

    public Guid? PricingRuleId { get; set; }

    public bool UseNew { get; set; }
}

public class SalesLineAttachment : GuidApiModelBase
{
    public string Container { get; set; }

    public string File { get; set; }

    public string Path { get; set; }

    public AttachmentType? AttachmentType { get; set; }
}

public class SalesLinePriceRequest
{
    public Guid CustomerId { get; set; }

    public Guid FacilityId { get; set; }

    public DateTimeOffset TruckTicketDate { get; set; }

    public string ProductNumber { get; set; }

    public string SourceLocation { get; set; }
}

public class TruckTicketSalesPersistenceRequest : IFacilityRelatedModel
{
    public TruckTicket TruckTicket { get; set; }

    public List<SalesLine> SalesLines { get; set; }

    public Guid FacilityId => TruckTicket.FacilityId;
}

public class TruckTicketSalesPersistenceResponse
{
    public TruckTicket TruckTicket { get; set; }

    public List<SalesLine> SalesLines { get; set; }
}

public class TruckTicketAssignInvoiceRequest
{
    public TruckTicket TruckTicket { get; set; }

    public Guid BillingConfigurationId { get; set; }

    public double SalesTotalValue { get; set; }

    public int SalesLineCount { get; set; }
}

public class SalesLineResendAckRemovalRequest
{
    public List<SalesLine> SalesLines { get; set; }

    public bool IsPublishOnly { get; set; }
}
