using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.AdditionalServicesConfiguration;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.ServiceType;
using SE.Shared.Domain.Entities.SourceLocation;
using SE.Shared.Domain.PricingRules;
using SE.Shared.Domain.Product;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Domain.Entities.SalesLine;

namespace SE.TruckTicketing.Domain.Tests.SalesLine;

[TestClass]
public class SalesLineInitializerTests
{
    [TestMethod]
    public void InitializeAdditionalServiceSalesLine_ZeroTotalConfigFalse_CanPriceBeRefreshedTrue_ReturnsPriceFromProductMap()
    {
        //setup
        var totalProductId = Guid.NewGuid();
        var serviceTypeId = Guid.NewGuid();
        var totalItem01Id = Guid.NewGuid();

        var totalProduct = new ProductEntity
        {
            Id = totalProductId,
            Name = "Total Product Name",
            Number = "Total Product Number",
            UnitOfMeasure = "mm"
        };

        var serviceType = new ServiceTypeEntity
        {
            Id = serviceTypeId,
            TotalThresholdType = SubstanceThresholdType.Percentage,
            IncludesSolids = true,
            ServiceTypeId = "Landfill Disposal - Industrial Waste",
            TotalItemId = totalItem01Id,
        };

        var context = new SalesLinePreviewRequestContext
        {
            Facility = new() { Type = FacilityType.Lf },
            PricingByProductNumberMap = new(),
            ServiceType = serviceType
        };

        var price1 = new ComputePricingResponse { Price = 33.50 };
        var price2 = new ComputePricingResponse { Price = 44 };
        var price3 = new ComputePricingResponse { Price = 55.75 };

        context.PricingByProductNumberMap.Add(totalProduct.Number, price1);
        context.PricingByProductNumberMap.Add("Second Product Number", price2);
        context.PricingByProductNumberMap.Add("Some other third thing", price3);

        var additionalServicesConfig = new AdditionalServicesConfig { ZeroTotal = false };

        var salesLineBase = new SalesLineEntity();

        var request = new SalesLinePreviewRequest
        {
            TruckTicket = new()
            {
                TruckTicketType = TruckTicketType.LF,
                NetWeight = 1,
                LoadVolume = 2,
            }
        };
        
        //execute
        var result = SalesLineInitializer.InitializeAdditionalServiceSalesLine(context, totalProduct, additionalServicesConfig, salesLineBase, request);

        //assert
        Assert.IsNotNull(result);
        Assert.AreEqual(totalProductId, result.ProductId);
        Assert.AreEqual("Total Product Name", result.ProductName);
        Assert.AreEqual("Total Product Number", result.ProductNumber);
        Assert.AreEqual(0, result.Quantity);
        Assert.AreEqual("mm", result.UnitOfMeasure);
        Assert.AreEqual(33.50, result.Rate);
    }

    [TestMethod]
    public void InitializeAdditionalServiceSalesLine_ZeroTotalConfigTrue_CanPriceBeRefreshedTrue_ReturnsZeroRate()
    {
        //setup
        var totalProductId = Guid.NewGuid();

        var context = new SalesLinePreviewRequestContext { Facility = new() { Type = FacilityType.Lf } };

        var totalProduct = new ProductEntity
        {
            Id = totalProductId,
            Name = "Total Product Name",
            Number = "Total Product Number",
            UnitOfMeasure = "mm"
        };

        var additionalServicesConfig = new AdditionalServicesConfig { ZeroTotal = true };

        var salesLineBase = new SalesLineEntity();

        var request = new SalesLinePreviewRequest
        {
            TruckTicket = new()
            {
                TruckTicketType = TruckTicketType.LF,
                NetWeight = 1,
                LoadVolume = 2,
            }
        };

        //execute
        var result = SalesLineInitializer.InitializeAdditionalServiceSalesLine(context, totalProduct, additionalServicesConfig, salesLineBase, request);

        //assert
        Assert.IsNotNull(result);
        Assert.AreEqual(totalProductId, result.ProductId);
        Assert.AreEqual("Total Product Name", result.ProductName);
        Assert.AreEqual("Total Product Number", result.ProductNumber);
        Assert.AreEqual(0, result.Quantity);
        Assert.AreEqual("mm", result.UnitOfMeasure);
        Assert.AreEqual(0, result.Rate);
    }

    [TestMethod]
    public void InitializeTotalSalesLine_ZeroTotalConfigFalse_CanPriceBeRefreshedNotSet_ReturnsPriceFromProductMap()
    {
        //setup
        var totalProductId = Guid.NewGuid();
        var serviceTypeId = Guid.NewGuid();
        var totalItem01Id = Guid.NewGuid();

        var totalProduct = new ProductEntity
        {
            Id = totalProductId,
            Name = "Total Product Name",
            Number = "Total Product Number",
            UnitOfMeasure = "mm"
        };

        var serviceType = new ServiceTypeEntity
        {
            Id = serviceTypeId,
            TotalThresholdType = SubstanceThresholdType.Percentage,
            IncludesSolids = false,
            ServiceTypeId = "Landfill Disposal - Industrial Waste",
            TotalItemId = totalItem01Id,
        };

        var context = new SalesLinePreviewRequestContext
        {
            Facility = new() { Type = FacilityType.Lf },
            PricingByProductNumberMap = new(),
            ServiceType = serviceType
        };

        var price1 = new ComputePricingResponse { Price = 33.50 };
        var price2 = new ComputePricingResponse { Price = 44 };
        var price3 = new ComputePricingResponse { Price = 55.75 };

        context.PricingByProductNumberMap.Add(totalProduct.Number, price1);
        context.PricingByProductNumberMap.Add("Second Product Number", price2);
        context.PricingByProductNumberMap.Add("Some other third thing", price3);

        var additionalServicesConfig = new AdditionalServicesConfig { ZeroTotal = false };

        var salesLineBase = new SalesLineEntity();

        var request = new SalesLinePreviewRequest
        {
            TruckTicket = new()
            {
                TruckTicketType = TruckTicketType.LF,
                NetWeight = 1,
                LoadVolume = 2,
            },
            TotalVolume = 2,
        };

        //execute
        var result = SalesLineInitializer.InitializeTotalSalesLine(context, request, salesLineBase, additionalServicesConfig, totalProduct);

        //assert
        Assert.IsNotNull(result);
        Assert.AreEqual(totalProductId, result.ProductId);
        Assert.AreEqual("Total Product Name", result.ProductName);
        Assert.AreEqual("Total Product Number", result.ProductNumber);
        Assert.AreEqual(2, result.Quantity);
        Assert.AreEqual("mm", result.UnitOfMeasure);
        Assert.AreEqual(33.50, result.Rate);
    }

