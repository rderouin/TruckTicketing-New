using System.Threading.Tasks;

namespace SE.TruckTicketing.Domain.Entities.SalesLine.Utils;

public record ExceptionStatusRefreshPricingStrategy : IShouldRefreshPricingStrategy
{
    public Task<bool> ShouldRefreshPricing()
    {
        return Task.FromResult(true);
    }
}
