using System.ComponentModel;

namespace SE.TruckTicketing.Contracts.Lookups;

public enum VolumeChangeReason
{
    Undefined = default,

    [Description("Customer Dispute")]
    CustomerDispute = 1,

    [Description("Meter Proving")]
    MeterProving = 2,

    [Description("Meter/Power Outage")]
    MeterPowerOutage = 3,

    [Description("Riser - Wrong/Switched")]
    RiserWrongSwitched = 4,

    [Description("Truck Driver Error")]
    TruckDriverError = 5,

    [Description("Split Load")]
    SplitLoad = 6,

    [Description("Other")]
    Other = 7,
}
