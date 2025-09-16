using System.ComponentModel;

namespace SE.TruckTicketing.Contracts.Lookups;

public enum LoadConfirmationAttachmentOrigin
{
    [Description("Unknown")]
    Unknown = default,

    [Description("Integrations")]
    Integrations,

    [Description("Preview")]
    Preview,

    [Description("Manual")]
    Manual,
}
