using System.ComponentModel;

namespace SE.Shared.Common.Lookups;

public enum AccountTypes
{
    Undefined = default,

    [Description("Customer")]
    Customer = 1,

    [Description("Generator")]
    Generator = 2,

    [Description("3rd Party Analytical")]
    ThirdPartyAnalytical = 3,

    [Description("Trucking Company")]
    TruckingCompany = 4,
}