    [TestMethod]
    public void InitializeWaterSalesLine_ZeroWaterConfigTrue_WaterItemReverseTrue_ReturnsZeroPrice()
    {
        //setup
        var oilProductId = Guid.NewGuid();
        var serviceTypeId = Guid.NewGuid();
        var oilItemId = Guid.NewGuid();
        var waterItemId = Guid.NewGuid();
        var waterProductId = Guid.NewGuid();

        var oilProduct = new ProductEntity
        {
            Id = oilProductId,
            Name = "Oil Product Name",
            Number = "Oil Product Number",
            UnitOfMeasure = "mm"
        };

        var waterProduct = new ProductEntity
        {
            Id = waterProductId,
            Name = "Water Product Name",
            Number = "Water Product Number",
            UnitOfMeasure = "m3"
        };

        var serviceType = new ServiceTypeEntity
        {
            Id = serviceTypeId,
            TotalThresholdType = SubstanceThresholdType.Percentage,
            IncludesSolids = false,
            IncludesOil = true,
            ServiceTypeId = "Landfill Disposal - Industrial Waste",
            OilItemId = oilItemId,
            OilItemReverse = true,
            WaterItemId = waterItemId,
            WaterItemName = "Water Item Name"
        };

        var context = new SalesLinePreviewRequestContext
        {
            Facility = new() { Type = FacilityType.Lf },
            PricingByProductNumberMap = new(),
            ServiceType = serviceType,
            ProductMap = new()
        };

        var price1 = new ComputePricingResponse { Price = 33.50 };
        var price2 = new ComputePricingResponse { Price = 44 };
        var price3 = new ComputePricingResponse { Price = 55.75 };
        var price4 = new ComputePricingResponse { Price = 88 };

        context.PricingByProductNumberMap.Add(oilProduct.Number, price1);
        context.PricingByProductNumberMap.Add(waterProduct.Number, price4);
        context.PricingByProductNumberMap.Add("Second Product Number", price2);
        context.PricingByProductNumberMap.Add("Some other third thing", price3);

        context.ProductMap.Add(serviceType.OilItemId, oilProduct);
        context.ProductMap.Add(serviceType.WaterItemId, waterProduct);

        var additionalServicesConfig = new AdditionalServicesConfig { ZeroWater = true };

        var salesLineBase = new SalesLineEntity();

        var request = new SalesLinePreviewRequest
        {
            TruckTicket = new()
            {
                TruckTicketType = TruckTicketType.LF,
                NetWeight = 1,
                LoadVolume = 2,
            },
            TotalVolume = 2,
            WaterVolume = 5,
            WaterVolumePercent = 46,
        };

        //execute
        var result = SalesLineInitializer.InitializeWaterSalesLine(context, request, serviceType, additionalServicesConfig, salesLineBase);

        //assert
        Assert.IsNotNull(result);
        Assert.AreEqual(waterProductId, result.ProductId);
        Assert.AreEqual("Water Product Name", result.ProductName);
        Assert.AreEqual("Water Product Number", result.ProductNumber);
        Assert.AreEqual(5, result.Quantity);
        Assert.AreEqual(46, result.QuantityPercent);
        Assert.AreEqual("m3", result.UnitOfMeasure);
        Assert.AreEqual(0, result.Rate);
    }

    [TestMethod]
    public void InitializeSolidSalesLine_ZeroSolidConfigTrue_SolidItemReverseTrue_ReturnsZeroPrice()
    {
        //setup
        var oilProductId = Guid.NewGuid();
        var serviceTypeId = Guid.NewGuid();
        var oilItemId = Guid.NewGuid();
        var solidItemId = Guid.NewGuid();
        var solidProductId = Guid.NewGuid();

        var oilProduct = new ProductEntity
        {
            Id = oilProductId,
            Name = "Oil Product Name",
            Number = "Oil Product Number",
            UnitOfMeasure = "mm"
        };

        var solidProduct = new ProductEntity
        {
            Id = solidProductId,
            Name = "Solid Product Name",
            Number = "Solid Product Number",
            UnitOfMeasure = "m3"
        };

        var serviceType = new ServiceTypeEntity
        {
            Id = serviceTypeId,
            TotalThresholdType = SubstanceThresholdType.Percentage,
            IncludesSolids = false,
            IncludesOil = true,
            ServiceTypeId = "Landfill Disposal - Industrial Waste",
            OilItemId = oilItemId,
            OilItemReverse = true,
            SolidItemId = solidItemId,
            SolidItemName = "Solid Item Name"
        };

        var context = new SalesLinePreviewRequestContext
        {
            Facility = new() { Type = FacilityType.Lf },
            PricingByProductNumberMap = new(),
            ServiceType = serviceType,
            ProductMap = new()
        };

        var price1 = new ComputePricingResponse { Price = 33.50 };
        var price2 = new ComputePricingResponse { Price = 44 };
        var price3 = new ComputePricingResponse { Price = 55.75 };
        var price4 = new ComputePricingResponse { Price = 88 };

        context.PricingByProductNumberMap.Add(oilProduct.Number, price1);
        context.PricingByProductNumberMap.Add(solidProduct.Number, price4);
        context.PricingByProductNumberMap.Add("Second Product Number", price2);
        context.PricingByProductNumberMap.Add("Some other third thing", price3);

        context.ProductMap.Add(serviceType.OilItemId, oilProduct);
        context.ProductMap.Add(serviceType.SolidItemId, solidProduct);

        var additionalServicesConfig = new AdditionalServicesConfig { ZeroSolids = true };

        var salesLineBase = new SalesLineEntity();

        var request = new SalesLinePreviewRequest
        {
            TruckTicket = new()
            {
                TruckTicketType = TruckTicketType.LF,
                NetWeight = 1,
                LoadVolume = 2,
            },
            TotalVolume = 2,
            SolidVolume = 5,
            SolidVolumePercent = 46,
        };

        //execute
        var result = SalesLineInitializer.InitializeSolidSalesLine(context, request, serviceType, additionalServicesConfig, salesLineBase);

        //assert
        Assert.IsNotNull(result);
        Assert.AreEqual(solidProductId, result.ProductId);
        Assert.AreEqual("Solid Product Name", result.ProductName);
        Assert.AreEqual("Solid Product Number", result.ProductNumber);
        Assert.AreEqual(5, result.Quantity);
        Assert.AreEqual(46, result.QuantityPercent);
        Assert.AreEqual("m3", result.UnitOfMeasure);
        Assert.AreEqual(0, result.Rate);
    }

