using System.ComponentModel;

namespace SE.Shared.Common.Lookups;

public enum AccountFieldSignatoryContactType
{
    [Description("")]
    None = default,

    Drilling,

    Completions,

    Production,

    Remediation,

    Industrial,
}
