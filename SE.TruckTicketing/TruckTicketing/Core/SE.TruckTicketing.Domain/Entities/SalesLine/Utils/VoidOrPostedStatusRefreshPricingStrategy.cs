using System.Threading.Tasks;

namespace SE.TruckTicketing.Domain.Entities.SalesLine.Utils;

public record VoidOrPostedStatusRefreshPricingStrategy : IShouldRefreshPricingStrategy
{
    public Task<bool> ShouldRefreshPricing()
    {
        return Task.FromResult(false);
    }
}
