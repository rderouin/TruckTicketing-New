using System.ComponentModel;

namespace SE.TruckTicketing.Contracts.Lookups;

public enum SalesLinePriceChangeReason
{
    Undefined = default,

    [Description("Requested by Ops/Sales")]
    RequestedByOpsSales = 1,

    [Description("Ticketing Error")]
    TicketingError = 2,

    [Description("System Rate Correction")]
    SystemRateCorrection = 3,

    [Description("Project/Job Specific Override")]
    ProjectJobSpecificOverride = 4,

    [Description("Other (additional comments must be provided)")]
    Other = 5,
}
