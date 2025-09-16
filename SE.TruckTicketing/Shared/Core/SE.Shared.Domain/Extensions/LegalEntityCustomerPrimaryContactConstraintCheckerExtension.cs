using SE.Shared.Domain.LegalEntity;

using Trident.Contracts.Configuration;

namespace SE.Shared.Domain.Extensions;

public static class CustomerPrimaryContactConstraintByBusinessStreamExtension
{
    public static LegalEntityConfiguration LegalEntityCustomerPrimaryContactConstraintCheckerExtension(this IAppSettings appSettings)
    {
        return appSettings.GetSection<LegalEntityConfiguration>($"{LegalEntityConfiguration.Section}");
    }
}
