using System.Threading.Tasks;

namespace SE.TruckTicketing.Domain.Entities.SalesLine.Utils;

public record DefaultShouldRefreshPricingStrategy : IShouldRefreshPricingStrategy
{
    public Task<bool> ShouldRefreshPricing()
    {
        return Task.FromResult(true);
    }
}
