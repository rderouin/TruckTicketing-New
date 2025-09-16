using System;
using System.Collections.Generic;
using System.Linq;

using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Entities.AdditionalServicesConfiguration;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.ServiceType;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.PricingRules;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.SalesLine.Utils;

namespace SE.TruckTicketing.Domain.Entities.SalesLine;

public class SalesLineManagerHelper
{
    public static ComputePricingResponse GetPricingResponseFromPricingProductMap(SalesLinePreviewRequestContext context, string number)
    {
        return context.PricingByProductNumberMap.TryGetValue(number, out var pricingResponse) ? pricingResponse : null;
    }

    public static void VoidTheSalesLine(SalesLineEntity salesLine)
    {
        salesLine.InvoiceId = null;
        salesLine.ProformaInvoiceNumber = null;
        salesLine.LoadConfirmationId = null;
        salesLine.LoadConfirmationNumber = null;
        salesLine.Status = SalesLineStatus.Void;
        salesLine.AwaitingRemovalAcknowledgment = true;
    }

    public static double GetPriceOrZero(ComputePricingResponse price)
    {
        if (price == null)
        {
            return 0;
        }
        
        return price.Price;
    }

    public static void SetSalesLineRateAndValues(SalesLineEntity salesLine, ComputePricingResponse pricing)
    {
        salesLine.Rate = pricing?.Price ?? 0;
        salesLine.PricingRuleId = pricing?.PricingRuleId;
        // salesLine.TotalValue = salesLine.Quantity * salesLine.Rate; COMMENTING OUT BECAUSE ITS WRONG BUT WAS IN PRODUCTION CODE. It gets set in ApplyFoRounding()
        salesLine.IsRateOverridden = false;
        salesLine.ApplyFoRounding();
    }

    public static bool GetSavedEntityValue(SalesLineEntity salesLine)
    {
        if (!salesLine.CanPriceBeRefreshed.HasValue)
        {
            return false;
        }

        return salesLine.CanPriceBeRefreshed.Value;
    }

    public static void ErrorOutSalesLineAndSetStatusToException(SalesLineEntity salesLine)
    {
        salesLine.Rate = 0;
        salesLine.TotalValue = 0;
        SetStatusToException(salesLine);
    }

    public static void SetStatusToException(SalesLineEntity salesLine)
    {
        salesLine.Status = SalesLineStatus.Exception;
    }

    public static bool DoesNotHaveAnyAdditionalServicesForTruckTicket(TruckTicketEntity truckTicket)
    {
        if (truckTicket.AdditionalServices == null) return true;

        if (truckTicket.AdditionalServices.Any())
        {
            return false;
        }

        return true;
    }

    public static bool GetCutTypeRules(SalesLineEntity salesLine, ServiceTypeEntity serviceType)
    {
        var factory = CutTypePricingRulesFactory.GetCutTypeRulesStrategy(serviceType, salesLine);
        return factory.ShouldRefreshPricing();
    }

    public static bool HasPriceBeenChangedManually(SalesLineEntity salesLine)
    {
        return salesLine.PriceChangeUserName.HasText() && salesLine.PriceChangeDate.HasValue;
    }

    public static bool DoesAdditionalServiceConfigHaveAnyAdditionalServices(AdditionalServicesConfigurationEntity additionalServicesConfig)
    {
        return additionalServicesConfig.AdditionalServices.Any();
    }

    public static bool CanPriceBeRefreshed(bool isAdditionalServiceConfigZeroTotal, SalesLineEntity salesLine)
    {
        if (isAdditionalServiceConfigZeroTotal && IsSalesLineCutTypeTotalAndHasZeroRate(salesLine))
        {
            return false;
        }

        return true;
    }

    public static bool IsSalesLineCutTypeTotalAndHasZeroRate(SalesLineEntity salesLine)
    {
        return salesLine.Rate == 0 && salesLine.CutType == SalesLineCutType.Total;
    }
    
    public static bool DoesNotHaveAnyAdditionalServicesConfigs(IEnumerable<AdditionalServicesConfigurationEntity> additionalServicesConfigurations)
    {
        if (additionalServicesConfigurations == null)
        {
            return true;
        }

        if (!additionalServicesConfigurations.Any())
        {
            return true;
        }

        return false;
    }

    public static bool IsFieldTicketOfTypeTicketByTicketOrLcBatch(BillingConfigurationEntity billingConfig)
    {
        return billingConfig.FieldTicketsUploadEnabled &&
               (billingConfig.FieldTicketDeliveryMethod == FieldTicketDeliveryMethod.TicketByTicket
             || billingConfig.FieldTicketDeliveryMethod == FieldTicketDeliveryMethod.LoadConfirmationBatch);
    }

    public static IEnumerable<AdditionalServicesConfigurationEntity> FindAdditionalServiceOfThisProduct(IEnumerable<AdditionalServicesConfigurationEntity> additionalServicesConfigsWithApplyZeroTotalVolume, Guid salesLineProductId)
    {
        return additionalServicesConfigsWithApplyZeroTotalVolume.Where(x => x.AdditionalServices.Select(y => y.ProductId).Contains(salesLineProductId));
    }
}


