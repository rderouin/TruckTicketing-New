using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SE.Shared.Domain.Entities.AdditionalServicesConfiguration;
using SE.Shared.Domain.Product;
using SE.TruckTicketing.Domain.Entities.SalesLine;

namespace SE.TruckTicketing.Domain.Tests.SalesLine;

[TestClass]
public class AdditionalServiceConfigInitializerTests
{
    [TestMethod]
    public void ShouldInitializeAdditionalServicesConfigurationEntityInitializerFromValidConfigs()
    {
        //setup
        AdditionalServicesConfigurationAdditionalServiceEntity additionalService1 = new AdditionalServicesConfigurationAdditionalServiceEntity
        {
            Id = Guid.NewGuid()
        };
        AdditionalServicesConfigurationAdditionalServiceEntity additionalService2 = new AdditionalServicesConfigurationAdditionalServiceEntity
        {
            Id = Guid.NewGuid()
        };
        AdditionalServicesConfigurationAdditionalServiceEntity additionalService3 = new AdditionalServicesConfigurationAdditionalServiceEntity
        {
            Id = Guid.NewGuid()
        };
        AdditionalServicesConfigurationAdditionalServiceEntity additionalService4 = new AdditionalServicesConfigurationAdditionalServiceEntity
        {
            Id = Guid.NewGuid()
        };
        AdditionalServicesConfigurationAdditionalServiceEntity additionalService5 = new AdditionalServicesConfigurationAdditionalServiceEntity
        {
            Id = Guid.NewGuid()
        };

        AdditionalServicesConfigurationEntity totalVolumeConfig = new AdditionalServicesConfigurationEntity
        {
            ApplyZeroTotalVolume = true,
            ApplyZeroOilVolume = false,
            ApplyZeroWaterVolume = false,
            ApplyZeroSolidsVolume = false,
            AdditionalServices = new(){ additionalService1}
        };

        AdditionalServicesConfigurationEntity oilVolumeConfig = new AdditionalServicesConfigurationEntity
        {
            ApplyZeroTotalVolume = false,
            ApplyZeroOilVolume = true,
            ApplyZeroWaterVolume = false,
            ApplyZeroSolidsVolume = false,
            AdditionalServices = new(){ additionalService2}
        };

        AdditionalServicesConfigurationEntity waterVolumeConfig = new AdditionalServicesConfigurationEntity
        {
            ApplyZeroTotalVolume = false,
            ApplyZeroOilVolume = false,
            ApplyZeroWaterVolume = true,
            ApplyZeroSolidsVolume = false,
            AdditionalServices = new(){ additionalService3}
        };

        AdditionalServicesConfigurationEntity solidVolumeConfig = new AdditionalServicesConfigurationEntity
        {
            ApplyZeroTotalVolume = false,
            ApplyZeroOilVolume = false,
            ApplyZeroWaterVolume = false,
            ApplyZeroSolidsVolume = true,
            AdditionalServices = new(){ additionalService4}
        };

        AdditionalServicesConfigurationEntity otherVolumeConfig = new AdditionalServicesConfigurationEntity
        {
            ApplyZeroTotalVolume = false,
            ApplyZeroOilVolume = false,
            ApplyZeroWaterVolume = false,
            ApplyZeroSolidsVolume = false,
            AdditionalServices = new(){ additionalService5}
        };

        List<AdditionalServicesConfigurationEntity> validConfigs = new List<AdditionalServicesConfigurationEntity>
        {
            totalVolumeConfig, oilVolumeConfig, waterVolumeConfig, solidVolumeConfig, otherVolumeConfig
        };

        //execute
        var result = AdditionalServicesConfigurationEntityInitializer.Instance.Initialize(validConfigs);

        //assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ZeroTotal);
        Assert.IsTrue(result.ZeroOil);
        Assert.IsTrue(result.ZeroWater);
        Assert.IsTrue(result.ZeroSolids);
        Assert.AreEqual(5, result.AdditionalServices.Count);
    }

    [TestMethod]
    public void ShouldInitializeAdditionalServicesConfigAdditionalServiceEntityNewFromTotalProduct()
    {
        //setup
        var productId = Guid.NewGuid();

        ProductEntity totalProduct = new ProductEntity()
        {
            Id = productId,
            Name = "Total Product Name",
            Number = "TPNum",
            UnitOfMeasure = "cm"
        };

        //execute
        var result = AdditionalServicesConfigurationAdditionalServiceEntityInitializer.Instance.InitializeNew(totalProduct);

        //assert
        Assert.IsNotNull(result);
        Assert.AreEqual(productId, result.ProductId);
        Assert.AreEqual("Total Product Name", result.Name);
        Assert.AreEqual("TPNum", result.Number);
        Assert.AreEqual(0, result.Quantity);
        Assert.AreEqual("cm", result.UnitOfMeasure);
        Assert.IsFalse(result.PullQuantityFromTicket);
    }
}
