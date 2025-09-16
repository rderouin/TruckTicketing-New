using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.ServiceType;
using SE.TruckTicketing.Contracts.Lookups;

namespace SE.TruckTicketing.Domain.Entities.SalesLine.Utils;

public abstract class CutTypePricingRulesFactory
{
    public static ISalesLinePricingRuleStrategy GetCutTypeRulesStrategy(ServiceTypeEntity serviceType, SalesLineEntity salesLine)
    {
        if (serviceType.IncludesOil && salesLine.CutType == SalesLineCutType.Oil)
        {
            return new OilSalesLinePricingRuleStrategy(salesLine, serviceType);
        }

        if (serviceType.IncludesSolids && salesLine.CutType == SalesLineCutType.Solid)
        {
            return new SolidsSalesLinePricingRuleStrategy(salesLine, serviceType);
        }

        if (serviceType.IncludesWater && salesLine.CutType == SalesLineCutType.Water)
        {
            return new WaterSalesLinePricingRuleStrategy(salesLine, serviceType);
        }

        return new DefaultSalesLinePricingRuleStrategy();
    }
}

public interface ISalesLinePricingRuleStrategy
{
    public bool ShouldRefreshPricing();
}

public class DefaultSalesLinePricingRuleStrategy : ISalesLinePricingRuleStrategy
{
    public bool ShouldRefreshPricing()
    {
        return true;
    }
}
