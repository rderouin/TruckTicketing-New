using System;
using System.Linq;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Constants;
using SE.Enterprise.Contracts.Models;
using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.PricingRules;
using SE.Shared.Domain.Product;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Contracts;
using Trident.Data.Contracts;

namespace SE.Integrations.Domain.Processors.EntityProcessors;

[EntityProcessorFor(ServiceBusConstants.EntityMessageTypes.TradeAgreement)]
public class PricingRuleMessageProcessor : BaseEntityProcessor<TradeAgreementModel>
{
    private readonly IProvider<Guid, AccountEntity> _accountProvider;

    private readonly IProvider<Guid, FacilityEntity> _facilityProvider;

    private readonly IManager<Guid, PricingRuleEntity> _pricingRuleManager;

    private readonly IProvider<Guid, ProductEntity> _productProvider;

    public PricingRuleMessageProcessor(IManager<Guid, PricingRuleEntity> pricingRuleManager,
                                       IProvider<Guid, FacilityEntity> facilityProvider,
                                       IProvider<Guid, ProductEntity> productProvider,
                                       IProvider<Guid, AccountEntity> accountProvider)
    {
        _pricingRuleManager = pricingRuleManager;
        _facilityProvider = facilityProvider;
        _productProvider = productProvider;
        _accountProvider = accountProvider;
    }

    public override async Task Process(EntityEnvelopeModel<TradeAgreementModel> message)
    {
        var existingRule = await _pricingRuleManager.GetById(message.EnterpriseId);
        var pricingRule = MapPricingRuleEntity(message.Payload, existingRule ?? new());
        await EnrichFacilityProductInfo(message.Payload, pricingRule);
        pricingRule.Id = message.EnterpriseId;

        await _pricingRuleManager.Save(pricingRule);
    }

    private async Task EnrichFacilityProductInfo(TradeAgreementModel source, PricingRuleEntity pricingRuleEntity)
    {
        var facilityEntity = new FacilityEntity();
        var productEntity = new ProductEntity();
        var accountEntity = new AccountEntity();
        if (source.SiteId.HasText())
        {
            facilityEntity = (await _facilityProvider.Get(p => p.SiteId.ToLower() == source.SiteId.ToLower()))?.FirstOrDefault();
        }

        pricingRuleEntity.FacilityId = facilityEntity?.Id ?? Guid.Empty;

        if (source.ProductNumber.HasText())
        {
            productEntity = (await _productProvider.Get(p => p.Number.ToLower() == source.ProductNumber.ToLower()))?.FirstOrDefault();
        }

        pricingRuleEntity.ProductId = productEntity?.Id ?? Guid.Empty;

        if (source.CustomerNumber.HasText())
        {
            accountEntity = (await _accountProvider.Get(p => p.CustomerNumber.ToLower() == source.CustomerNumber.ToLower()))?.FirstOrDefault();
        }

        pricingRuleEntity.AccountId = accountEntity?.Id ?? Guid.Empty;
    }

    private PricingRuleEntity MapPricingRuleEntity(TradeAgreementModel model, PricingRuleEntity entity)
    {
        entity.CustomerNumber = model.CustomerNumber;
        entity.SalesQuoteType = model.SalesQuoteType;
        entity.ProductNumber = model.ProductNumber;
        entity.PriceGroup = model.PriceGroup;
        entity.SiteId = model.SiteId;
        entity.ActiveFrom = model.ActiveFrom;
        entity.ActiveTo = model.ActiveTo;
        entity.Price = model.Price;
        entity.SourceLocation = model.SourceLocation;
        return entity;
    }
}

public class TradeAgreementModel
{
    public string CustomerNumber { get; set; }

    public SalesQuoteType SalesQuoteType { get; set; }

    public string ProductNumber { get; set; }

    public string PriceGroup { get; set; }

    public string SiteId { get; set; }

    public DateTimeOffset ActiveFrom { get; set; }

    public DateTimeOffset? ActiveTo { get; set; }

    public double Price { get; set; }

    public string SourceLocation { get; set; }
}
