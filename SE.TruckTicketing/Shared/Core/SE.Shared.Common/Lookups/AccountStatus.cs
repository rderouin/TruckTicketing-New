using System.ComponentModel;

namespace SE.Shared.Common.Lookups;

public enum AccountStatus
{
    Undefined = default,

    [Description("Open")]
    Open = 1,

    [Description("Close")]
    Close = 2,
}
