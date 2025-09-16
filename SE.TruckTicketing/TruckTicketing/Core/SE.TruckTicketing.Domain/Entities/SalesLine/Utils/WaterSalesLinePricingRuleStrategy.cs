using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.ServiceType;

namespace SE.TruckTicketing.Domain.Entities.SalesLine.Utils;

public class WaterSalesLinePricingRuleStrategy : ISalesLinePricingRuleStrategy
{
    private readonly SalesLineEntity _salesLine;

    private readonly ServiceTypeEntity _serviceType;

    public WaterSalesLinePricingRuleStrategy(SalesLineEntity salesLine, ServiceTypeEntity serviceType)
    {
        _salesLine = salesLine;
        _serviceType = serviceType;
    }

    public bool ShouldRefreshPricing()
    {
        if (_salesLine.QuantityPercent < _serviceType.WaterMinPricingPercentage)
        {
            return false;
        }

        return true;
    }
}
