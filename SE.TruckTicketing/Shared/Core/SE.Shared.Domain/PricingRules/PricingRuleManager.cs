using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Validation;
using Trident.Workflow;

namespace SE.Shared.Domain.PricingRules;

public class PricingRuleManager : ManagerBase<Guid, PricingRuleEntity>, IPricingRuleManager
{
    private readonly IProvider<Guid, PricingRuleEntity> _pricingProvider;

    public PricingRuleManager(ILog logger,
                              IProvider<Guid, PricingRuleEntity> provider,
                              IValidationManager<PricingRuleEntity> validationManager = null,
                              IWorkflowManager<PricingRuleEntity> workflowManager = null)
        : base(logger, provider, validationManager, workflowManager)
    {
        _pricingProvider = provider;
    }

    public async Task<List<ComputePricingResponse>> ComputePrice(ComputePricingRequest request)
    {
        var output = new List<ComputePricingResponse>();

        bool IsValidJobQuoteOrBdAgreement(PricingRuleEntity rule)
        {
            return rule.SourceLocation.HasText() &&
                   rule.SourceLocation.Equals(request.SourceLocation, StringComparison.OrdinalIgnoreCase) &&
                   rule.CustomerNumber.Equals(request.CustomerNumber, StringComparison.OrdinalIgnoreCase) &&
                   rule.SalesQuoteType is SalesQuoteType.JobQuote or SalesQuoteType.BDAgreement;
        }

        bool IsValidCustomerQuote(PricingRuleEntity rule)
        {
            return rule.CustomerNumber.Equals(request.CustomerNumber, StringComparison.OrdinalIgnoreCase) &&
                   rule.SalesQuoteType is SalesQuoteType.CustomerQuote;
        }

        bool IsValidCommercialTerms(PricingRuleEntity rule)
        {
            return rule.CustomerNumber.Equals(request.CustomerNumber, StringComparison.OrdinalIgnoreCase) &&
                   rule.SalesQuoteType is SalesQuoteType.CommercialTerms;
        }

        bool IsValidTieredPricing(PricingRuleEntity rule)
        {
            return rule.PriceGroup.Equals(request.TieredPriceGroup, StringComparison.OrdinalIgnoreCase) &&
                   rule.SalesQuoteType is SalesQuoteType.TieredPricing;
        }

        bool IsValidFacilityPricing(PricingRuleEntity rule)
        {
            return rule.SalesQuoteType is SalesQuoteType.FacilityBaseRate;
        }

        foreach (var product in request.ProductNumber)
        {
            var rules = (await _pricingProvider.Get(rule =>
                                                        rule.SiteId == request.SiteId &&
                                                        rule.ProductNumber == product))
                                                    .Where(x => x.ActiveFrom.Date <= request.Date.Date &&
                                                    (x.ActiveTo == null || x.ActiveTo.Value.Date >= request.Date.Date)).ToList();

            var selectedPricingRule = rules.Where(rule => IsValidJobQuoteOrBdAgreement(rule) ||
                                                          IsValidCustomerQuote(rule) ||
                                                          IsValidCommercialTerms(rule) ||
                                                          IsValidTieredPricing(rule) ||
                                                          IsValidFacilityPricing(rule))
                                           .OrderByDescending(rule => rule.UpdatedAt)
                                           .MinBy(rule => rule.SalesQuoteType);

            if (selectedPricingRule != null)
            {
                output.Add(new()
                {
                    ProductNumber = product,
                    Price = selectedPricingRule.Price,
                    PricingRuleId = selectedPricingRule.Id,
                });
            }
        }

        return output;
    }
}
