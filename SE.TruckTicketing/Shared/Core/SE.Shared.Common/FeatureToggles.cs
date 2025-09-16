using System;
using System.Diagnostics.CodeAnalysis;

using Trident.Contracts.Configuration;

namespace SE.Shared.Common;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class FeatureToggles
{
    public bool DisablePdfCompression { get; set; }

    public bool DisableChangeTracking { get; set; }

    public static Lazy<FeatureToggles> Init(IAppSettings appSettings)
    {
        return new(() => appSettings.GetSection<FeatureToggles>("FeatureToggles"));
    }
}
