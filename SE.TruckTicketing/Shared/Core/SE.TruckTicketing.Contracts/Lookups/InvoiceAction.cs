namespace SE.TruckTicketing.Contracts.Lookups;

/// <summary>
///     Used to indicate what action to execute on Invoice
/// </summary>
public enum InvoiceAction
{
    Undefined,

    Open,

    Resend,

    Email,

    AdvancedEmail,

    PostAndSend,

    Reverse,

    Send,

    Post,

    MarkAsPaidUnsettled,

    Void,

    Regenerate,
}
