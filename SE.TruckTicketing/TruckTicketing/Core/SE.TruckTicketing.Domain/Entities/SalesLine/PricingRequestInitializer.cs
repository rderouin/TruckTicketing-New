using System;

using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.PricingRules;

namespace SE.TruckTicketing.Domain.Entities.SalesLine;

public class PricingRequestInitializer
{
    public static PricingRequestInitializer Instance = new();

    public ComputePricingRequest Initialize(AccountEntity account, DateTimeOffset? loadDate, string productNumber, string siteId, string sourceLocation)
    {
        if (loadDate == null || loadDate == DateTimeOffset.MinValue)
        {
            loadDate = DateTimeOffset.UtcNow;
        }

        return new()
        {
            CustomerGroup = account.CustomerNumber,
            CustomerNumber = account.PriceGroup.HasText() ? account.PriceGroup : account.CustomerNumber,
            Date = (DateTimeOffset)loadDate,
            ProductNumber = new() { productNumber },
            SiteId = siteId,
            SourceLocation = sourceLocation,
            TieredPriceGroup = account.TmaGroup,
        };
    }
}
