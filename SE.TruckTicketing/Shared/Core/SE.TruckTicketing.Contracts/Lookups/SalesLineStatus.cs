using System.ComponentModel;

namespace SE.TruckTicketing.Contracts.Lookups;

public enum SalesLineStatus
{
    Unspecified = default,

    [Description("Preview")]
    Preview,

    [Description("Approved")]
    Approved,

    [Description("Exception")]
    Exception,

    [Description("SentToFo")]
    SentToFo,

    [Description("Posted")]
    Posted,
    
    [Description("Void")]
    Void,
}
