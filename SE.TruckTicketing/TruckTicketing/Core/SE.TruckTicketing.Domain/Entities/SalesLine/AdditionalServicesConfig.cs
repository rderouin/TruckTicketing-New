using System.Collections.Generic;

using SE.Shared.Domain.Entities.AdditionalServicesConfiguration;

namespace SE.TruckTicketing.Domain.Entities.SalesLine;

public class AdditionalServicesConfig
{
    public bool ZeroTotal { get; set; }

    public bool ZeroOil { get; set; }

    public bool ZeroWater { get; set; }

    public bool ZeroSolids { get; set; }

    public List<AdditionalServicesConfigurationAdditionalServiceEntity> AdditionalServices { get; set; } = new();
}
