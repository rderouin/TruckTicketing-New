using System.ComponentModel;

namespace SE.Shared.Common.Lookups;

public enum WatchListStatus
{
    Undefined = default,

    [Description("Red")]
    Red = 1,

    [Description("Yellow")]
    Yellow = 2,
}
