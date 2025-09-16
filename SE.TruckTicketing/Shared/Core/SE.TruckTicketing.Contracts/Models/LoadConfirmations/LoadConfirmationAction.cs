using System.ComponentModel;

namespace SE.TruckTicketing.Contracts.Models.LoadConfirmations;

public enum LoadConfirmationAction
{
    Unknown,

    [Description("Process Load Confirmation")]
    SendLoadConfirmation,

    [Description("Send Signature Email (Advanced)")]
    ResendAdvancedLoadConfirmationSignatureEmail,

    [Description("Send Signature Email")]
    ResendLoadConfirmationSignatureEmail,

    [Description("Send Field Ticket")]
    ResendFieldTickets,

    [Description("Approve Signature")]
    ApproveSignature,

    [Description("Reject Signature")]
    RejectSignature,

    [Description("Mark as Ready")]
    MarkLoadConfirmationAsReady,

    [Description("Void")]
    VoidLoadConfirmation,
}
