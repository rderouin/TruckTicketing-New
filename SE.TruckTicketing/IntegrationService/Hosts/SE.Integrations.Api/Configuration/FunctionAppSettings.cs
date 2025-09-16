using Microsoft.Extensions.Configuration;

using SE.TridentContrib.Extensions;

using Trident.Azure.Functions;
using Trident.Data;

namespace SE.Integrations.Api.Configuration;

public class FunctionAppSettings : FunctionAppSettingsBase
{
    public FunctionAppSettings()
        : base((appSettings, builder) =>
               {
                   SyncfusionLicense.Register(appSettings);

                   RepositoryBase.Options[RepositoryBase.DisablePartitionKeyInferenceOption] = bool.Parse(appSettings[RepositoryBase.DisablePartitionKeyInferenceSetting] ?? "false");

                   builder.AddJsonFile("entity-updates-publisher.settings.json");
                   builder.AddJsonFile("change.settings.json");
                   builder.AddJsonFile("sequences.settings.json");
                   builder.AddJsonFile("entities.settings.json");
               })
    {
    }
}
