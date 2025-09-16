using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SE.Shared.Common.Extensions;
using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Api.Search;
using Trident.Contracts.Api;

namespace SE.TruckTicketing.Contracts.Models.LoadConfirmations;

public class LoadConfirmation : GuidApiModelBase, IFacilityRelatedModel
{
    public string Number { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public LoadConfirmationStatus Status { get; set; }

    public string FacilityName { get; set; }

    public string GlInvoiceNumber { get; set; }

    public Guid InvoiceId { get; set; }

    public string InvoiceNumber { get; set; }

    public bool IsReversed { get; set; }

    public bool IsReversal { get; set; }

    public Guid? ReversedLoadConfirmationId { get; set; }

    public Guid? ReversalLoadConfirmationId { get; set; }

    public Guid BillingCustomerId { get; set; }

    public string BillingCustomerName { get; set; }

    public string BillingCustomerNumber { get; set; }

    public string BillingCustomerDunsNumber { get; set; }

    public bool FieldTicketsUploadEnabled { get; set; }

    public string Frequency { get; set; }

    //This is the load confirmation creation date
    public DateTimeOffset StartDate { get; set; }

    //This is the load confirmation closure date
    public DateTimeOffset? EndDate { get; set; }

    public DateTimeOffset TicketStartDate { get; set; }

    public DateTimeOffset TicketEndDate { get; set; }

    public List<LoadConfirmationGenerator> Generators { get; set; } = new();

    public string GeneratorNames { get; set; }

    public List<SignatoryContact> Signatories { get; set; } = new();

    public bool SignatoriesAreUpdated { get; set; }

    public string SignatoryNames { get; set; }

    [NotMapped]
    public string SignatoryEmails { get; set; }

    public int SalesLineCount { get; set; }

    public double TotalCost { get; set; }

    public Guid BillingConfigurationId { get; set; }

    public string BillingConfigurationName { get; set; }

    [NotMapped]
    public SearchResultsModel<SalesLine, SearchCriteriaModel> SalesLineItemResults { get; set; } = new();

    public List<LoadConfirmationAttachment> Attachments { get; set; } = new();

    [NotMapped]
    public string TicketDateRange => TicketStartDate.Date.ToShortDateString() + "-" + TicketEndDate.Date.ToShortDateString();

    public CreditStatus? CustomerCreditStatus { get; set; }

    public WatchListStatus? CustomerWatchListStatus { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public InvoiceStatus InvoiceStatus { get; set; }

    public bool RequiresInvoiceDocumentRegeneration { get; set; }

    public string SiteId { get; set; }

    public string Currency { get; set; }

    public string LegalEntity { get; set; }

    public string InvoicePermutationId { get; set; }

    public bool HasFailedDueToGatewayError { get; set; }

    public bool RequiresDocumentRegeneration { get; set; }

    public DateTimeOffset LastApprovalEmailSentOn { get; set; }

    public int SentCount { get; set; }

    public Guid FacilityId { get; set; }
}

public class LoadConfirmationGenerator : GuidApiModelBase
{
    public Guid AccountId { get; set; }

    public string Name { get; set; }
}

public class LoadConfirmationAttachment : ApiModelBase<Guid>
{
    public string ReferenceId => Id.ToReferenceId();

    public string Uri { get; set; }

    public string BlobContainer { get; set; }

    public string BlobPath { get; set; }

    public string FileName { get; set; }

    public string ContentType { get; set; }

    public DateTimeOffset AttachedOn { get; set; }

    public bool IsIncludedInInvoice { get; set; }

    public LoadConfirmationAttachmentOrigin AttachmentOrigin { get; set; }
}
