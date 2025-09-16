using System.ComponentModel;

namespace SE.Shared.Common.Lookups;

public enum LoadSummaryReportFrequency
{
    Undefined = default,

    [Description("Daily")]
    Daily,

    [Description("Weekly")]
    Weekly,

    [Description("Monthly")]
    Monthly,
}
