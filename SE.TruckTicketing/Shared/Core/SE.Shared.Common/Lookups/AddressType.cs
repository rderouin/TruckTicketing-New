using System.ComponentModel;

namespace SE.Shared.Common.Lookups;

public enum AddressType
{
    Undefined = default,

    [Description("Mail")]
    Mail = 1,
}
