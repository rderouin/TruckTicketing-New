using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SE.Shared.Domain.Entities.AdditionalServicesConfiguration;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.ServiceType;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.PricingRules;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.SalesLine;
using SE.TruckTicketing.Domain.Entities.SalesLine.Utils;

namespace SE.TruckTicketing.Domain.Tests.SalesLine;

[TestClass]
public class SalesLineManagerHelperTests
{
    [TestMethod]
    public void VoidOrPostedStatusRefreshPricingStrategy_AlwaysReturnsFalse()
    {
        //setup

        //execute
        var result = new VoidOrPostedStatusRefreshPricingStrategy().ShouldRefreshPricing().Result;

        //assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ExceptionStatusRefreshPricingStrategy_AlwaysReturnsTrue()
    {
        //setup

        //execute
        var result = new ExceptionStatusRefreshPricingStrategy().ShouldRefreshPricing().Result;

        //assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void DefaultShouldRefreshPricingStrategy_AlwaysReturnsTrue()
    {
        //setup

        //execute
        var result = new DefaultShouldRefreshPricingStrategy().ShouldRefreshPricing().Result;

        //assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ShouldRefreshPricingFactoryGetStrategy_StatusSentToFo_ReturnsVoidOrPostedStatusRefreshPricingStrategy()
    {
        //setup
        SalesLineEntity salesLine = new SalesLineEntity() { Status = SalesLineStatus.SentToFo };
        PriceRefreshContext priceRefreshContext = GetDefaultPriceRefreshContext(salesLine);

        //execute
        var strategy = ShouldRefreshPricingFactory.GetStrategy(priceRefreshContext).GetType().Name;
        
        //assert
        Assert.AreEqual(nameof(PreviewApprovedSentToFoRefreshPricingStrategy), strategy);
    }

    [TestMethod]
    public void ShouldRefreshPricingFactoryGetStrategy_StatusApproved_ReturnsVoidOrPostedStatusRefreshPricingStrategy()
    {
        //setup
        SalesLineEntity salesLine = new SalesLineEntity() { Status = SalesLineStatus.Approved };
        PriceRefreshContext priceRefreshContext = GetDefaultPriceRefreshContext(salesLine);

        //execute
        var strategy = ShouldRefreshPricingFactory.GetStrategy(priceRefreshContext).GetType().Name;
        
        //assert
        Assert.AreEqual(nameof(PreviewApprovedSentToFoRefreshPricingStrategy), strategy);
    }

    [TestMethod]
    public void ShouldRefreshPricingFactoryGetStrategy_StatusPreview_ReturnsVoidOrPostedStatusRefreshPricingStrategy()
    {
        //setup
        SalesLineEntity salesLine = new SalesLineEntity() { Status = SalesLineStatus.Preview };
        PriceRefreshContext priceRefreshContext = GetDefaultPriceRefreshContext(salesLine);
        //execute
        var strategy = ShouldRefreshPricingFactory.GetStrategy(priceRefreshContext).GetType().Name;
        
        //assert
        Assert.AreEqual(nameof(PreviewApprovedSentToFoRefreshPricingStrategy), strategy);
    }

    [TestMethod]
    public void ShouldRefreshPricingFactoryGetStrategy_StatusPosted_ReturnsVoidOrPostedStatusRefreshPricingStrategy()
    {
        //setup
        SalesLineEntity salesLine = new SalesLineEntity() { Status = SalesLineStatus.Posted };
        PriceRefreshContext priceRefreshContext = GetDefaultPriceRefreshContext(salesLine);

        //execute
        var strategy = ShouldRefreshPricingFactory.GetStrategy(priceRefreshContext).GetType().Name;
        
        //assert
        Assert.AreEqual(nameof(VoidOrPostedStatusRefreshPricingStrategy), strategy);
    }

    [TestMethod]
    public void ShouldRefreshPricingFactoryGetStrategy_StatusVoid_ReturnsVoidOrPostedStatusRefreshPricingStrategy()
    {
        //setup
        SalesLineEntity salesLine = new SalesLineEntity() { Status = SalesLineStatus.Void };
        PriceRefreshContext priceRefreshContext = GetDefaultPriceRefreshContext(salesLine);

        //execute
        var strategy = ShouldRefreshPricingFactory.GetStrategy(priceRefreshContext).GetType().Name;
        
        //assert
        Assert.AreEqual(nameof(VoidOrPostedStatusRefreshPricingStrategy), strategy);
    }

    [TestMethod]
    public void ShouldRefreshPricingFactoryGetStrategy_StatusUnspecified_ReturnsDefaultShouldRefreshPricingStrategy()
    {
        //setup
        SalesLineEntity salesLine = new SalesLineEntity() { Status = SalesLineStatus.Unspecified };
        PriceRefreshContext priceRefreshContext = GetDefaultPriceRefreshContext(salesLine);

        //execute
        var strategy = ShouldRefreshPricingFactory.GetStrategy(priceRefreshContext).GetType().Name;
        
        //assert
        Assert.AreEqual(nameof(DefaultShouldRefreshPricingStrategy), strategy);
    }


    [TestMethod]
    public void ShouldRefreshPricingFactoryGetStrategy_StatusException_ReturnsException()
    {
        //setup
        SalesLineEntity salesLine = new SalesLineEntity() { Status = SalesLineStatus.Exception };
        PriceRefreshContext priceRefreshContext = GetDefaultPriceRefreshContext(salesLine);

        //execute
        var strategy = ShouldRefreshPricingFactory.GetStrategy(priceRefreshContext).GetType().Name;
        
        //assert
        Assert.AreEqual(nameof(ExceptionStatusRefreshPricingStrategy), strategy);
    }

    [TestMethod]
    public void ShouldRefreshPricingFactoryGetStrategy_NullSalesLine_ReturnsDefault()
    {
        //setup
        SalesLineEntity salesLine = null;
        PriceRefreshContext priceRefreshContext = GetDefaultPriceRefreshContext(salesLine);

        //execute
        var strategy = ShouldRefreshPricingFactory.GetStrategy(priceRefreshContext).GetType().Name;
        
        //assert
        Assert.AreEqual(nameof(DefaultShouldRefreshPricingStrategy), strategy);
    }

    [TestMethod]
    public void GetSalesLinePricingRuleStrategy_WaterCutType_IncludeWater_ReturnsWaterStrategy()
    {
        //setup
        var serviceType = CreateServiceType(true, true, true);
        var salesLine = CreateSalesLine(SalesLineCutType.Water);

        //execute
        var factory = CutTypePricingRulesFactory.GetCutTypeRulesStrategy(serviceType, salesLine);
        var strategyName = factory.GetType().Name;
        
        //assert
        Assert.AreEqual(nameof(WaterSalesLinePricingRuleStrategy), strategyName);

    }
    
    [TestMethod]
    public void GetSalesLinePricingRuleStrategy_SolidCutType_IncludeSolid_ReturnsSolidStrategy()
    {
        //setup
        var serviceType = CreateServiceType(true, true, true);
        var salesLine = CreateSalesLine(SalesLineCutType.Solid);

        //execute
        var factory = CutTypePricingRulesFactory.GetCutTypeRulesStrategy(serviceType, salesLine);
        var strategyName = factory.GetType().Name;
        
        //assert
        Assert.AreEqual(nameof(SolidsSalesLinePricingRuleStrategy), strategyName);

    }
    
    [TestMethod]
    public void GetSalesLinePricingRuleStrategy_OilCutType_IncludeOil_ReturnsOilStrategy()
    {
        //setup
        var serviceType = CreateServiceType(true, true, true);
        var salesLine = CreateSalesLine(SalesLineCutType.Oil);

        //execute
        var factory = CutTypePricingRulesFactory.GetCutTypeRulesStrategy(serviceType, salesLine);
        var strategyName = factory.GetType().Name;
        
        //assert
        Assert.AreEqual(nameof(OilSalesLinePricingRuleStrategy), strategyName);

    }
    
    [TestMethod]
    public void GetSalesLinePricingRuleStrategy_OilCutType_DoesNotIncludeOil_ReturnsDefaultStrategy()
    {
        //setup
        var serviceType = CreateServiceType(false, true, true);
        var salesLine = CreateSalesLine(SalesLineCutType.Oil);

        //execute
        var factory = CutTypePricingRulesFactory.GetCutTypeRulesStrategy(serviceType, salesLine);
        var strategyName = factory.GetType().Name;
        
        //assert
        Assert.AreEqual(nameof(DefaultSalesLinePricingRuleStrategy), strategyName);

    }
    
    [TestMethod]
    public void GetSalesLinePricingRuleStrategy_SolidCutType_DoesNotIncludeSolid_ReturnsDefaultStrategy()
    {
        //setup
        var serviceType = CreateServiceType(true, false, true);
        var salesLine = CreateSalesLine(SalesLineCutType.Solid);

        //execute
        var factory = CutTypePricingRulesFactory.GetCutTypeRulesStrategy(serviceType, salesLine);
        var strategyName = factory.GetType().Name;
        
        //assert
        Assert.AreEqual(nameof(DefaultSalesLinePricingRuleStrategy), strategyName);
    }

    [TestMethod]
    public void GetSalesLinePricingRuleStrategy_WaterCutType_DoesNotIncludeWater_ReturnsDefaultStrategy()
    {
        //setup
        var serviceType = CreateServiceType(true, true, false);
        var salesLine = CreateSalesLine(SalesLineCutType.Water);

        //execute
        var factory = CutTypePricingRulesFactory.GetCutTypeRulesStrategy(serviceType, salesLine);
        var strategyName = factory.GetType().Name;
        
        //assert
        Assert.AreEqual(nameof(DefaultSalesLinePricingRuleStrategy), strategyName);
    }

    private static ServiceTypeEntity CreateServiceType(bool includesOil, bool includesSolids, bool includesWater)
    {
        return new()
        {
            IncludesOil = includesOil,
            IncludesSolids = includesSolids,
            IncludesWater = includesWater
        };
    }
    
    private static SalesLineEntity CreateSalesLine(SalesLineCutType cutType)
    {
        return new() { CutType = cutType };
    }
    
    [TestMethod]
    public void VoidTheSalesLines_NullsOutCorrectFields()
    {
        //setup
        var salesLine = new SalesLineEntity
        {
            InvoiceId = Guid.NewGuid(),
            ProformaInvoiceNumber = "Proforma Inv #",
            LoadConfirmationId = Guid.NewGuid(),
            LoadConfirmationNumber = "Some number",
            Status = SalesLineStatus.Approved,
            AwaitingRemovalAcknowledgment = false
        };

        //execute
        SalesLineManagerHelper.VoidTheSalesLine(salesLine);

        //assert
        Assert.IsNull(salesLine.InvoiceId);
        Assert.IsNull(salesLine.ProformaInvoiceNumber);
        Assert.IsNull(salesLine.LoadConfirmationId);
        Assert.IsNull(salesLine.LoadConfirmationNumber);
        Assert.IsTrue(salesLine.AwaitingRemovalAcknowledgment);
    }

    [TestMethod]
    public void GetPriceOrZero_ValidPrice()
    {
        //setup
        var price = new ComputePricingResponse { Price = 666 };

        //execute
        var result = SalesLineManagerHelper.GetPriceOrZero(price);

        //assert
        Assert.AreEqual(666, result);
    }

    [TestMethod]
    public void GetPriceOrZero_NullPrice()
    {
        //setup

        //execute
        var result = SalesLineManagerHelper.GetPriceOrZero(null);

        //assert
        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void SetSalesLineRateAndValues_PricingResponseIsNotNull()
    {
        //setup
        var salesLineId = Guid.NewGuid();
        var pricingRuleId = Guid.NewGuid();

        var salesLine = new SalesLineEntity {Id = salesLineId, Rate = 55.464, Quantity = 33.232};
        var pricing = new ComputePricingResponse {PricingRuleId = pricingRuleId, Price = 13};

        //execute
        SalesLineManagerHelper.SetSalesLineRateAndValues(salesLine, pricing);

        //assert
        Assert.IsNotNull(salesLine);
        Assert.AreEqual(13, salesLine.Rate);
        Assert.AreEqual(pricingRuleId, salesLine.PricingRuleId);
        Assert.IsFalse(salesLine.IsRateOverridden);
        Assert.AreEqual(salesLineId, salesLine.Id);
        Assert.AreEqual(33.23, salesLine.Quantity);
        Assert.AreEqual(431.99, salesLine.TotalValue);
        
    }

    [TestMethod]
    public void SetSalesLineRateAndValues_PricingResponseIsNull()
    {
        //setup
        var salesLineId = Guid.NewGuid();

        var salesLine = new SalesLineEntity {Id = salesLineId, Rate = 55.464, Quantity = 33.232};
        ComputePricingResponse pricing = null;

        //execute
        SalesLineManagerHelper.SetSalesLineRateAndValues(salesLine, pricing);

        //assert
        Assert.IsNotNull(salesLine);
        Assert.AreEqual(0, salesLine.Rate);
        Assert.IsNull(salesLine.PricingRuleId);
        Assert.IsFalse(salesLine.IsRateOverridden);
        Assert.AreEqual(salesLineId, salesLine.Id);
        Assert.AreEqual(33.23, salesLine.Quantity);
        Assert.AreEqual(0, salesLine.TotalValue);
    }

    [TestMethod]
    public void CanPriceBeRefreshed_False_ReturnsTrue()
    {
        //setup
        var salesLine = new SalesLineEntity {CanPriceBeRefreshed = true};

        //execute
        var result = SalesLineManagerHelper.GetSavedEntityValue(salesLine);
        
        //assert
        Assert.IsTrue(result, "Yes, refresh the price.");
    }

    [TestMethod]
    public void CanPriceBeRefreshed_False_ReturnsFalse()
    {
        //setup
        var salesLine = new SalesLineEntity {CanPriceBeRefreshed = false};

        //execute
        var result = SalesLineManagerHelper.GetSavedEntityValue(salesLine);
        
        //assert
        Assert.IsFalse(result, "Can not refresh price, no sir.");
    }

    [TestMethod]
    public void CanPriceBeRefreshed_Null_ReturnsFalse()
    {
        //setup
        var salesLine = new SalesLineEntity {CanPriceBeRefreshed = null};

        //execute
        var result = SalesLineManagerHelper.GetSavedEntityValue(salesLine);
        
        //assert
        Assert.IsFalse(result, "Can not refresh price, no sir.");
    }

    [TestMethod]
    public void ErrorOutSalesLineAndSetStatusToException()
    {
        //setup
        var salesLine = new SalesLineEntity { 
            Rate = 99,
            TotalValue = 88,
            Status = SalesLineStatus.Approved,
        };

        //execute
        SalesLineManagerHelper.ErrorOutSalesLineAndSetStatusToException(salesLine);

        //assert
        Assert.AreEqual(0, salesLine.Rate);
        Assert.AreEqual(0, salesLine.TotalValue);
        Assert.AreEqual(SalesLineStatus.Exception, salesLine.Status);
    }

    [TestMethod]
    public void FindAdditionalServiceOfThisProduct_ReturnsOne()
    {
        //setup
        var firstProductId = Guid.NewGuid();
        var secondProductId = Guid.NewGuid();
        var thirdProductId = Guid.NewGuid();

        var additionalServicesConfigsWithApplyZeroTotalVolume = new List<AdditionalServicesConfigurationEntity>();
        var firstList = new List<AdditionalServicesConfigurationAdditionalServiceEntity> { new() { ProductId = firstProductId } };
        var secondList = new List<AdditionalServicesConfigurationAdditionalServiceEntity> { new() { ProductId = secondProductId } };
        var thirdList = new List<AdditionalServicesConfigurationAdditionalServiceEntity> { new() { ProductId = thirdProductId} };

        additionalServicesConfigsWithApplyZeroTotalVolume.Add(new(){AdditionalServices = firstList});
        additionalServicesConfigsWithApplyZeroTotalVolume.Add(new(){AdditionalServices = secondList});
        additionalServicesConfigsWithApplyZeroTotalVolume.Add(new(){AdditionalServices = thirdList});

        var salesLine = new SalesLineEntity {ProductId = firstProductId};

        //execute
        var results = SalesLineManagerHelper.FindAdditionalServiceOfThisProduct(additionalServicesConfigsWithApplyZeroTotalVolume, salesLine.ProductId).ToList();

        //assert
        Assert.IsNotNull(results);
        Assert.AreEqual(1, results.Count);

        var foundAdditionalServices = results[0].AdditionalServices;
        Assert.IsNotNull(foundAdditionalServices);
        Assert.AreEqual(1, foundAdditionalServices.Count);
        Assert.AreEqual(firstProductId, foundAdditionalServices[0].ProductId);

    }

    [TestMethod]
    public void DoesNotHaveAnyAdditionalServicesForTruckTicket_OneAdditionalServices_ReturnsFalse()
    {
        //setup
        var truckTicket = new TruckTicketEntity { AdditionalServices = new() { new() } };

        //execute
        var result = SalesLineManagerHelper.DoesNotHaveAnyAdditionalServicesForTruckTicket(truckTicket);

        //assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void DoesNotHaveAnyAdditionalServicesForTruckTicket_EmptyAdditionalServices_ReturnsTrue()
    {
        //setup
        var truckTicket = new TruckTicketEntity {AdditionalServices = new()};


        //execute
        var result = SalesLineManagerHelper.DoesNotHaveAnyAdditionalServicesForTruckTicket(truckTicket);

        //assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void DoesNotHaveAnyAdditionalServicesForTruckTicket_NullAdditionalServices_ReturnsTrue()
    {
        //setup
        var truckTicket = new TruckTicketEntity();


        //execute
        var result = SalesLineManagerHelper.DoesNotHaveAnyAdditionalServicesForTruckTicket(truckTicket);

        //assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void FindAdditionalServiceOfThisProduct_ReturnsEmptyList()
    {
        //setup
        var productIdThatShouldntBeFound = Guid.NewGuid();
        var salesLine = new SalesLineEntity { ProductId = productIdThatShouldntBeFound };

        var additionalServicesConfigsWithApplyZeroTotalVolume = new List<AdditionalServicesConfigurationEntity>();
        
        var firstConfig = new AdditionalServicesConfigurationEntity();
        firstConfig.AdditionalServices.Add(new() { ProductId = Guid.NewGuid() });

        var secondConfig = new AdditionalServicesConfigurationEntity();
        secondConfig.AdditionalServices.Add(new() { ProductId = Guid.NewGuid() });

        additionalServicesConfigsWithApplyZeroTotalVolume.Add(firstConfig);
        additionalServicesConfigsWithApplyZeroTotalVolume.Add(secondConfig);

        //execute
        var results = SalesLineManagerHelper.FindAdditionalServiceOfThisProduct(additionalServicesConfigsWithApplyZeroTotalVolume, salesLine.ProductId);

        //assert
        Assert.IsNotNull(results, "results should not be null");
        Assert.AreEqual(0, results.ToList().Count);
    }

    [TestMethod]
    public void DoesNotHaveAnyAdditionalServicesConfigs_OneInList_ReturnsFalse()
    {
        //setup
        var list = new List<AdditionalServicesConfigurationEntity> { new AdditionalServicesConfigurationEntity() };

        //execute
        var results = SalesLineManagerHelper.DoesNotHaveAnyAdditionalServicesConfigs(list);

        //assert
        Assert.IsFalse(results, "should be false");
    }

    [TestMethod]
    public void DoesNotHaveAnyAdditionalServicesConfigs_EmptyList_ReturnsTrue()
    {
        //setup
        IEnumerable<AdditionalServicesConfigurationEntity> emptyList = new List<AdditionalServicesConfigurationEntity>();

        //execute
        var results = SalesLineManagerHelper.DoesNotHaveAnyAdditionalServicesConfigs(emptyList);

        //assert
        Assert.IsTrue(results, "should be true");
    }

    [TestMethod]
    public void DoesNotHaveAnyAdditionalServicesConfigs_NullList_ReturnsTrue()
    {
        //setup

        //execute
        var results = SalesLineManagerHelper.DoesNotHaveAnyAdditionalServicesConfigs(null);

        //assert
        Assert.IsTrue(results, "should be true");
    }

    [TestMethod]
    public void IsSalesLineAdditionalServiceWithZeroRateCutTypeTotal__NotTotal_ReturnsFalse()
    {
        //setup
        var salesLine = new SalesLineEntity { IsAdditionalService = true, Rate = 0, CutType = SalesLineCutType.Solid };

        //execute
        var result = SalesLineManagerHelper.IsSalesLineCutTypeTotalAndHasZeroRate(salesLine);

        //assert
        Assert.IsFalse(result, "should be false");
    }

    [TestMethod]
    public void IsSalesLineAdditionalServiceWithZeroRateAndCutTypeTotal__RateOfOne_ReturnsFalse()
    {
        //setup
        var salesLine = new SalesLineEntity { IsAdditionalService = true, Rate = 1, CutType = SalesLineCutType.Total };

        //execute
        var result = SalesLineManagerHelper.IsSalesLineCutTypeTotalAndHasZeroRate(salesLine);

        //assert
        Assert.IsFalse(result, "should be false");
    }

    [TestMethod]
    public void IsSalesLineAdditionalServiceWithZeroRateAndCutTypeTotal_IsNotAdditionalService_ReturnsTrue()
    {
        //setup
        var salesLine = new SalesLineEntity { IsAdditionalService = false, Rate = 0, CutType = SalesLineCutType.Total };

        //execute
        var result = SalesLineManagerHelper.IsSalesLineCutTypeTotalAndHasZeroRate(salesLine);

        //assert
        Assert.IsTrue(result, "should be true");
    }
    
    [TestMethod]
    public void CanPriceBeRefreshed_AdditionalConfigIsZeroTotal_IsAdditionalService_ReturnsFalse()
    {
        //setup
        var salesLine = new SalesLineEntity { IsAdditionalService = true, Rate = 0, CutType = SalesLineCutType.Total };

        //execute
        var result = SalesLineManagerHelper.CanPriceBeRefreshed(true, salesLine);

        //assert
        Assert.IsFalse(result, "should be False");
    }
    
    [TestMethod]
    public void CanPriceBeRefreshed_AdditionalConfigIsNotZeroTotal_IsNotAdditionalService_ReturnsTrue()
    {
        //setup
        var salesLine = new SalesLineEntity { IsAdditionalService = false, Rate = 0, CutType = SalesLineCutType.Total };

        //execute
        var result = SalesLineManagerHelper.CanPriceBeRefreshed(false, salesLine);

        //assert
        Assert.IsTrue(result, "should be True");
    }
    
     [TestMethod]
    public void IsSalesLineAdditionalServiceWithZeroRateAndCutTypeTotal_IsAdditionalServiceWithZeroRate_ReturnsTrue()
    {
        //setup
        var salesLine = new SalesLineEntity { IsAdditionalService = true, Rate = 0, CutType = SalesLineCutType.Total};

        //execute
        var result = SalesLineManagerHelper.IsSalesLineCutTypeTotalAndHasZeroRate(salesLine);

        //assert
        Assert.IsTrue(result, "should be true");
    }
    
    [TestMethod]
    public void DoesAdditionalServiceConfigHaveAnyAdditionalServices_OneAdditionalService_ReturnsTrue()
    {
        //setup
        var additionalService = new AdditionalServicesConfigurationAdditionalServiceEntity { Id = Guid.NewGuid() };

        var config = new AdditionalServicesConfigurationEntity();
        config.AdditionalServices.Add(additionalService);

        //execute
        var result = SalesLineManagerHelper.DoesAdditionalServiceConfigHaveAnyAdditionalServices(config);

        //assert
        Assert.IsTrue(result, "should be true");
    }
    
    [TestMethod]
    public void DoesAdditionalServiceConfigHaveAnyAdditionalServices_NoAdditionalServices_ReturnsFalse()
    {
        //setup
        var config = new AdditionalServicesConfigurationEntity();

        //execute
        var result = SalesLineManagerHelper.DoesAdditionalServiceConfigHaveAnyAdditionalServices(config);

        //assert
        Assert.IsFalse(result, "should be false");
    }

    [TestMethod]
    public void ShouldRefreshPricing_Solids_PriceIsNotZero_RefreshPricingTrue()
    {
        //setup
        var salesLine = new SalesLineEntity { CutType = SalesLineCutType.Solid, Rate = 0.1 };

        var serviceType = new ServiceTypeEntity { IncludesSolids = true };

        //execute
        var result = SalesLineManagerHelper.GetCutTypeRules(salesLine, serviceType);

        //assert
        Assert.IsTrue(result, "Should Refresh Pricing");
    }

    [TestMethod]
    public void ShouldRefreshPricing_Water_PriceIsNotZero_RefreshPricingTrue()
    {
        //setup
        var salesLine = new SalesLineEntity { CutType = SalesLineCutType.Water, Rate = 1 };

        var serviceType = new ServiceTypeEntity { IncludesWater = true };

        //execute
        var result = SalesLineManagerHelper.GetCutTypeRules(salesLine, serviceType);

        //assert
        Assert.IsTrue(result, "Should Refresh Pricing");
    }
    
    [TestMethod]
    public void ShouldRefreshPricing_Oil_PriceIsNotZero_RefreshPricingTrue()
    {
        //setup
        var salesLine = new SalesLineEntity { CutType = SalesLineCutType.Oil, Rate = -0.1 };

        var serviceType = new ServiceTypeEntity { IncludesOil = true };

        //execute
        var result = SalesLineManagerHelper.GetCutTypeRules(salesLine, serviceType);

        //assert
        Assert.IsTrue(result, "Should Refresh Pricing");
    }

    [TestMethod]
    public void ShouldRefreshPricing_Solid_PriceIsZero_QtyPctGreaterThanMin_RefreshPricingTrue()
    {
        //setup
        var salesLine = new SalesLineEntity { CutType = SalesLineCutType.Solid, Rate = 0, QuantityPercent = 2 };

        var serviceType = new ServiceTypeEntity { IncludesSolids = true, SolidMinPricingPercentage = 1 };

        //execute
        var result = SalesLineManagerHelper.GetCutTypeRules(salesLine, serviceType);

        //assert
        Assert.IsTrue(result, "Should Refresh Pricing");
    }

    [TestMethod]
    public void ShouldRefreshPricing_Solid_PriceIsZero_QtyPctEqualToMin_RefreshPricingTrue()
    {
        //setup
        var salesLine = new SalesLineEntity { CutType = SalesLineCutType.Solid, Rate = 0, QuantityPercent = 2 };

        var serviceType = new ServiceTypeEntity { IncludesSolids = true, SolidMinPricingPercentage = 2 };

        //execute
        var result = SalesLineManagerHelper.GetCutTypeRules(salesLine, serviceType);

        //assert
        Assert.IsTrue(result, "Should Refresh Pricing");
    }

    [TestMethod]
    public void ShouldRefreshPricing_Solid_PriceIsZero_QtyPctLessThanMin_RefreshPricingFalse()
    {
        //setup
        var salesLine = new SalesLineEntity { CutType = SalesLineCutType.Solid, Rate = 0, QuantityPercent = 1.99 };

        var serviceType = new ServiceTypeEntity { IncludesSolids = true, SolidMinPricingPercentage = 2 };

        //execute
        var result = SalesLineManagerHelper.GetCutTypeRules(salesLine, serviceType);

        //assert
        Assert.IsFalse(result, "Should NOT Refresh Pricing");
    }

    [TestMethod]
    public void ShouldRefreshPricing_Water_PriceIsZero_QtyPctGreaterThanMin_RefreshPricingTrue()
    {
        //setup
        var salesLine = new SalesLineEntity { CutType = SalesLineCutType.Water, Rate = 0, QuantityPercent = 2 };

        var serviceType = new ServiceTypeEntity { IncludesWater = true, WaterMinPricingPercentage = 1 };

        //execute
        var result = SalesLineManagerHelper.GetCutTypeRules(salesLine, serviceType);

        //assert
        Assert.IsTrue(result, "Should Refresh Pricing");
    }

    [TestMethod]
    public void ShouldRefreshPricing_Water_PriceIsZero_QtyPctEqualToMin_RefreshPricingTrue()
    {
        //setup
        var salesLine = new SalesLineEntity { CutType = SalesLineCutType.Water, Rate = 0, QuantityPercent = 2 };

        var serviceType = new ServiceTypeEntity { IncludesWater = true, WaterMinPricingPercentage = 2 };

        //execute
        var result = SalesLineManagerHelper.GetCutTypeRules(salesLine, serviceType);

        //assert
        Assert.IsTrue(result, "Should Refresh Pricing");
    }

    [TestMethod]
    public void ShouldRefreshPricing_Water_PriceIsZero_QtyPctLessThanMin_RefreshPricingFalse()
    {
        //setup
        var salesLine = new SalesLineEntity { CutType = SalesLineCutType.Water, Rate = 0, QuantityPercent = 1.99 };

        var serviceType = new ServiceTypeEntity { IncludesWater = true, WaterMinPricingPercentage = 2 };

        //execute
        var result = SalesLineManagerHelper.GetCutTypeRules(salesLine, serviceType);

        //assert
        Assert.IsFalse(result, "Should NOT Refresh Pricing");
    }

    [TestMethod]
    public void ShouldRefreshPricing_Oil_PriceIsZero_QtyGreaterThanMin_RefreshPricingTrue()
    {
        //setup
        var salesLine = new SalesLineEntity { CutType = SalesLineCutType.Oil, Rate = 0, Quantity = 2 };

        var serviceType = new ServiceTypeEntity { IncludesOil = true, OilCreditMinVolume = 1 };

        //execute
        var result = SalesLineManagerHelper.GetCutTypeRules(salesLine, serviceType);

        //assert
        Assert.IsTrue(result, "Should Refresh Pricing");
    }

    [TestMethod]
    public void ShouldRefreshPricing_Oil_PriceIsZero_QtyEqualToMin_RefreshPricingTrue()
    {
        //setup
        var salesLine = new SalesLineEntity { CutType = SalesLineCutType.Oil, Rate = 0, Quantity = 2 };

        var serviceType = new ServiceTypeEntity { IncludesOil = true, OilCreditMinVolume = 2 };

        //execute
        var result = SalesLineManagerHelper.GetCutTypeRules(salesLine, serviceType);

        //assert
        Assert.IsTrue(result, "Should Refresh Pricing");
    }

    [TestMethod]
    public void ShouldRefreshPricing_Oil_PriceIsZero_QtyLessThanMin_RefreshPricingFalse()
    {
        //setup
        var salesLine = new SalesLineEntity { CutType = SalesLineCutType.Oil, Rate = 0, Quantity = 1.99 };

        var serviceType = new ServiceTypeEntity { IncludesOil = true, OilCreditMinVolume = 2 };

        //execute
        var result = SalesLineManagerHelper.GetCutTypeRules(salesLine, serviceType);

        //assert
        Assert.IsFalse(result, "Should NOT Refresh Pricing");
    }

    [TestMethod]
    public void ShouldRefreshPricing_Oil_PriceIsNegativeTwo_AbsValueQtyMoreThanMin_RefreshPricingFalse()
    {
        //setup
        var salesLine = new SalesLineEntity { CutType = SalesLineCutType.Oil, Rate = 0, Quantity = -2 };

        var serviceType = new ServiceTypeEntity { IncludesOil = true, OilCreditMinVolume = 1 };

        //execute
        var result = SalesLineManagerHelper.GetCutTypeRules(salesLine, serviceType);

        //assert
        Assert.IsTrue(result, "YES, Refresh Pricing");
    }

    [TestMethod]
    public void ShouldRefreshPricing_Oil_PriceIsNegativeTwo_QtyLessThanMin_RefreshPricingFalse()
    {
        //setup
        var salesLine = new SalesLineEntity { CutType = SalesLineCutType.Oil, Rate = 0, Quantity = -2 };

        var serviceType = new ServiceTypeEntity { IncludesOil = true, OilCreditMinVolume = 3 };

        //execute
        var result = SalesLineManagerHelper.GetCutTypeRules(salesLine, serviceType);

        //assert
        Assert.IsFalse(result, "No do NOT Refresh Pricing");
    }


    private static PriceRefreshContext GetDefaultPriceRefreshContext(SalesLineEntity salesLine)
    {
        return new PriceRefreshContext
        {
            BillingConfigurationProvider = null,
            ServiceTypeManager = null,
            LoadConfirmationProvider = null,
            CurrentSalesLine = salesLine
        };
    }
}
