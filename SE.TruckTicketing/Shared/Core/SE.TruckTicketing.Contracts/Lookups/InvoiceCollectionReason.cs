using System.ComponentModel;

namespace SE.TruckTicketing.Contracts.Lookups;

public enum InvoiceCollectionReason
{
    None = default,

    [Description("Sent to Advisor for review")]
    SentToAdvisorForReview,

    [Description("Sent to Sales – Field for review")]
    SentToSalesFieldForReview,

    [Description("Sent to Sales – Corporate for review")]
    SentToSalesCorporateForReview,

    [Description("Sent to AR for review")]
    SentToARForReview,

    [Description("Sent to Facility for review")]
    SentToFacilityForReview,

    [Description("Sent to EDI for entry")]
    SentToEDIForEntry,

    [Description("Sent for Payment")]
    SentForPayment,

    [Description("Entered in EDI platform")]
    EnteredInEDIPlatform,

    [Description("Submitted status in EDI platform")]
    SubmittedStatusInEDIPlatform,

    [Description("Approved status in EDI platform")]
    ApprovedStatusInEDIPlatform,

    [Description("Resubmitted status in EDI platform")]
    ResubmittedStatusInEDIPlatform,

    [Description("Monthly AR statement sent")]
    MonthlyARStatementSent,

    [Description("Paid")]
    Paid,

    [Description("Other")]
    Other,
}