    [TestMethod]
    public void InitializeOilSalesLine_ZeroOilConfigTrue_OilItemReverseTrue_ReturnsZeroPrice()
    {
        //setup
        var oilProductId = Guid.NewGuid();
        var serviceTypeId = Guid.NewGuid();
        var oilItemId = Guid.NewGuid();
        var waterProductId = Guid.NewGuid();

        var oilProduct = new ProductEntity
        {
            Id = oilProductId,
            Name = "Oil Product Name",
            Number = "Oil Product Number",
            UnitOfMeasure = "mm"
        };

        var waterProduct = new ProductEntity
        {
            Id = waterProductId,
            Name = "Water Product Name",
            Number = "Water Product Number",
            UnitOfMeasure = "mm"
        };

        var serviceType = new ServiceTypeEntity
        {
            Id = serviceTypeId,
            TotalThresholdType = SubstanceThresholdType.Percentage,
            IncludesSolids = false,
            IncludesOil = true,
            ServiceTypeId = "Landfill Disposal - Industrial Waste",
            OilItemId = oilItemId,
            OilItemReverse = true,
        };

        var context = new SalesLinePreviewRequestContext
        {
            Facility = new() { Type = FacilityType.Lf },
            PricingByProductNumberMap = new(),
            ServiceType = serviceType,
            ProductMap = new()
        };

        var price1 = new ComputePricingResponse { Price = 33.50 };
        var price2 = new ComputePricingResponse { Price = 44 };
        var price3 = new ComputePricingResponse { Price = 55.75 };
        var price4 = new ComputePricingResponse { Price = 88 };

        context.PricingByProductNumberMap.Add(oilProduct.Number, price1);
        context.PricingByProductNumberMap.Add(waterProduct.Number, price4);
        context.PricingByProductNumberMap.Add("Second Product Number", price2);
        context.PricingByProductNumberMap.Add("Some other third thing", price3);

        context.ProductMap.Add(serviceType.OilItemId, oilProduct);
        context.ProductMap.Add(serviceType.WaterItemId, waterProduct);

        var additionalServicesConfig = new AdditionalServicesConfig { ZeroOil = true };

        var salesLineBase = new SalesLineEntity();

        var request = new SalesLinePreviewRequest
        {
            TruckTicket = new()
            {
                TruckTicketType = TruckTicketType.LF,
                NetWeight = 1,
                LoadVolume = 2,
            },
            TotalVolume = 2,
            OilVolume = 5,
            OilVolumePercent = 46,
        };

        //execute
        var result = SalesLineInitializer.InitializeOilSalesLine(context, serviceType, request, additionalServicesConfig, salesLineBase);

        //assert
        Assert.IsNotNull(result);
        Assert.AreEqual(oilProductId, result.ProductId);
        Assert.AreEqual("Oil Product Name", result.ProductName);
        Assert.AreEqual("Oil Product Number", result.ProductNumber);
        Assert.AreEqual(-5, result.Quantity);
        Assert.AreEqual(46, result.QuantityPercent);
        Assert.AreEqual("mm", result.UnitOfMeasure);
        Assert.AreEqual(0, result.Rate);
    }

    [TestMethod]
    public void InitializeOilSalesLine_ZeroOilConfigFalse_OilItemReverseTrue_ReturnsPriceFromMap()
    {
        //setup
        var oilProductId = Guid.NewGuid();
        var serviceTypeId = Guid.NewGuid();
        var oilItemId = Guid.NewGuid();
        var waterProductId = Guid.NewGuid();

        var oilProduct = new ProductEntity
        {
            Id = oilProductId,
            Name = "Oil Product Name",
            Number = "Oil Product Number",
            UnitOfMeasure = "mm"
        };

        var waterProduct = new ProductEntity
        {
            Id = waterProductId,
            Name = "Water Product Name",
            Number = "Water Product Number",
            UnitOfMeasure = "mm"
        };

        var serviceType = new ServiceTypeEntity
        {
            Id = serviceTypeId,
            TotalThresholdType = SubstanceThresholdType.Percentage,
            IncludesSolids = false,
            IncludesOil = true,
            ServiceTypeId = "Landfill Disposal - Industrial Waste",
            OilItemId = oilItemId,
            OilItemReverse = true,
        };

        var context = new SalesLinePreviewRequestContext
        {
            Facility = new() { Type = FacilityType.Lf },
            PricingByProductNumberMap = new(),
            ServiceType = serviceType,
            ProductMap = new()
        };

        var price1 = new ComputePricingResponse { Price = 33.50 };
        var price2 = new ComputePricingResponse { Price = 44 };
        var price3 = new ComputePricingResponse { Price = 55.75 };
        var price4 = new ComputePricingResponse { Price = 88 };

        context.PricingByProductNumberMap.Add(oilProduct.Number, price1);
        context.PricingByProductNumberMap.Add(waterProduct.Number, price4);
        context.PricingByProductNumberMap.Add("Second Product Number", price2);
        context.PricingByProductNumberMap.Add("Some other third thing", price3);

        context.ProductMap.Add(serviceType.OilItemId, oilProduct);
        context.ProductMap.Add(serviceType.WaterItemId, waterProduct);

        var additionalServicesConfig = new AdditionalServicesConfig { ZeroOil = false };

        var salesLineBase = new SalesLineEntity();

        var request = new SalesLinePreviewRequest
        {
            TruckTicket = new()
            {
                TruckTicketType = TruckTicketType.LF,
                NetWeight = 1,
                LoadVolume = 2,
            },
            TotalVolume = 2,
            OilVolume = 5,
            OilVolumePercent = 46,
        };

        //execute
        var result = SalesLineInitializer.InitializeOilSalesLine(context, serviceType, request, additionalServicesConfig, salesLineBase);

        //assert
        Assert.IsNotNull(result);
        Assert.AreEqual(oilProductId, result.ProductId);
        Assert.AreEqual("Oil Product Name", result.ProductName);
        Assert.AreEqual("Oil Product Number", result.ProductNumber);
        Assert.AreEqual(-5, result.Quantity);
        Assert.AreEqual(46, result.QuantityPercent);
        Assert.AreEqual("mm", result.UnitOfMeasure);
        Assert.AreEqual(33.50, result.Rate);
    }

