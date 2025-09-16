using System.ComponentModel;

namespace SE.Shared.Common.Lookups;

public enum InvoiceSplittingCategories
{
    Undefined = default,

    [Description("Source Location")]
    SourceLocation = 1,

    [Description("Service Type")]
    ServiceType = 2,

    [Description("Well Classification")]
    WellClassification = 3,

    [Description("Substance")]
    Substance = 4,
    [Description("Facility")]
    Facility = 5,
}
