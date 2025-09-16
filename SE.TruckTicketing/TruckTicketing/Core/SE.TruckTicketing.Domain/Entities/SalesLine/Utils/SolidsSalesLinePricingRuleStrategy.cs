using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.ServiceType;

namespace SE.TruckTicketing.Domain.Entities.SalesLine.Utils;

public class SolidsSalesLinePricingRuleStrategy : ISalesLinePricingRuleStrategy
{
    private readonly SalesLineEntity _salesLine;

    private readonly ServiceTypeEntity _serviceType;

    public SolidsSalesLinePricingRuleStrategy(SalesLineEntity salesLine, ServiceTypeEntity serviceType)
    {
        _salesLine = salesLine;
        _serviceType = serviceType;
    }

    public bool ShouldRefreshPricing()
    {
        if (_salesLine.QuantityPercent < _serviceType.SolidMinPricingPercentage)
        {
            return false;
        }

        return true;
    }
}