    [TestMethod]
    public void InitializeWaterSalesLine_ZeroWaterConfigFalse_WaterItemReverseTrue_ReturnsPriceFromMap()
    {
        //setup
        var oilProductId = Guid.NewGuid();
        var serviceTypeId = Guid.NewGuid();
        var oilItemId = Guid.NewGuid();
        var waterItemId = Guid.NewGuid();
        var waterProductId = Guid.NewGuid();

        var oilProduct = new ProductEntity
        {
            Id = oilProductId,
            Name = "Oil Product Name",
            Number = "Oil Product Number",
            UnitOfMeasure = "mm"
        };

        var waterProduct = new ProductEntity
        {
            Id = waterProductId,
            Name = "Water Product Name",
            Number = "Water Product Number",
            UnitOfMeasure = "m3"
        };

        var serviceType = new ServiceTypeEntity
        {
            Id = serviceTypeId,
            TotalThresholdType = SubstanceThresholdType.Percentage,
            IncludesSolids = false,
            IncludesOil = true,
            ServiceTypeId = "Landfill Disposal - Industrial Waste",
            OilItemId = oilItemId,
            OilItemReverse = true,
            WaterItemId = waterItemId,
            WaterItemName = "Water Item Name"
        };

        var context = new SalesLinePreviewRequestContext
        {
            Facility = new() { Type = FacilityType.Lf },
            PricingByProductNumberMap = new(),
            ServiceType = serviceType,
            ProductMap = new()
        };

        var price1 = new ComputePricingResponse { Price = 33.50 };
        var price2 = new ComputePricingResponse { Price = 44 };
        var price3 = new ComputePricingResponse { Price = 55.75 };
        var price4 = new ComputePricingResponse { Price = 88 };

        context.PricingByProductNumberMap.Add(oilProduct.Number, price1);
        context.PricingByProductNumberMap.Add(waterProduct.Number, price4);
        context.PricingByProductNumberMap.Add("Second Product Number", price2);
        context.PricingByProductNumberMap.Add("Some other third thing", price3);

        context.ProductMap.Add(serviceType.OilItemId, oilProduct);
        context.ProductMap.Add(serviceType.WaterItemId, waterProduct);

        var additionalServicesConfig = new AdditionalServicesConfig { ZeroWater = false };

        var salesLineBase = new SalesLineEntity();

        var request = new SalesLinePreviewRequest
        {
            TruckTicket = new()
            {
                TruckTicketType = TruckTicketType.LF,
                NetWeight = 1,
                LoadVolume = 2,
            },
            TotalVolume = 2,
            WaterVolume = 5,
            WaterVolumePercent = 46,
        };

        //execute
        var result = SalesLineInitializer.InitializeWaterSalesLine(context, request, serviceType, additionalServicesConfig, salesLineBase);

        //assert
        Assert.IsNotNull(result);
        Assert.AreEqual(waterProductId, result.ProductId);
        Assert.AreEqual("Water Product Name", result.ProductName);
        Assert.AreEqual("Water Product Number", result.ProductNumber);
        Assert.AreEqual(5, result.Quantity);
        Assert.AreEqual(46, result.QuantityPercent);
        Assert.AreEqual("m3", result.UnitOfMeasure);
        Assert.AreEqual(88, result.Rate);
    }
    
    [TestMethod]
    public void InitializeSolidSalesLine_ZeroSolidConfigFalse_SolidItemReverseTrue_ReturnsPriceFromMap()
    {
        //setup
        var oilProductId = Guid.NewGuid();
        var serviceTypeId = Guid.NewGuid();
        var oilItemId = Guid.NewGuid();
        var solidItemId = Guid.NewGuid();
        var solidProductId = Guid.NewGuid();

        var oilProduct = new ProductEntity
        {
            Id = oilProductId,
            Name = "Oil Product Name",
            Number = "Oil Product Number",
            UnitOfMeasure = "mm"
        };

        var solidProduct = new ProductEntity
        {
            Id = solidProductId,
            Name = "Solid Product Name",
            Number = "Solid Product Number",
            UnitOfMeasure = "cm"
        };

        var serviceType = new ServiceTypeEntity
        {
            Id = serviceTypeId,
            TotalThresholdType = SubstanceThresholdType.Percentage,
            IncludesSolids = false,
            IncludesOil = true,
            ServiceTypeId = "Landfill Disposal - Industrial Waste",
            OilItemId = oilItemId,
            OilItemReverse = true,
            SolidItemId = solidItemId,
            SolidItemName = "Solid Item Name",
            SolidMinValue = 1,
            SolidMinPricingPercentage = 5,
        };

        var context = new SalesLinePreviewRequestContext
        {
            Facility = new() { Type = FacilityType.Lf },
            PricingByProductNumberMap = new(),
            ServiceType = serviceType,
            ProductMap = new()
        };

        var price1 = new ComputePricingResponse { Price = 33.50 };
        var price2 = new ComputePricingResponse { Price = 44 };
        var price3 = new ComputePricingResponse { Price = 55.75 };
        var price4 = new ComputePricingResponse { Price = 88 };

        context.PricingByProductNumberMap.Add(oilProduct.Number, price1);
        context.PricingByProductNumberMap.Add(solidProduct.Number, price4);
        context.PricingByProductNumberMap.Add("Second Product Number", price2);
        context.PricingByProductNumberMap.Add("Some other third thing", price3);

        context.ProductMap.Add(serviceType.OilItemId, oilProduct);
        context.ProductMap.Add(serviceType.SolidItemId, solidProduct);

        var additionalServicesConfig = new AdditionalServicesConfig { ZeroSolids = false };

        var salesLineBase = new SalesLineEntity();

        var request = new SalesLinePreviewRequest
        {
            TruckTicket = new()
            {
                TruckTicketType = TruckTicketType.LF,
                NetWeight = 1,
                LoadVolume = 2,
            },
            TotalVolume = 2,
            SolidVolume = 5,
            SolidVolumePercent = 46,
        };

        //execute
        var result = SalesLineInitializer.InitializeSolidSalesLine(context, request, serviceType, additionalServicesConfig, salesLineBase);

        //assert
        Assert.IsNotNull(result);
        Assert.AreEqual(solidProductId, result.ProductId);
        Assert.AreEqual("Solid Product Name", result.ProductName);
        Assert.AreEqual("Solid Product Number", result.ProductNumber);
        Assert.AreEqual(5, result.Quantity);
        Assert.AreEqual(46, result.QuantityPercent);
        Assert.AreEqual("cm", result.UnitOfMeasure);
        Assert.AreEqual(88, result.Rate);
    }
    
