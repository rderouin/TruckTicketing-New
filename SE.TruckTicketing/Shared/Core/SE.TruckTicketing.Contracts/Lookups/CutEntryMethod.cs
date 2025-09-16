using System.ComponentModel;

namespace SE.TruckTicketing.Contracts.Lookups;

public enum CutEntryMethod
{
    [Description("By Fixed Value")]
    FixedValue = default,

    [Description("By Percentage")]
    Percentage = 1,
}
