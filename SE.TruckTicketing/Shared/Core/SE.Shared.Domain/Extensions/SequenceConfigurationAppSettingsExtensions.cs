using SE.Shared.Domain.Entities.Sequences;

using Trident.Contracts.Configuration;

namespace SE.Shared.Domain.Extensions;

public static class SequenceConfigurationAppSettingsExtensions
{
    public static SequenceConfiguration GetSequenceConfiguration(this IAppSettings appSettings, string sequenceType)
    {
        return appSettings.GetSection<SequenceConfiguration>($"{SequenceConfiguration.Section}:{sequenceType}");
    }
}
