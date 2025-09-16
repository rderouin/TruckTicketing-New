using SE.Shared.Domain.Entities.SalesLine;

namespace SE.TruckTicketing.Domain.Entities.SalesLine.Utils;

public interface IOilPricingRuleStrategy
{
    public bool ShouldRefreshPricing(SalesLineEntity salesLine);
}
