using SE.TruckTicketing.Contracts.Lookups;

namespace SE.TruckTicketing.Domain.Entities.SalesLine.Utils;

public abstract record ShouldRefreshPricingFactory
{
    public static IShouldRefreshPricingStrategy GetStrategy(PriceRefreshContext priceRefreshContext)
    {
        if (priceRefreshContext.CurrentSalesLine == null)
        {
            return new DefaultShouldRefreshPricingStrategy();
        }

        if (priceRefreshContext.CurrentSalesLine.Status == SalesLineStatus.Exception)
        {
            return new ExceptionStatusRefreshPricingStrategy();
        }

        if (priceRefreshContext.CurrentSalesLine.Status is SalesLineStatus.Void or SalesLineStatus.Posted)
        {
            return new VoidOrPostedStatusRefreshPricingStrategy();
        }

        if (priceRefreshContext.CurrentSalesLine.Status is SalesLineStatus.Preview or SalesLineStatus.Approved or SalesLineStatus.SentToFo)
        {
            return new PreviewApprovedSentToFoRefreshPricingStrategy(priceRefreshContext);
        }

        return new DefaultShouldRefreshPricingStrategy();
    }
}
