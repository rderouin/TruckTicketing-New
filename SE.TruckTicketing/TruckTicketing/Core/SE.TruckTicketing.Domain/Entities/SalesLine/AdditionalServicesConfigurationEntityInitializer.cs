using System.Collections.Generic;
using System.Linq;

using SE.Shared.Domain.Entities.AdditionalServicesConfiguration;

namespace SE.TruckTicketing.Domain.Entities.SalesLine;
public class AdditionalServicesConfigurationEntityInitializer
{
    public static AdditionalServicesConfigurationEntityInitializer Instance = new();

    public AdditionalServicesConfig Initialize(List<AdditionalServicesConfigurationEntity> validConfigs)
    {
        var additionalServicesConfig = new AdditionalServicesConfig()
        {
            ZeroTotal = validConfigs.Any(config => config.ApplyZeroTotalVolume),
            ZeroOil = validConfigs.Any(config => config.ApplyZeroOilVolume),
            ZeroWater = validConfigs.Any(config => config.ApplyZeroWaterVolume),
            ZeroSolids = validConfigs.Any(config => config.ApplyZeroSolidsVolume),
            AdditionalServices = validConfigs.SelectMany(config => config.AdditionalServices).ToList(),
        };

        return additionalServicesConfig;
    }


}
