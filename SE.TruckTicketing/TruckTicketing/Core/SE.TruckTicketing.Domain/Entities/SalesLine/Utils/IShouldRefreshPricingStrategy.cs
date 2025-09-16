using System.Threading.Tasks;

namespace SE.TruckTicketing.Domain.Entities.SalesLine.Utils;

public interface IShouldRefreshPricingStrategy
{
    Task<bool> ShouldRefreshPricing();
}
