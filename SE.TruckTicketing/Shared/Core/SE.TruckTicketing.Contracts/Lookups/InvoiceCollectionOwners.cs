using System.ComponentModel;

namespace SE.TruckTicketing.Contracts.Lookups;

public enum InvoiceCollectionOwners
{
    [Description("Unknown")]
    Unknown = default,

    [Description("Facility Admin")]
    FacilityAdmin,

    [Description("Advisor")]
    Advisor,

    [Description("AR")]
    AR,

    [Description("Sales - Corporate")]
    SalesCorporate,

    [Description("Sales - Field")]
    SalesField,

    [Description("EDI Team")]
    EDITeam,
}
