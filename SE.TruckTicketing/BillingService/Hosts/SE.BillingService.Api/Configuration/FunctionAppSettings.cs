using Azure.Identity;

using Microsoft.Extensions.Configuration;

using SE.TridentContrib.Extensions;

using Trident.Azure.Functions;
using Trident.Data;

namespace SE.BillingService.Api.Configuration;

public class FunctionAppSettings : FunctionAppSettingsBase
{
    private const string AppConfigurationConnectionStringKey = "ConnectionStrings:AzureAppConfiguration";

    private const string AzureTenantIdKey = "AppSettings:AZURE_TENANT_ID";

    public FunctionAppSettings()
        : base((appSettings, builder) =>
               {
                   var connectionString = appSettings[AppConfigurationConnectionStringKey];
                   var azureTenantId = appSettings[AzureTenantIdKey];

                   SyncfusionLicense.Register(appSettings);

                   RepositoryBase.Options[RepositoryBase.DisablePartitionKeyInferenceOption] = bool.Parse(appSettings[RepositoryBase.DisablePartitionKeyInferenceSetting] ?? "false");

                   if (!string.IsNullOrEmpty(connectionString))
                   {
                       builder.AddAzureAppConfiguration(options =>
                                                        {
                                                            options.Connect(connectionString)
                                                                    //.Select("*")
                                                                   .ConfigureKeyVault(kv => kv.SetCredential(new DefaultAzureCredential(new DefaultAzureCredentialOptions
                                                                    {
                                                                        InteractiveBrowserTenantId = azureTenantId,
                                                                        SharedTokenCacheTenantId = azureTenantId,
                                                                        VisualStudioTenantId = azureTenantId,
                                                                        VisualStudioCodeTenantId = azureTenantId,
                                                                    })));
                                                        });
                   }

                   builder.AddJsonFile("entity-updates-publisher.settings.json");
                   builder.AddJsonFile("change.settings.json");
                   builder.AddJsonFile("sequences.settings.json");
               })
    {
    }
}
