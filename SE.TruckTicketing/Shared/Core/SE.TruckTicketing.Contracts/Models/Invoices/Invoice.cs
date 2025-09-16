using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

using SE.Shared.Common.Extensions;
using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Contracts.Api;
using Trident.Data;

namespace SE.TruckTicketing.Contracts.Models.Invoices;

public class Invoice : GuidApiModelBase, IFacilityRelatedModel
{
    public string ProformaInvoiceNumber { get; set; }

    public string GlInvoiceNumber { get; set; }

    public Guid CustomerId { get; set; }

    public string CustomerName { get; set; }

    public string FacilityName { get; set; }

    public string SiteId { get; set; }

    public InvoiceStatus Status { get; set; }

    public bool IsReversed { get; set; }

    public bool IsReversal { get; set; }

    public Guid? ReversedInvoiceId { get; set; }

    public string ReversedProformaInvoiceNumber { get; set; }

    public Guid? ReversalInvoiceId { get; set; }

    public string ReversalProformaInvoiceNumber { get; set; }

    public DateTimeOffset TicketDateRangeStart { get; set; }

    public DateTimeOffset TicketDateRangeEnd { get; set; }

    public DateTimeOffset InvoiceStartDate { get; set; }

    public DateTimeOffset? InvoiceEndDate { get; set; }

    public DateTimeOffset? LastDistributionDate { get; set; }

    public Guid LastSentById { get; set; }

    public string LastSentByName { get; set; }

    public bool Paid { get; set; }

    public int SalesLineCount { get; set; }

    public double InvoiceAmount { get; set; }

    [OwnedHierarchy]
    public List<InvoiceAttachment> Attachments { get; set; } = new();

    public string Currency { get; set; }

    [OwnedHierarchy]
    public List<string> Generators { get; set; } = new();

    public string GeneratorNames { get; set; }

    public bool HasAllLoadConfirmationApprovals { get; set; }

    public string BillingConfigurationNames { get; set; }

    public string CustomerNumber { get; set; }

    public string ManifestNumber { get; set; }

    public string BillofLading { get; set; }

    public List<EDIFieldValue> EDIFieldValues { get; set; } = new();

    public string EDIFieldsValueString { get; set; }

    public Guid SourceLocationTypeId { get; set; }

    public string SourceLocationTypeName { get; set; }

    public Guid SourceLocationId { get; set; }

    public string SourceLocationIdentifier { get; set; }

    public string SourceLocationFormattedIdentifier { get; set; }

    public DowNonDow DowNonDow { get; set; }

    public AttachmentIndicatorType AttachmentIndicatorType { get; set; }

    public HasAttachments HasAttachments { get; set; }

    [NotMapped]
    public string TicketDateRange => TicketDateRangeStart.Date.ToShortDateString() + "-" + TicketDateRangeEnd.Date.ToShortDateString();

    public List<InvoiceBillingConfiguration> BillingConfigurations { get; set; } = new();

    public InvoiceDistributionMethod DistributionMethod { get; set; }

    public InvoiceReversalReason InvoiceReversalReason { get; set; }

    public string InvoiceReversalDescription { get; set; }

    public InvoiceCollectionOwners CollectionOwner { get; set; }

    public InvoiceCollectionReason CollectionReason { get; set; }

    public string CollectionNotes { get; set; }

    public bool TransactionComplete { get; set; }

    public Guid? BillingConfigurationId { get; set; }

    public CreditStatus? CustomerCreditStatus { get; set; }

    public WatchListStatus? CustomerWatchListStatus { get; set; }

    public int TruckTicketCount { get; set; }

    public double? MaxInvoiceAmountThreshold { get; set; }

    public double? MaxTruckTicketCountThreshold { get; set; }

    public string BusinessUnit { get; set; }

    public string Division { get; set; }

    public string AccountNumber { get; set; }

    public string OriginalGlInvoiceNumber { get; set; }

    public string OriginalProformaInvoiceNumber { get; set; }

    public string CreatedBy { get; set; }

    public string CreatedById { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string UpdatedBy { get; set; }

    public string UpdatedById { get; set; }

    public bool? IsDeliveredToErp { get; set; }

    public bool RequiresPdfRegeneration { get; set; } = true;

    public Guid? BillingContactId { get; set; }

    public Guid FacilityId { get; set; }
}

public class InvoiceAttachment : GuidApiModelBase
{
    public string ReferenceId => Id.ToReferenceId();

    public string BlobPath { get; set; }

    public string FileName { get; set; }

    public string ContentType { get; set; }

    public DateTimeOffset AttachedOn { get; set; }

    public bool IsUploaded { get; set; }
}

public class InvoiceAttachmentUpload
{
    public InvoiceAttachment Attachment { get; set; }

    public string Uri { get; set; }
}

public class PostInvoiceActionRequest
{
    public CompositeKey<Guid> InvoiceKey { get; set; }

    public InvoiceStatus InvoiceStatus { get; set; }

    public InvoiceAction InvoiceAction { get; set; }
}

public class InvoiceAdvancedEmailRequest
{
    public CompositeKey<Guid> InvoiceKey { get; set; }

    public string To { get; set; }

    public string Cc { get; set; }

    public string Bcc { get; set; }

    public InvoiceStatus InvoiceStatus { get; set; }

    public string AdHocNote { get; set; }

    public bool IsCustomeEmail { get; set; }
}

public class ReverseInvoiceRequest
{
    public CompositeKey<Guid> InvoiceKey { get; set; }

    public InvoiceReversalReason InvoiceReversalReason { get; set; }

    public string InvoiceReversalDescription { get; set; }

    public bool IncludeOriginalDocuments { get; set; }

    public bool CreateProForma { get; set; }
}

public class ReverseInvoiceResponse
{
    public Guid? OriginalInvoiceId { get; set; }

    public Guid? ReversalInvoiceId { get; set; }

    public Guid? ProformaInvoiceId { get; set; }

    public string ErrorMessage { get; set; }
}

public class InvoiceBillingConfiguration : GuidApiModelBase
{
    public int AssociatedSalesLinesCount { get; set; }

    public Guid? BillingConfigurationId { get; set; }

    public string BillingConfigurationName { get; set; }
}
