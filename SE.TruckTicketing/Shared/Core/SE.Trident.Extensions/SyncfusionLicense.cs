using SE.Shared.Common.Extensions;

using Syncfusion.Licensing;

using Trident.Contracts.Configuration;

namespace SE.TridentContrib.Extensions;

public static class SyncfusionLicense
{
    private const string SyncfusionLicenseKey = "AppSettings:SyncfusionLicense";

    public static void Register(IAppSettings appSettings)
    {
        var syncfusionLicense = appSettings[SyncfusionLicenseKey];
        if (syncfusionLicense.HasText())
        {
            SyncfusionLicenseProvider.RegisterLicense(syncfusionLicense);
        }
    }
}
