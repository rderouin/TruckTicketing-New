using System.ComponentModel;

namespace SE.TruckTicketing.Contracts.Lookups;

public enum Class
{
    Undefined = default,

    [Description("Class1")]
    Class1 = 1,

    [Description("Class2")]
    Class2 = 2,
}
