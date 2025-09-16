using System;
using System.ComponentModel;

namespace SE.TruckTicketing.Contracts.Lookups;

public enum EmailTemplateCcType
{
    None = default,

    [Description("Facility Main Email")]
    FacilityMainEmail = 1,

    [Obsolete("Custom option is no longer needed, if the custom email is specified, it will be used.")]
    Custom = 2,
}
