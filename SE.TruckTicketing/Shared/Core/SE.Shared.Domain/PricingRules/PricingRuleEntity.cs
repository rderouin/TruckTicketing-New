using System;

using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Data;
using Trident.SourceGeneration.Attributes;

namespace SE.Shared.Domain.PricingRules;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Pricing, nameof(DocumentType), Databases.DocumentTypes.PricingRule, PartitionKeyType.WellKnown)]
[Discriminator(nameof(EntityType), Containers.Discriminators.PricingRule)]
[GenerateProvider]
[GenerateRepository]
public class PricingRuleEntity : TTAuditableEntityBase
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

    public string SourceLocation { get; set; }
}
