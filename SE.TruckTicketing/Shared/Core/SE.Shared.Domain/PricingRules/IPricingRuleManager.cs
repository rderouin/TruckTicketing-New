using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Trident.Contracts;

namespace SE.Shared.Domain.PricingRules;

public interface IPricingRuleManager : IManager<Guid, PricingRuleEntity>
{
    Task<List<ComputePricingResponse>> ComputePrice(ComputePricingRequest request);
}

public class ComputePricingRequest
{
    public string SiteId { get; set; }

    public List<string> ProductNumber { get; set; }

    public DateTimeOffset Date { get; set; }

    public string CustomerNumber { get; set; }

    public string CustomerGroup { get; set; }

    public string SourceLocation { get; set; }

    public string TieredPriceGroup { get; set; }

    public string ParentSubsidiaryCustomerNumber { get; set; }
}

public class ComputePricingResponse
{
    public string ProductNumber { get; set; }

    public double Price { get; set; }

    public Guid? PricingRuleId { get; set; }
}
