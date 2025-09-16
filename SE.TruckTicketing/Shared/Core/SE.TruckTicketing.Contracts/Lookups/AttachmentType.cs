using System.ComponentModel;

namespace SE.TruckTicketing.Contracts.Lookups;

public enum AttachmentType
{
    External = default,

    [Description("Internal")]
    Internal = 1,
}
