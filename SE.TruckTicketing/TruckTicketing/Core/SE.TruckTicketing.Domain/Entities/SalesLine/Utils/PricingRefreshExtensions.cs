using SE.Shared.Domain.Entities.SalesLine;

namespace SE.TruckTicketing.Domain.Entities.SalesLine.Utils;
public static class PricingRefreshExtensions
{
    //everything But Oil Credits
    public static bool IsProductAnythingButOilCredit(this SalesLineEntity salesLine)
    {
        return salesLine.ProductNumber.StartsWith("7");
    }
}
