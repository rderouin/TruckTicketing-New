using System.ComponentModel;

namespace SE.Shared.Common.Lookups;

public enum AccountContactFunctions
{
    Unspecified = default,

    [Category(nameof(AccountTypes.Customer))]
    [Description("Billing Contact")]
    BillingContact,

    [Category(nameof(AccountTypes.Customer))]
    [Description("Field Signatory Contact")]
    FieldSignatoryContact,

    [Category(nameof(AccountTypes.Generator))]
    [Description("Generator Representative")]
    GeneratorRepresentative,

    [Category(nameof(AccountTypes.Generator))]
    [Description("Production Accountant")]
    ProductionAccountant,

    [Category(nameof(AccountTypes.ThirdPartyAnalytical))]
    [Description("3rd Party Contact")]
    ThirdPartyContact,

    [Description("General")]
    General,
}
