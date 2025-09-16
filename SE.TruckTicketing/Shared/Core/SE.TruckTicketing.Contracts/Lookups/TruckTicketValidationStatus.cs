using System.ComponentModel;

namespace SE.TruckTicketing.Contracts.Lookups;

public enum TruckTicketValidationStatus
{
    [Description("Unverified")]
    Unverified = default,

    [Description("Valid")]
    Valid = 1,

    [Description("Error")]
    Error = 2,
}