    [TestMethod]
    public void InitializeWithProductAndCutLine_ShouldInitializeAdditionalService_NullPriceAndCutLine_StatusException()
    {
        //setup
        var productId = Guid.NewGuid();

        var salesLineBase = new SalesLineEntity {Quantity = 11, QuantityPercent = 12, Status = SalesLineStatus.Preview};
        ProductEntity product = new ProductEntity {Id = productId, Name = "product name", Number = "product number", UnitOfMeasure = "m3"};
        ComputePricingResponse price = null;

        //execute
        var result = SalesLineInitializer.Instance.Initialize(salesLineBase, product, price, 3, 300, false);

        //assert
        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.Quantity);
        Assert.AreEqual(300, result.QuantityPercent);
        Assert.AreEqual(0, result.Rate);
        Assert.IsNull(result.PricingRuleId);
        Assert.AreEqual(false, result.IsCutLine);
        Assert.AreEqual(productId, result.ProductId);
        Assert.AreEqual("product name", result.ProductName);
        Assert.AreEqual("product number", result.ProductNumber);
        Assert.AreEqual("m3", result.UnitOfMeasure);
        Assert.AreEqual(SalesLineStatus.Exception, result.Status);
        Assert.IsFalse(result.IsAdditionalService);
    }

    [TestMethod]
    public void InitializeAdditionalServiceSalesLine()
    {
        //setup
        var pricingRuleId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var salesLineBase = new SalesLineEntity {Quantity = 11, QuantityPercent = 12, Status = SalesLineStatus.Preview};
        ProductEntity product = new ProductEntity {Id = productId, Name = "product name", Number = "product number", UnitOfMeasure = "m3"};
        ComputePricingResponse price = new ComputePricingResponse {Price = 556, PricingRuleId = pricingRuleId};

        //execute
        var result = SalesLineInitializer.Instance.InitializeAdditionalService(salesLineBase, product, price);

        //assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Quantity);
        Assert.AreEqual(100, result.QuantityPercent);
        Assert.AreEqual(556, result.Rate);
        Assert.AreEqual(pricingRuleId, result.PricingRuleId);
        Assert.IsFalse(result.IsCutLine);
        Assert.AreEqual(productId, result.ProductId);
        Assert.AreEqual("product name", result.ProductName);
        Assert.AreEqual("product number", result.ProductNumber);
        Assert.AreEqual("m3", result.UnitOfMeasure);
        Assert.AreEqual(SalesLineStatus.Preview, result.Status);
        Assert.IsTrue(result.IsAdditionalService);
        
    }

    [TestMethod]
    public void InitializeTotalSalesLine()
    {
        //setup
        var pricingRuleId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var salesLineBase = new SalesLineEntity {Quantity = 11, QuantityPercent = 12, Status = SalesLineStatus.Preview};
        ProductEntity product = new ProductEntity {Id = productId, Name = "product name", Number = "product number", UnitOfMeasure = "m3"};
        ComputePricingResponse price = new ComputePricingResponse {Price = 556, PricingRuleId = pricingRuleId};

        //execute
        var result = SalesLineInitializer.Instance.InitializeTotalSalesLine(salesLineBase, product, price, 3, 300);

        //assert
        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.Quantity);
        Assert.AreEqual(300, result.QuantityPercent);
        Assert.AreEqual(556, result.Rate);
        Assert.AreEqual(pricingRuleId, result.PricingRuleId);
        Assert.IsFalse(result.IsCutLine);
        Assert.AreEqual(productId, result.ProductId);
        Assert.AreEqual("product name", result.ProductName);
        Assert.AreEqual("product number", result.ProductNumber);
        Assert.AreEqual("m3", result.UnitOfMeasure);
        Assert.AreEqual(SalesLineStatus.Preview, result.Status);
        Assert.AreEqual(SalesLineCutType.Total, result.CutType);
        Assert.IsFalse(result.IsAdditionalService);
    }

    [TestMethod]
    public void InitializeSolidSalesLine()
    {
        //setup
        var pricingRuleId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var salesLineBase = new SalesLineEntity {Quantity = 11, QuantityPercent = 12, Status = SalesLineStatus.Preview};
        ProductEntity product = new ProductEntity {Id = productId, Name = "product name", Number = "product number", UnitOfMeasure = "m3"};
        ComputePricingResponse price = new ComputePricingResponse {Price = 556, PricingRuleId = pricingRuleId};

        //execute
        var result = SalesLineInitializer.Instance.InitializeSolidSalesLine(salesLineBase, product, price, 3, 300);

        //assert
        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.Quantity);
        Assert.AreEqual(300, result.QuantityPercent);
        Assert.AreEqual(556, result.Rate);
        Assert.AreEqual(pricingRuleId, result.PricingRuleId);
        Assert.IsTrue(result.IsCutLine);
        Assert.AreEqual(productId, result.ProductId);
        Assert.AreEqual("product name", result.ProductName);
        Assert.AreEqual("product number", result.ProductNumber);
        Assert.AreEqual("m3", result.UnitOfMeasure);
        Assert.AreEqual(SalesLineStatus.Preview, result.Status);
        Assert.AreEqual(SalesLineCutType.Solid, result.CutType);
        Assert.IsFalse(result.IsAdditionalService);
    }

    [TestMethod]
    public void InitializeWaterSalesLine()
    {
        //setup
        var pricingRuleId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var salesLineBase = new SalesLineEntity {Quantity = 11, QuantityPercent = 12, Status = SalesLineStatus.Preview};
        ProductEntity product = new ProductEntity {Id = productId, Name = "product name", Number = "product number", UnitOfMeasure = "m3"};
        ComputePricingResponse price = new ComputePricingResponse {Price = 556, PricingRuleId = pricingRuleId};

        //execute
        var result = SalesLineInitializer.Instance.InitializeWaterSalesLine(salesLineBase, product, price, 3, 300);

        //assert
        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.Quantity);
        Assert.AreEqual(300, result.QuantityPercent);
        Assert.AreEqual(556, result.Rate);
        Assert.AreEqual(pricingRuleId, result.PricingRuleId);
        Assert.IsTrue(result.IsCutLine);
        Assert.AreEqual(productId, result.ProductId);
        Assert.AreEqual("product name", result.ProductName);
        Assert.AreEqual("product number", result.ProductNumber);
        Assert.AreEqual("m3", result.UnitOfMeasure);
        Assert.AreEqual(SalesLineStatus.Preview, result.Status);
        Assert.AreEqual(SalesLineCutType.Water, result.CutType);
        Assert.IsFalse(result.IsAdditionalService);
    }

    [TestMethod]
    public void InitializeOilSalesLine()
    {
        //setup
        var pricingRuleId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var salesLineBase = new SalesLineEntity {Quantity = 11, QuantityPercent = 12, Status = SalesLineStatus.Preview};
        ProductEntity product = new ProductEntity {Id = productId, Name = "product name", Number = "product number", UnitOfMeasure = "m3"};
        ComputePricingResponse price = new ComputePricingResponse {Price = 556, PricingRuleId = pricingRuleId};

        //execute
        var result = SalesLineInitializer.Instance.InitializeOilSalesLine(salesLineBase, product, price, 2, 200);

        //assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Quantity);
        Assert.AreEqual(200, result.QuantityPercent);
        Assert.AreEqual(556, result.Rate);
        Assert.AreEqual(pricingRuleId, result.PricingRuleId);
        Assert.IsTrue(result.IsCutLine);
        Assert.AreEqual(productId, result.ProductId);
        Assert.AreEqual("product name", result.ProductName);
        Assert.AreEqual("product number", result.ProductNumber);
        Assert.AreEqual("m3", result.UnitOfMeasure);
        Assert.AreEqual(SalesLineStatus.Preview, result.Status);
        Assert.AreEqual(SalesLineCutType.Oil, result.CutType);
        Assert.IsFalse(result.IsAdditionalService);
    }

    [TestMethod]
    public void InitializeWithProductAndCutLine_ShouldInitializeAdditionalServiceWithProductAndCutLine()
    {
        //setup
        var pricingRuleId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var salesLineBase = new SalesLineEntity {Quantity = 11, QuantityPercent = 12, Status = SalesLineStatus.Preview};
        ProductEntity product = new ProductEntity {Id = productId, Name = "product name", Number = "product number", UnitOfMeasure = "m3"};
        ComputePricingResponse price = new ComputePricingResponse {Price = 556, PricingRuleId = pricingRuleId};

        //execute
        var result = SalesLineInitializer.Instance.Initialize(salesLineBase, product, price, 3, 300, false);

        //assert
        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.Quantity);
        Assert.AreEqual(300, result.QuantityPercent);
        Assert.AreEqual(556, result.Rate);
        Assert.AreEqual(pricingRuleId, result.PricingRuleId);
        Assert.AreEqual(false, result.IsCutLine);
        Assert.AreEqual(productId, result.ProductId);
        Assert.AreEqual("product name", result.ProductName);
        Assert.AreEqual("product number", result.ProductNumber);
        Assert.AreEqual("m3", result.UnitOfMeasure);
        Assert.AreEqual(SalesLineStatus.Preview, result.Status);
        Assert.IsFalse(result.IsAdditionalService);
    }

    [TestMethod]
    public void ShouldInitializeSalesLine_NoSourceLocationFormattedId_PreviewStatus()
    {
        //setup
        var truckTicketId = Guid.NewGuid();
        var truckTicketDate = new DateTime(2001, 1, 1, 1, 1, 1);
        var generatorId = Guid.NewGuid();
        var ediFieldDefinitionId = Guid.NewGuid();
        var ediId = Guid.NewGuid();
        var incorrectMaterialApprovalId = Guid.NewGuid();
        var materialApprovalId = Guid.NewGuid();
        var incorrectServiceTypeId = Guid.NewGuid();
        var serviceTypeId = Guid.NewGuid();
        var truckingCompanyId = Guid.NewGuid();
        var sourceLocationId = Guid.NewGuid();
        var facilityId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        SalesLinePreviewRequest request = new SalesLinePreviewRequest
        {
            TruckTicket = new()
            {
                Id = truckTicketId,
                WellClassification = WellClassifications.Drilling,
                LoadDate = DateTimeOffset.UtcNow, //skipping asserting dates until we use a Clock wrapper class
                EffectiveDate = new DateTime(2001, 1, 1, 1, 1, 1),
                GeneratorId = generatorId,
                GeneratorName = "Gen Name",
                TicketNumber = "Truck Ticket #1",
                EdiFieldValues = new ()
                {
                    new()
                    {
                        Id = ediId,
                        EDIFieldDefinitionId = ediFieldDefinitionId,
                        EDIFieldName = "Edi Field Name goes here",
                        EDIFieldValueContent = "blah"
                    }
                },
                IsEdiValid = true,
                DowNonDow = DowNonDow.Dow,
                BillOfLading = "BoL LoL",
                ManifestNumber = "Mannie Num",
                TareWeight = 25.36,
                GrossWeight = 87.54,
                MaterialApprovalId = incorrectMaterialApprovalId,
                MaterialApprovalNumber = "TT Mat App Num",
                ServiceTypeId = incorrectServiceTypeId,
                SubstanceName = "Rat Poison",
                BillingCustomerId = customerId,
                BillingCustomerName = "Husky Energy",

            },
            TareWeight = 88.87,
            GrossWeight = 77.66,
            MaterialApprovalId = materialApprovalId,
            MaterialApprovalNumber = "Mat App Num",
            ServiceTypeId = serviceTypeId,
            ServiceTypeName = "Serv Type Name",
            TruckingCompanyId = truckingCompanyId,
            TruckingCompanyName = "TC Name"

        };

        FacilityEntity facility = new FacilityEntity { Id = facilityId, SiteId = "Some Site Id", BusinessUnitId = "Biz 66", Division = "SESK", LegalEntity = "LE09"};
        SourceLocationEntity sourceLocation = new SourceLocationEntity{Id = sourceLocationId, Identifier = "The Identifier", SourceLocationTypeName = "The type name", SourceLocationName = "SLN"};
        AccountEntity customer = new AccountEntity {CustomerNumber = "ACN", AccountNumber = "Account#"};

        //execute
        var result = SalesLineInitializer.Instance.InitializeSalesLine(request, facility, sourceLocation, customer);

        //assert
        Assert.IsNotNull(result, "Should not be null");
        Assert.AreEqual(truckTicketId, result.TruckTicketId);
        Assert.AreEqual(WellClassifications.Drilling, result.WellClassification);
        Assert.AreEqual(truckTicketDate, result.TruckTicketEffectiveDate);
        Assert.AreEqual(generatorId, result.GeneratorId);
        Assert.AreEqual("Gen Name", result.GeneratorName);
        Assert.AreEqual("Truck Ticket #1", result.TruckTicketNumber);
        Assert.IsNotNull(result.EdiFieldValues);
        Assert.AreEqual(1, result.EdiFieldValues.Count);
        Assert.AreEqual(ediId, result.EdiFieldValues[0].Id);
        Assert.AreEqual(ediFieldDefinitionId, result.EdiFieldValues[0].EDIFieldDefinitionId);
        Assert.AreEqual("Edi Field Name goes here", result.EdiFieldValues[0].EDIFieldName);
        Assert.AreEqual("blah", result.EdiFieldValues[0].EDIFieldValueContent);
        Assert.IsTrue(result.IsEdiValid);
        Assert.AreEqual(DowNonDow.Dow, result.DowNonDow);
        Assert.AreEqual("BoL LoL", result.BillOfLading);
        Assert.AreEqual("Mannie Num", result.ManifestNumber);
        Assert.AreEqual(88.87, result.TareWeight);
        Assert.AreEqual(77.66, result.GrossWeight);
        Assert.AreEqual(materialApprovalId, result.MaterialApprovalId);
        Assert.AreEqual("Mat App Num", result.MaterialApprovalNumber);
        Assert.AreEqual(serviceTypeId, result.ServiceTypeId);
        Assert.AreEqual("Serv Type Name", result.ServiceTypeName);
        Assert.AreEqual(truckingCompanyId, result.TruckingCompanyId);
        Assert.AreEqual("TC Name", result.TruckingCompanyName);
        Assert.AreEqual("Rat Poison", result.Substance);
        Assert.IsFalse(result.IsRateOverridden);
        Assert.AreEqual(sourceLocationId, result.SourceLocationId);
        Assert.AreEqual("SLN", result.SourceLocationFormattedIdentifier);
        Assert.AreEqual("The Identifier", result.SourceLocationIdentifier);
        Assert.AreEqual("The type name", result.SourceLocationTypeName);
        Assert.AreEqual(facilityId, result.FacilityId);
        Assert.AreEqual("Some Site Id", result.FacilitySiteId);
        Assert.AreEqual("Biz 66", result.BusinessUnit);
        Assert.AreEqual("SESK", result.Division);
        Assert.AreEqual("LE09", result.LegalEntity);
        Assert.AreEqual(customerId, result.CustomerId);
        Assert.AreEqual("Husky Energy", result.CustomerName);
        Assert.AreEqual("ACN", result.CustomerNumber);
        Assert.AreEqual("Account#", result.AccountNumber);
        Assert.IsFalse(result.IsAdditionalService, "This should not be an additional service");
        Assert.AreEqual(SalesLineStatus.Preview, result.Status);
    }

    [TestMethod]
    public void ShouldInitializeSalesLine_SourceLocationFormattedId_PreviewStatus()
    {
        //setup
        var truckTicketId = Guid.NewGuid();
        var truckTicketDate = new DateTime(2001, 1, 1, 1, 1, 1);
        var generatorId = Guid.NewGuid();
        var ediFieldDefinitionId = Guid.NewGuid();
        var ediId = Guid.NewGuid();
        var incorrectMaterialApprovalId = Guid.NewGuid();
        var materialApprovalId = Guid.NewGuid();
        var incorrectServiceTypeId = Guid.NewGuid();
        var serviceTypeId = Guid.NewGuid();
        var truckingCompanyId = Guid.NewGuid();
        var sourceLocationId = Guid.NewGuid();
        var facilityId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        SalesLinePreviewRequest request = new SalesLinePreviewRequest
        {
            TruckTicket = new()
            {
                Id = truckTicketId,
                WellClassification = WellClassifications.Drilling,
                LoadDate = DateTimeOffset.UtcNow, //skipping asserting dates until we use a Clock wrapper class
                EffectiveDate = new DateTime(2001, 1, 1, 1, 1, 1),
                GeneratorId = generatorId,
                GeneratorName = "Gen Name",
                TicketNumber = "Truck Ticket #1",
                EdiFieldValues = new ()
                {
                    new()
                    {
                        Id = ediId,
                        EDIFieldDefinitionId = ediFieldDefinitionId,
                        EDIFieldName = "Edi Field Name goes here",
                        EDIFieldValueContent = "blah"
                    }
                },
                IsEdiValid = true,
                DowNonDow = DowNonDow.Dow,
                BillOfLading = "BoL LoL",
                ManifestNumber = "Mannie Num",
                TareWeight = 25.36,
                GrossWeight = 87.54,
                MaterialApprovalId = incorrectMaterialApprovalId,
                MaterialApprovalNumber = "TT Mat App Num",
                ServiceTypeId = incorrectServiceTypeId,
                SubstanceName = "Rat Poison",
                BillingCustomerId = customerId,
                BillingCustomerName = "Husky Energy",

            },
            TareWeight = 88.87,
            GrossWeight = 77.66,
            MaterialApprovalId = materialApprovalId,
            MaterialApprovalNumber = "Mat App Num",
            ServiceTypeId = serviceTypeId,
            ServiceTypeName = "Serv Type Name",
            TruckingCompanyId = truckingCompanyId,
            TruckingCompanyName = "TC Name"

        };

        FacilityEntity facility = new FacilityEntity { Id = facilityId, SiteId = "Some Site Id", BusinessUnitId = "Biz 66", Division = "SESK", LegalEntity = "LE09"};
        SourceLocationEntity sourceLocation = new SourceLocationEntity{Id = sourceLocationId, FormattedIdentifier = "FIder", Identifier = "The Identifier", SourceLocationTypeName = "The type name"};
        AccountEntity customer = new AccountEntity {CustomerNumber = "ACN", AccountNumber = "Account#"};

        //execute
        var result = SalesLineInitializer.Instance.InitializeSalesLine(request, facility, sourceLocation, customer);

        //assert
        Assert.IsNotNull(result, "Should not be null");
        Assert.AreEqual(truckTicketId, result.TruckTicketId);
        Assert.AreEqual(WellClassifications.Drilling, result.WellClassification);
        Assert.AreEqual(truckTicketDate, result.TruckTicketEffectiveDate);
        Assert.AreEqual(generatorId, result.GeneratorId);
        Assert.AreEqual("Gen Name", result.GeneratorName);
        Assert.AreEqual("Truck Ticket #1", result.TruckTicketNumber);
        Assert.IsNotNull(result.EdiFieldValues);
        Assert.AreEqual(1, result.EdiFieldValues.Count);
        Assert.AreEqual(ediId, result.EdiFieldValues[0].Id);
        Assert.AreEqual(ediFieldDefinitionId, result.EdiFieldValues[0].EDIFieldDefinitionId);
        Assert.AreEqual("Edi Field Name goes here", result.EdiFieldValues[0].EDIFieldName);
        Assert.AreEqual("blah", result.EdiFieldValues[0].EDIFieldValueContent);
        Assert.IsTrue(result.IsEdiValid);
        Assert.AreEqual(DowNonDow.Dow, result.DowNonDow);
        Assert.AreEqual("BoL LoL", result.BillOfLading);
        Assert.AreEqual("Mannie Num", result.ManifestNumber);
        Assert.AreEqual(88.87, result.TareWeight);
        Assert.AreEqual(77.66, result.GrossWeight);
        Assert.AreEqual(materialApprovalId, result.MaterialApprovalId);
        Assert.AreEqual("Mat App Num", result.MaterialApprovalNumber);
        Assert.AreEqual(serviceTypeId, result.ServiceTypeId);
        Assert.AreEqual("Serv Type Name", result.ServiceTypeName);
        Assert.AreEqual(truckingCompanyId, result.TruckingCompanyId);
        Assert.AreEqual("TC Name", result.TruckingCompanyName);
        Assert.AreEqual("Rat Poison", result.Substance);
        Assert.IsFalse(result.IsRateOverridden);
        Assert.AreEqual(sourceLocationId, result.SourceLocationId);
        Assert.AreEqual("FIder", result.SourceLocationFormattedIdentifier);
        Assert.AreEqual("The Identifier", result.SourceLocationIdentifier);
        Assert.AreEqual("The type name", result.SourceLocationTypeName);
        Assert.AreEqual(facilityId, result.FacilityId);
        Assert.AreEqual("Some Site Id", result.FacilitySiteId);
        Assert.AreEqual("Biz 66", result.BusinessUnit);
        Assert.AreEqual("SESK", result.Division);
        Assert.AreEqual("LE09", result.LegalEntity);
        Assert.AreEqual(customerId, result.CustomerId);
        Assert.AreEqual("Husky Energy", result.CustomerName);
        Assert.AreEqual("ACN", result.CustomerNumber);
        Assert.AreEqual("Account#", result.AccountNumber);
        Assert.IsFalse(result.IsAdditionalService, "This should not be an additional service");
        Assert.AreEqual(SalesLineStatus.Preview, result.Status);
    }

    [TestMethod]
    public void ShouldInitializeAdditionalService_PullQuantityFromTicketFalse_UsesAdditionalServiceQuantity()
    {
        //setup
        Guid pricingRuleId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();

        SalesLineEntity salesLineBase = new SalesLineEntity();
        AdditionalServicesConfigurationAdditionalServiceEntity additionalServicesConfig = new AdditionalServicesConfigurationAdditionalServiceEntity
        {
            ProductId = productId, Name = "Test Product Name", Number = "456", UnitOfMeasure = "m3", PullQuantityFromTicket = false, Quantity = 12
        };
        SalesLinePreviewRequest request = new SalesLinePreviewRequest { TruckTicket = new() { NetWeight = 33, LoadVolume = 58} };
        ComputePricingResponse price = new ComputePricingResponse {PricingRuleId = pricingRuleId, Price = 44.50};

        FacilityType facilityType = FacilityType.Cavern;
        const bool isReadOnlyLine = true;

        //execute
        var result = SalesLineInitializer.Instance.InitializeAdditionalService(salesLineBase, additionalServicesConfig, request, price, facilityType, isReadOnlyLine);

        //assert
        Assert.IsNotNull(result);
        Assert.AreEqual(12, result.Quantity);
        Assert.AreEqual(44.50, result.Rate);
        Assert.AreEqual(pricingRuleId, result.PricingRuleId);
        Assert.IsFalse(result.IsCutLine);
        Assert.IsTrue(result.IsAdditionalService);
        Assert.AreEqual(productId, result.ProductId);
        Assert.AreEqual("Test Product Name", result.ProductName);
        Assert.AreEqual("456", result.ProductNumber);
        Assert.AreEqual("m3", result.UnitOfMeasure);
        Assert.IsTrue(result.IsReadOnlyLine);
        Assert.IsTrue(result.CanPriceBeRefreshed);

    }
  
    [TestMethod]
    public void ShouldInitializeAdditionalService_PullQuantityFromTicketNull_UsesAdditionalServiceQuantity()
    {
        //setup
        Guid pricingRuleId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();

        SalesLineEntity salesLineBase = new SalesLineEntity();
        AdditionalServicesConfigurationAdditionalServiceEntity additionalServicesConfig = new AdditionalServicesConfigurationAdditionalServiceEntity
        {
            ProductId = productId, Name = "Test Product Name", Number = "456", UnitOfMeasure = "m3", PullQuantityFromTicket = null, Quantity = 12
        };
        SalesLinePreviewRequest request = new SalesLinePreviewRequest { TruckTicket = new() { NetWeight = 33, LoadVolume = 58} };
        ComputePricingResponse price = new ComputePricingResponse {PricingRuleId = pricingRuleId, Price = 44.50};

        FacilityType facilityType = FacilityType.Cavern;
        const bool isReadOnlyLine = true;

        //execute
        var result = SalesLineInitializer.Instance.InitializeAdditionalService(salesLineBase, additionalServicesConfig, request, price, facilityType, isReadOnlyLine);

        //assert
        Assert.IsNotNull(result);
        Assert.AreEqual(12, result.Quantity);
        Assert.AreEqual(44.50, result.Rate);
        Assert.AreEqual(pricingRuleId, result.PricingRuleId);
        Assert.IsFalse(result.IsCutLine);
        Assert.IsTrue(result.IsAdditionalService);
        Assert.AreEqual(productId, result.ProductId);
        Assert.AreEqual("Test Product Name", result.ProductName);
        Assert.AreEqual("456", result.ProductNumber);
        Assert.AreEqual("m3", result.UnitOfMeasure);
        Assert.IsTrue(result.IsReadOnlyLine);
        Assert.IsTrue(result.CanPriceBeRefreshed);

    }
  
    [TestMethod]
    public void ShouldInitializeAdditionalService_FacilityTypeCavern_UsesLoadVolume()
    {
        //setup
        Guid pricingRuleId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();

        SalesLineEntity salesLineBase = new SalesLineEntity();
        AdditionalServicesConfigurationAdditionalServiceEntity additionalServicesConfig = new AdditionalServicesConfigurationAdditionalServiceEntity
        {
            ProductId = productId, Name = "Test Product Name", Number = "456", UnitOfMeasure = "m3", PullQuantityFromTicket = true
        };
        SalesLinePreviewRequest request = new SalesLinePreviewRequest { TruckTicket = new() { NetWeight = 33, LoadVolume = 58} };
        ComputePricingResponse price = new ComputePricingResponse {PricingRuleId = pricingRuleId, Price = 44.50};

        FacilityType facilityType = FacilityType.Cavern;
        const bool isReadOnlyLine = true;

        //execute
        var result = SalesLineInitializer.Instance.InitializeAdditionalService(salesLineBase, additionalServicesConfig, request, price, facilityType, isReadOnlyLine);

        //assert
        Assert.IsNotNull(result);
        Assert.AreEqual(58, result.Quantity);
        Assert.AreEqual(44.50, result.Rate);
        Assert.AreEqual(pricingRuleId, result.PricingRuleId);
        Assert.IsFalse(result.IsCutLine);
        Assert.IsTrue(result.IsAdditionalService);
        Assert.AreEqual(productId, result.ProductId);
        Assert.AreEqual("Test Product Name", result.ProductName);
        Assert.AreEqual("456", result.ProductNumber);
        Assert.AreEqual("m3", result.UnitOfMeasure);
        Assert.AreEqual(true, result.IsReadOnlyLine);
        Assert.IsTrue(result.CanPriceBeRefreshed);

    }
 
    [TestMethod]
    public void ShouldInitializeAdditionalService_FacilityTypeLf_UsesNetWeight()
    {
        //setup
        Guid pricingRuleId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();

        SalesLineEntity salesLineBase = new SalesLineEntity();
        AdditionalServicesConfigurationAdditionalServiceEntity additionalServicesConfig = new AdditionalServicesConfigurationAdditionalServiceEntity
        {
            ProductId = productId, Name = "Test Product Name", Number = "456", UnitOfMeasure = "m3", PullQuantityFromTicket = true
        };
        SalesLinePreviewRequest request = new SalesLinePreviewRequest { TruckTicket = new() { NetWeight = 33 } };
        ComputePricingResponse price = new ComputePricingResponse {PricingRuleId = pricingRuleId, Price = 44.50};

        FacilityType facilityType = FacilityType.Lf;
        const bool isReadOnlyLine = true;

        //execute
        var result = SalesLineInitializer.Instance.InitializeAdditionalService(salesLineBase, additionalServicesConfig, request, price, facilityType, isReadOnlyLine);

        //assert
        Assert.IsNotNull(result);
        Assert.AreEqual(33, result.Quantity);
        Assert.AreEqual(44.50, result.Rate);
        Assert.AreEqual(pricingRuleId, result.PricingRuleId);
        Assert.IsFalse(result.IsCutLine);
        Assert.IsTrue(result.IsAdditionalService);
        Assert.AreEqual(productId, result.ProductId);
        Assert.AreEqual("Test Product Name", result.ProductName);
        Assert.AreEqual("456", result.ProductNumber);
        Assert.AreEqual("m3", result.UnitOfMeasure);
        Assert.IsTrue(result.IsReadOnlyLine);
        Assert.IsTrue(result.CanPriceBeRefreshed);

    }

}
