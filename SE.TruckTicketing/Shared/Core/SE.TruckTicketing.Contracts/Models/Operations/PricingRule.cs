using System;

using SE.TruckTicketing.Contracts.Lookups;

namespace SE.TruckTicketing.Contracts.Models.Operations;

public class PricingRule : GuidApiModelBase
{
    public Guid FacilityId { get; set; }

    public string SiteId { get; set; }

    public Guid ProductId { get; set; }

    public string ProductNumber { get; set; }

    public string CustomerNumber { get; set; }

    public Guid AccountId { get; set; }

    public string PriceGroup { get; set; }

    public SalesQuoteType SalesQuoteType { get; set; }

    public DateTimeOffset ActiveFrom { get; set; }

    public DateTimeOffset? ActiveTo { get; set; }

    public double Price { get; set; }
}
