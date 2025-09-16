using System.ComponentModel;

namespace SE.TruckTicketing.Contracts.Lookups;

public enum LoadConfirmationStatus
{
    [Description("Unknown")]
    Unknown = default,

    [Description("Open")]
    Open,

    [Description("Pending Signature")]
    PendingSignature,

    [Description("Submitted to Gateway")]
    SubmittedToGateway,

    [Description("Waiting Signature Validation")]
    WaitingSignatureValidation,

    [Description("Signature Verified")]
    SignatureVerified,

    [Description("Rejected")]
    Rejected,

    [Description("Waiting for Invoice")]
    WaitingForInvoice,

    [Description("Posted")]
    Posted,

    [Description("Void")]
    Void,
}
