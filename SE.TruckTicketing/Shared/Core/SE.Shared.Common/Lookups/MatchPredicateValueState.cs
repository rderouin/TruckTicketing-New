using System.ComponentModel;

namespace SE.Shared.Common.Lookups;

public enum MatchPredicateValueState
{
    Unspecified = default,

    [Description("Any")]
    Any = 1,

    [Description("Ignore")]
    NotSet = 2,

    [Description("Value: ")]
    Value = 3,
}
