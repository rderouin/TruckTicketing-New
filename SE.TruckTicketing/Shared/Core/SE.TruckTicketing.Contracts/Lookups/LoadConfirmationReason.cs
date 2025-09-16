using System.ComponentModel;

namespace SE.TruckTicketing.Contracts.Lookups;

public enum LoadConfirmationReason
{
    Unknown = default,

    [Description("Missing signature or attachment")]
    MissingSignatureAttachment,

    [Description("Customer rejected")]
    CustomerRejected,

    [Description("Other")]
    Other,
}
