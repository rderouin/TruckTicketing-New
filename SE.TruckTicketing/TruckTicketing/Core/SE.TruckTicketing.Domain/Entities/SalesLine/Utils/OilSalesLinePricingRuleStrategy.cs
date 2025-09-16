using System;

using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.ServiceType;

namespace SE.TruckTicketing.Domain.Entities.SalesLine.Utils;

public class OilSalesLinePricingRuleStrategy : ISalesLinePricingRuleStrategy
{
    private readonly SalesLineEntity _salesLine;
    private readonly ServiceTypeEntity _serviceType;

    public OilSalesLinePricingRuleStrategy(SalesLineEntity salesLine, ServiceTypeEntity serviceType)
    {
        _salesLine = salesLine;
        _serviceType = serviceType;
    }

    public bool ShouldRefreshPricing()
    {
        
        if (Math.Abs(_salesLine.Quantity) < _serviceType.OilCreditMinVolume)
        {
            return false;
        }

        return true;
    }
}
