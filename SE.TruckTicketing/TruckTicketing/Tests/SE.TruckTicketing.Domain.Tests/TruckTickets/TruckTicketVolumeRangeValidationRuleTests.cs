using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain;
using SE.Shared.Domain.Entities.ServiceType;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.TruckTicket.Rules;

using Trident.Business;
using Trident.Contracts;
using Trident.Extensions;
using Trident.Testing.TestScopes;
using Trident.Validation;

namespace SE.TruckTicketing.Domain.Tests.TruckTickets;

[TestClass]
public class TruckTicketVolumeRangeValidationRuleTests
{
    [TestMethod]
    public async Task Task_ShouldRunWithNoValidation_WhenTicketStatusIsOpen()
    {
        //arrange
        var scope = new DefaultScope();
        var truckEntity = GenFu.GenFu.New<TruckTicketEntity>();
        var serviceTypeEntity = GenFu.GenFu.New<ServiceTypeEntity>();
        scope.SetServiceManagerMock(serviceTypeEntity);

        truckEntity.Status = TruckTicketStatus.Open;
        var context = new BusinessContext<TruckTicketEntity>(truckEntity);
        var validationResults = new List<ValidationResult>();
        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //Assert
        Assert.AreEqual(0, validationResults.Count);
    }

    [TestMethod]
    public async Task Task_ShouldRunWithNoValidation_WhenTicketNotManualSource()
    {
        //arrange
        var scope = new DefaultScope();
        var truckEntity = GenFu.GenFu.New<TruckTicketEntity>();
        var serviceTypeEntity = GenFu.GenFu.New<ServiceTypeEntity>();
        scope.SetServiceManagerMock(serviceTypeEntity);
        truckEntity.TruckTicketType = TruckTicketType.WT;
        truckEntity.Source = TruckTicketSource.Scaled;
        var context = new BusinessContext<TruckTicketEntity>(truckEntity);
        var validationResults = new List<ValidationResult>();
        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //Assert
        Assert.AreEqual(0, validationResults.Count);
    }

    [TestMethod]
    public async Task Task_ShouldRunWithNoValidation_WhenTicketStatusApprovedAndManualSourceAndNotWTTicketType()
    {
        //arrange
        var scope = new DefaultScope();
        var truckEntity = GenFu.GenFu.New<TruckTicketEntity>();
        var serviceTypeEntity = GenFu.GenFu.New<ServiceTypeEntity>();
        scope.SetServiceManagerMock(serviceTypeEntity);

        truckEntity.TruckTicketType = TruckTicketType.SP;
        var context = new BusinessContext<TruckTicketEntity>(truckEntity);
        var validationResults = new List<ValidationResult>();
        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //Assert
        Assert.AreEqual(0, validationResults.Count);
    }

    [TestMethod]
    public async Task Task_ShouldRunWithNoValidation_WhenTicketIsWTTypeNoCutIsIncluded()
    {
        //arrange
        var scope = new DefaultScope();
        var truckEntity = GenFu.GenFu.New<TruckTicketEntity>();
        var serviceTypeEntity = GenFu.GenFu.New<ServiceTypeEntity>();
        serviceTypeEntity.IncludesOil = false;
        serviceTypeEntity.IncludesSolids = false;
        serviceTypeEntity.IncludesWater = false;
        scope.ServiceManagerMock.Setup(x => x.GetById(It.IsAny<Guid?>(), false))
             .ReturnsAsync(serviceTypeEntity);

        truckEntity.TruckTicketType = TruckTicketType.WT;
        var context = new BusinessContext<TruckTicketEntity>(truckEntity);
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //Assert
        Assert.AreEqual(0, validationResults.Count);
    }

    //Oil Fixed
    [TestMethod]
    public async Task Task_ShouldRunWithValidation_WhenServiceType_OilFixed_MinMaxGiven()
    {
        //arrange
        var scope = new DefaultScope();
        var truckEntity = GenFu.GenFu.New<TruckTicketEntity>();
        var serviceTypeEntity = GenFu.GenFu.New<ServiceTypeEntity>();
        truckEntity.ServiceTypeId = serviceTypeEntity.Id;
        serviceTypeEntity.IncludesOil = true;
        serviceTypeEntity.OilMinValue = 10.0;
        serviceTypeEntity.OilMaxValue = 20.0;
        serviceTypeEntity.OilThresholdType = SubstanceThresholdType.Fixed;

        serviceTypeEntity.IncludesSolids = false;
        serviceTypeEntity.IncludesWater = false;
        scope.ServiceManagerMock.Setup(x => x.GetById(It.IsAny<Guid?>(), false))
             .ReturnsAsync(serviceTypeEntity);

        truckEntity.TruckTicketType = TruckTicketType.WT;
        truckEntity.OilVolume = 5.0;
        var original = truckEntity.Clone();
        original.Status = TruckTicketStatus.Open;
        truckEntity.Status = TruckTicketStatus.Approved;
        var context = new BusinessContext<TruckTicketEntity>(truckEntity, original) { Operation = Operation.Update };
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //Assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.TruckTicket_VolumeOilFixedBothOutOfRange));
    }

    [TestMethod]
    public async Task Task_ShouldRunWithValidation_WhenServiceType_OilFixed_MinGiven()
    {
        //arrange
        var scope = new DefaultScope();
        var truckEntity = GenFu.GenFu.New<TruckTicketEntity>();
        var serviceTypeEntity = GenFu.GenFu.New<ServiceTypeEntity>();
        serviceTypeEntity.IncludesOil = true;
        serviceTypeEntity.OilMinValue = 10.0;
        serviceTypeEntity.OilMaxValue = null;
        serviceTypeEntity.OilThresholdType = SubstanceThresholdType.Fixed;

        serviceTypeEntity.IncludesSolids = false;
        serviceTypeEntity.IncludesWater = false;
        truckEntity.ServiceTypeId = serviceTypeEntity.Id;
        scope.ServiceManagerMock.Setup(x => x.GetById(It.IsAny<Guid?>(), false))
             .ReturnsAsync(serviceTypeEntity);

        truckEntity.TruckTicketType = TruckTicketType.WT;
        truckEntity.OilVolume = 5.0;
        var original = truckEntity.Clone();
        original.Status = TruckTicketStatus.Open;
        truckEntity.Status = TruckTicketStatus.Approved;
        var context = new BusinessContext<TruckTicketEntity>(truckEntity, original) { Operation = Operation.Update };
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //Assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.TruckTicket_VolumeOilFixedLessThanMin));
    }

    [TestMethod]
    public async Task Task_ShouldRunWithValidation_WhenServiceType_OilFixed_MaxGiven()
    {
        //arrange
        var scope = new DefaultScope();
        var truckEntity = GenFu.GenFu.New<TruckTicketEntity>();
        var serviceTypeEntity = GenFu.GenFu.New<ServiceTypeEntity>();
        serviceTypeEntity.IncludesOil = true;
        serviceTypeEntity.OilMinValue = null;
        serviceTypeEntity.OilMaxValue = 40.0;
        serviceTypeEntity.OilThresholdType = SubstanceThresholdType.Fixed;

        serviceTypeEntity.IncludesSolids = false;
        serviceTypeEntity.IncludesWater = false;
        truckEntity.ServiceTypeId = serviceTypeEntity.Id;
        scope.ServiceManagerMock.Setup(x => x.GetById(It.IsAny<Guid?>(), false))
             .ReturnsAsync(serviceTypeEntity);

        truckEntity.TruckTicketType = TruckTicketType.WT;
        truckEntity.OilVolume = 50.0;
        var original = truckEntity.Clone();
        original.Status = TruckTicketStatus.Open;
        truckEntity.Status = TruckTicketStatus.Approved;
        var context = new BusinessContext<TruckTicketEntity>(truckEntity, original) { Operation = Operation.Update };
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //Assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.TruckTicket_VolumeOilFixedGreaterThanMax));
    }

    //Oil Percentage
    [TestMethod]
    public async Task Task_ShouldRunWithValidation_WhenServiceType_OilPercentage_MinMaxGiven()
    {
        //arrange
        var scope = new DefaultScope();
        var truckEntity = GenFu.GenFu.New<TruckTicketEntity>();
        var serviceTypeEntity = GenFu.GenFu.New<ServiceTypeEntity>();
        truckEntity.ServiceTypeId = serviceTypeEntity.Id;
        serviceTypeEntity.IncludesOil = true;
        serviceTypeEntity.OilMinValue = 10.0;
        serviceTypeEntity.OilMaxValue = 20.0;
        serviceTypeEntity.OilThresholdType = SubstanceThresholdType.Percentage;

        serviceTypeEntity.IncludesSolids = false;
        serviceTypeEntity.IncludesWater = false;

        scope.ServiceManagerMock.Setup(x => x.GetById(It.IsAny<Guid?>(), false))
             .ReturnsAsync(serviceTypeEntity);

        truckEntity.TruckTicketType = TruckTicketType.WT;
        truckEntity.OilVolumePercent = 5.0;
        var original = truckEntity.Clone();
        original.Status = TruckTicketStatus.Open;
        truckEntity.Status = TruckTicketStatus.Approved;
        var context = new BusinessContext<TruckTicketEntity>(truckEntity, original) { Operation = Operation.Update };
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //Assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.TruckTicket_VolumeOilBothOutOfRange));
    }

    [TestMethod]
    public async Task Task_ShouldRunWithValidation_WhenServiceType_OilPercentage_MinGiven()
    {
        //arrange
        var scope = new DefaultScope();
        var truckEntity = GenFu.GenFu.New<TruckTicketEntity>();
        var serviceTypeEntity = GenFu.GenFu.New<ServiceTypeEntity>();
        truckEntity.ServiceTypeId = serviceTypeEntity.Id;
        serviceTypeEntity.IncludesOil = true;
        serviceTypeEntity.OilMinValue = 10.0;
        serviceTypeEntity.OilMaxValue = null;
        serviceTypeEntity.OilThresholdType = SubstanceThresholdType.Percentage;

        serviceTypeEntity.IncludesSolids = false;
        serviceTypeEntity.IncludesWater = false;

        scope.ServiceManagerMock.Setup(x => x.GetById(It.IsAny<Guid?>(), false))
             .ReturnsAsync(serviceTypeEntity);

        truckEntity.TruckTicketType = TruckTicketType.WT;
        truckEntity.OilVolumePercent = 5.0;
        var original = truckEntity.Clone();
        original.Status = TruckTicketStatus.Open;
        truckEntity.Status = TruckTicketStatus.Approved;
        var context = new BusinessContext<TruckTicketEntity>(truckEntity, original) { Operation = Operation.Update };
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //Assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.TruckTicket_VolumeOilLessThanMin));
    }

    [TestMethod]
    public async Task Task_ShouldRunWithValidation_WhenServiceType_OilPercentage_MaxGiven()
    {
        //arrange
        var scope = new DefaultScope();
        var truckEntity = GenFu.GenFu.New<TruckTicketEntity>();
        var serviceTypeEntity = GenFu.GenFu.New<ServiceTypeEntity>();
        truckEntity.ServiceTypeId = serviceTypeEntity.Id;
        serviceTypeEntity.IncludesOil = true;
        serviceTypeEntity.OilMinValue = null;
        serviceTypeEntity.OilMaxValue = 40.0;
        serviceTypeEntity.OilThresholdType = SubstanceThresholdType.Percentage;

        serviceTypeEntity.IncludesSolids = false;
        serviceTypeEntity.IncludesWater = false;

        scope.ServiceManagerMock.Setup(x => x.GetById(It.IsAny<Guid?>(), false))
             .ReturnsAsync(serviceTypeEntity);

        truckEntity.TruckTicketType = TruckTicketType.WT;
        truckEntity.OilVolumePercent = 50.0;
        var original = truckEntity.Clone();
        original.Status = TruckTicketStatus.Open;
        truckEntity.Status = TruckTicketStatus.Approved;
        var context = new BusinessContext<TruckTicketEntity>(truckEntity, original) { Operation = Operation.Update };
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //Assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.TruckTicket_VolumeOilGreaterThanMax));
    }

    //Water Fixed
    [TestMethod]
    public async Task Task_ShouldRunWithValidation_WhenServiceType_WaterFixed_MinMaxGiven()
    {
        //arrange
        var scope = new DefaultScope();
        var truckEntity = GenFu.GenFu.New<TruckTicketEntity>();
        var serviceTypeEntity = GenFu.GenFu.New<ServiceTypeEntity>();
        truckEntity.ServiceTypeId = serviceTypeEntity.Id;
        serviceTypeEntity.IncludesOil = false;
        serviceTypeEntity.IncludesSolids = false;

        serviceTypeEntity.IncludesWater = true;
        serviceTypeEntity.WaterMinValue = 10.0;
        serviceTypeEntity.WaterMaxValue = 20.0;
        serviceTypeEntity.WaterThresholdType = SubstanceThresholdType.Fixed;

        scope.ServiceManagerMock.Setup(x => x.GetById(It.IsAny<Guid?>(), false))
             .ReturnsAsync(serviceTypeEntity);

        truckEntity.TruckTicketType = TruckTicketType.WT;
        truckEntity.WaterVolume = 5.0;
        var original = truckEntity.Clone();
        original.Status = TruckTicketStatus.Open;
        truckEntity.Status = TruckTicketStatus.Approved;
        var context = new BusinessContext<TruckTicketEntity>(truckEntity, original) { Operation = Operation.Update };
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //Assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.TruckTicket_VolumeWaterFixedBothOutOfRange));
    }

    [TestMethod]
    public async Task Task_ShouldRunWithValidation_WhenServiceType_WaterFixed_MinGiven()
    {
        //arrange
        var scope = new DefaultScope();
        var truckEntity = GenFu.GenFu.New<TruckTicketEntity>();
        var serviceTypeEntity = GenFu.GenFu.New<ServiceTypeEntity>();
        truckEntity.ServiceTypeId = serviceTypeEntity.Id;
        serviceTypeEntity.IncludesOil = false;
        serviceTypeEntity.IncludesSolids = false;

        serviceTypeEntity.IncludesWater = true;
        serviceTypeEntity.WaterMinValue = 10.0;
        serviceTypeEntity.WaterMaxValue = null;
        serviceTypeEntity.WaterThresholdType = SubstanceThresholdType.Fixed;
        //scope.SetServiceManagerMock(serviceTypeEntity);
        scope.ServiceManagerMock.Setup(x => x.GetById(It.IsAny<Guid?>(), false))
             .ReturnsAsync(serviceTypeEntity);

        truckEntity.TruckTicketType = TruckTicketType.WT;
        truckEntity.WaterVolume = 5.0;
        var original = truckEntity.Clone();
        original.Status = TruckTicketStatus.Open;
        truckEntity.Status = TruckTicketStatus.Approved;
        var context = new BusinessContext<TruckTicketEntity>(truckEntity, original) { Operation = Operation.Update };
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //Assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.TruckTicket_VolumeWaterFixedLessThanMin));
    }

    [TestMethod]
    public async Task Task_ShouldRunWithValidation_WhenServiceType_WaterFixed_MaxGiven()
    {
        //arrange
        var scope = new DefaultScope();
        var truckEntity = GenFu.GenFu.New<TruckTicketEntity>();
        var serviceTypeEntity = GenFu.GenFu.New<ServiceTypeEntity>();
        truckEntity.ServiceTypeId = serviceTypeEntity.Id;
        serviceTypeEntity.IncludesOil = false;
        serviceTypeEntity.IncludesSolids = false;

        serviceTypeEntity.IncludesWater = true;
        serviceTypeEntity.WaterMinValue = null;
        serviceTypeEntity.WaterMaxValue = 40.0;
        serviceTypeEntity.WaterThresholdType = SubstanceThresholdType.Fixed;
        //scope.SetServiceManagerMock(serviceTypeEntity);
        scope.ServiceManagerMock.Setup(x => x.GetById(It.IsAny<Guid?>(), false))
             .ReturnsAsync(serviceTypeEntity);

        truckEntity.TruckTicketType = TruckTicketType.WT;
        truckEntity.WaterVolume = 50.0;
        var original = truckEntity.Clone();
        original.Status = TruckTicketStatus.Open;
        truckEntity.Status = TruckTicketStatus.Approved;
        var context = new BusinessContext<TruckTicketEntity>(truckEntity, original) { Operation = Operation.Update };
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //Assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.TruckTicket_VolumeWaterFixedGreaterThanMax));
    }

    //Water Percentage
    [TestMethod]
    public async Task Task_ShouldRunWithValidation_WhenServiceType_WaterPercentage_MinMaxGiven()
    {
        //arrange
        var scope = new DefaultScope();
        var truckEntity = GenFu.GenFu.New<TruckTicketEntity>();
        var serviceTypeEntity = GenFu.GenFu.New<ServiceTypeEntity>();
        truckEntity.ServiceTypeId = serviceTypeEntity.Id;
        serviceTypeEntity.IncludesOil = false;
        serviceTypeEntity.IncludesSolids = false;

        serviceTypeEntity.IncludesWater = true;
        serviceTypeEntity.WaterMinValue = 10.0;
        serviceTypeEntity.WaterMaxValue = 20.0;
        serviceTypeEntity.WaterThresholdType = SubstanceThresholdType.Percentage;

        scope.ServiceManagerMock.Setup(x => x.GetById(It.IsAny<Guid?>(), false))
             .ReturnsAsync(serviceTypeEntity);

        truckEntity.TruckTicketType = TruckTicketType.WT;
        truckEntity.WaterVolumePercent = 5.0;
        var original = truckEntity.Clone();
        original.Status = TruckTicketStatus.Open;
        truckEntity.Status = TruckTicketStatus.Approved;
        var context = new BusinessContext<TruckTicketEntity>(truckEntity, original) { Operation = Operation.Update };
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //Assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.TruckTicket_VolumeWaterBothOutOfRange));
    }

    [TestMethod]
    public async Task Task_ShouldRunWithValidation_WhenServiceType_WaterPercentage_MinGiven()
    {
        //arrange
        var scope = new DefaultScope();
        var truckEntity = GenFu.GenFu.New<TruckTicketEntity>();
        var serviceTypeEntity = GenFu.GenFu.New<ServiceTypeEntity>();
        truckEntity.ServiceTypeId = serviceTypeEntity.Id;
        serviceTypeEntity.IncludesOil = false;
        serviceTypeEntity.IncludesSolids = false;

        serviceTypeEntity.IncludesWater = true;
        serviceTypeEntity.WaterMinValue = 10.0;
        serviceTypeEntity.WaterMaxValue = null;
        serviceTypeEntity.WaterThresholdType = SubstanceThresholdType.Percentage;

        scope.ServiceManagerMock.Setup(x => x.GetById(It.IsAny<Guid?>(), false))
             .ReturnsAsync(serviceTypeEntity);

        truckEntity.TruckTicketType = TruckTicketType.WT;
        truckEntity.WaterVolumePercent = 5.0;
        var original = truckEntity.Clone();
        original.Status = TruckTicketStatus.Open;
        truckEntity.Status = TruckTicketStatus.Approved;
        var context = new BusinessContext<TruckTicketEntity>(truckEntity, original) { Operation = Operation.Update };
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //Assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.TruckTicket_VolumeWaterLessThanMin));
    }

    [TestMethod]
    public async Task Task_ShouldRunWithValidation_WhenServiceType_WaterPercentage_MaxGiven()
    {
        //arrange
        var scope = new DefaultScope();
        var truckEntity = GenFu.GenFu.New<TruckTicketEntity>();
        var serviceTypeEntity = GenFu.GenFu.New<ServiceTypeEntity>();
        truckEntity.ServiceTypeId = serviceTypeEntity.Id;
        serviceTypeEntity.IncludesOil = false;
        serviceTypeEntity.IncludesSolids = false;

        serviceTypeEntity.IncludesWater = true;
        serviceTypeEntity.WaterMinValue = null;
        serviceTypeEntity.WaterMaxValue = 40.0;
        serviceTypeEntity.WaterThresholdType = SubstanceThresholdType.Percentage;

        scope.ServiceManagerMock.Setup(x => x.GetById(It.IsAny<Guid?>(), false))
             .ReturnsAsync(serviceTypeEntity);

        truckEntity.TruckTicketType = TruckTicketType.WT;
        truckEntity.WaterVolumePercent = 50.0;
        var original = truckEntity.Clone();
        original.Status = TruckTicketStatus.Open;
        truckEntity.Status = TruckTicketStatus.Approved;
        var context = new BusinessContext<TruckTicketEntity>(truckEntity, original) { Operation = Operation.Update };
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //Assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.TruckTicket_VolumeWaterGreaterThanMax));
    }

    //Solids Fixed

    [TestMethod]
    public async Task Task_ShouldRunWithValidation_WhenServiceType_SolidsFixed_MinMaxGiven()
    {
        //arrange
        var scope = new DefaultScope();
        var truckEntity = GenFu.GenFu.New<TruckTicketEntity>();
        var serviceTypeEntity = GenFu.GenFu.New<ServiceTypeEntity>();
        truckEntity.ServiceTypeId = serviceTypeEntity.Id;
        serviceTypeEntity.IncludesOil = false;

        serviceTypeEntity.IncludesSolids = true;
        serviceTypeEntity.SolidMinValue = 10.0;
        serviceTypeEntity.SolidMaxValue = 20.0;
        serviceTypeEntity.SolidThresholdType = SubstanceThresholdType.Fixed;

        serviceTypeEntity.IncludesWater = false;

        scope.ServiceManagerMock.Setup(x => x.GetById(It.IsAny<Guid?>(), false))
             .ReturnsAsync(serviceTypeEntity);

        truckEntity.TruckTicketType = TruckTicketType.WT;
        truckEntity.SolidVolume = 5.0;
        var original = truckEntity.Clone();
        original.Status = TruckTicketStatus.Open;
        truckEntity.Status = TruckTicketStatus.Approved;
        var context = new BusinessContext<TruckTicketEntity>(truckEntity, original) { Operation = Operation.Update };
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //Assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.TruckTicket_VolumeSolidFixedBothOutOfRange));
    }

    [TestMethod]
    public async Task Task_ShouldRunWithValidation_WhenServiceType_SolidsFixed_MinGiven()
    {
        //arrange
        var scope = new DefaultScope();
        var truckEntity = GenFu.GenFu.New<TruckTicketEntity>();
        var serviceTypeEntity = GenFu.GenFu.New<ServiceTypeEntity>();
        truckEntity.ServiceTypeId = serviceTypeEntity.Id;
        serviceTypeEntity.IncludesOil = false;

        serviceTypeEntity.IncludesSolids = true;
        serviceTypeEntity.SolidMinValue = 10.0;
        serviceTypeEntity.SolidMaxValue = null;
        serviceTypeEntity.SolidThresholdType = SubstanceThresholdType.Fixed;

        serviceTypeEntity.IncludesWater = false;

        scope.ServiceManagerMock.Setup(x => x.GetById(It.IsAny<Guid?>(), false))
             .ReturnsAsync(serviceTypeEntity);

        truckEntity.TruckTicketType = TruckTicketType.WT;
        truckEntity.SolidVolume = 5.0;
        var original = truckEntity.Clone();
        original.Status = TruckTicketStatus.Open;
        truckEntity.Status = TruckTicketStatus.Approved;
        var context = new BusinessContext<TruckTicketEntity>(truckEntity, original) { Operation = Operation.Update };
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //Assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.TruckTicket_VolumeSolidFixedLessThanMin));
    }

    [TestMethod]
    public async Task Task_ShouldRunWithValidation_WhenServiceType_SolidsFixed_MaxGiven()
    {
        //arrange
        var scope = new DefaultScope();
        var truckEntity = GenFu.GenFu.New<TruckTicketEntity>();
        var serviceTypeEntity = GenFu.GenFu.New<ServiceTypeEntity>();
        truckEntity.ServiceTypeId = serviceTypeEntity.Id;
        serviceTypeEntity.IncludesOil = false;

        serviceTypeEntity.IncludesSolids = true;
        serviceTypeEntity.SolidMinValue = null;
        serviceTypeEntity.SolidMaxValue = 40.0;
        serviceTypeEntity.SolidThresholdType = SubstanceThresholdType.Fixed;

        serviceTypeEntity.IncludesWater = false;

        scope.ServiceManagerMock.Setup(x => x.GetById(It.IsAny<Guid?>(), false))
             .ReturnsAsync(serviceTypeEntity);

        truckEntity.TruckTicketType = TruckTicketType.WT;
        truckEntity.SolidVolume = 50.0;
        var original = truckEntity.Clone();
        original.Status = TruckTicketStatus.Open;
        truckEntity.Status = TruckTicketStatus.Approved;
        var context = new BusinessContext<TruckTicketEntity>(truckEntity, original) { Operation = Operation.Update };
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //Assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.TruckTicket_VolumeSolidFixedGreaterThanMax));
    }

    //Solid Percentage
    [TestMethod]
    public async Task Task_ShouldRunWithValidation_WhenServiceType_SolidsPercentage_MinMaxGiven()
    {
        //arrange
        var scope = new DefaultScope();
        var truckEntity = GenFu.GenFu.New<TruckTicketEntity>();
        var serviceTypeEntity = GenFu.GenFu.New<ServiceTypeEntity>();
        truckEntity.ServiceTypeId = serviceTypeEntity.Id;
        serviceTypeEntity.IncludesOil = false;

        serviceTypeEntity.IncludesSolids = true;
        serviceTypeEntity.SolidMinValue = 10.0;
        serviceTypeEntity.SolidMaxValue = 20.0;
        serviceTypeEntity.SolidThresholdType = SubstanceThresholdType.Percentage;

        serviceTypeEntity.IncludesWater = false;

        scope.ServiceManagerMock.Setup(x => x.GetById(It.IsAny<Guid?>(), false))
             .ReturnsAsync(serviceTypeEntity);

        truckEntity.TruckTicketType = TruckTicketType.WT;
        truckEntity.SolidVolumePercent = 5.0;
        var original = truckEntity.Clone();
        original.Status = TruckTicketStatus.Open;
        truckEntity.Status = TruckTicketStatus.Approved;
        var context = new BusinessContext<TruckTicketEntity>(truckEntity, original) { Operation = Operation.Update };
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //Assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.TruckTicket_VolumeSolidBothOutOfRange));
    }

    [TestMethod]
    public async Task Task_ShouldRunWithValidation_WhenServiceType_SolidsPercentage_MinGiven()
    {
        //arrange
        var scope = new DefaultScope();
        var truckEntity = GenFu.GenFu.New<TruckTicketEntity>();
        var serviceTypeEntity = GenFu.GenFu.New<ServiceTypeEntity>();
        truckEntity.ServiceTypeId = serviceTypeEntity.Id;
        serviceTypeEntity.IncludesOil = false;

        serviceTypeEntity.IncludesSolids = true;
        serviceTypeEntity.SolidMinValue = 10.0;
        serviceTypeEntity.SolidMaxValue = null;
        serviceTypeEntity.SolidThresholdType = SubstanceThresholdType.Percentage;

        serviceTypeEntity.IncludesWater = false;

        scope.ServiceManagerMock.Setup(x => x.GetById(It.IsAny<Guid?>(), false))
             .ReturnsAsync(serviceTypeEntity);

        truckEntity.TruckTicketType = TruckTicketType.WT;
        truckEntity.SolidVolumePercent = 5.0;
        var original = truckEntity.Clone();
        original.Status = TruckTicketStatus.Open;
        truckEntity.Status = TruckTicketStatus.Approved;
        var context = new BusinessContext<TruckTicketEntity>(truckEntity, original) { Operation = Operation.Update };
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //Assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.TruckTicket_VolumeSolidLessThanMin));
    }

    [TestMethod]
    public async Task Task_ShouldRunWithValidation_WhenServiceType_SolidsPercentage_MaxGiven()
    {
        //arrange
        var scope = new DefaultScope();
        var truckEntity = GenFu.GenFu.New<TruckTicketEntity>();
        var serviceTypeEntity = GenFu.GenFu.New<ServiceTypeEntity>();
        truckEntity.ServiceTypeId = serviceTypeEntity.Id;
        serviceTypeEntity.IncludesOil = false;

        serviceTypeEntity.IncludesSolids = true;
        serviceTypeEntity.SolidMinValue = null;
        serviceTypeEntity.SolidMaxValue = 40.0;
        serviceTypeEntity.SolidThresholdType = SubstanceThresholdType.Percentage;

        serviceTypeEntity.IncludesWater = false;

        scope.ServiceManagerMock.Setup(x => x.GetById(It.IsAny<Guid?>(), false))
             .ReturnsAsync(serviceTypeEntity);

        truckEntity.TruckTicketType = TruckTicketType.WT;
        truckEntity.SolidVolumePercent = 50.0;
        var original = truckEntity.Clone();
        original.Status = TruckTicketStatus.Open;
        truckEntity.Status = TruckTicketStatus.Approved;
        var context = new BusinessContext<TruckTicketEntity>(truckEntity, original) { Operation = Operation.Update };
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //Assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Where(errorCode => errorCode is not null)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.TruckTicket_VolumeSolidGreaterThanMax));
    }

    [TestMethod]
    public async Task Task_ShouldRunWithNoValidation_WhenServiceType_Fixed_AllCutMInMaxGiven()
    {
        //arrange
        var scope = new DefaultScope();
        var truckEntity = GenFu.GenFu.New<TruckTicketEntity>();
        var serviceTypeEntity = GenFu.GenFu.New<ServiceTypeEntity>();
        truckEntity.ServiceTypeId = serviceTypeEntity.Id;
        serviceTypeEntity.IncludesOil = true;
        serviceTypeEntity.OilMinValue = 10.0;
        serviceTypeEntity.OilMaxValue = 40.0;
        serviceTypeEntity.OilThresholdType = SubstanceThresholdType.Fixed;

        serviceTypeEntity.IncludesSolids = true;
        serviceTypeEntity.SolidMinValue = 10.0;
        serviceTypeEntity.SolidMaxValue = 40.0;
        serviceTypeEntity.SolidThresholdType = SubstanceThresholdType.Fixed;

        serviceTypeEntity.IncludesWater = true;
        serviceTypeEntity.WaterMinValue = 10.0;
        serviceTypeEntity.WaterMaxValue = 40.0;
        serviceTypeEntity.WaterThresholdType = SubstanceThresholdType.Fixed;

        scope.ServiceManagerMock.Setup(x => x.GetById(It.IsAny<Guid?>(), false))
             .ReturnsAsync(serviceTypeEntity);

        truckEntity.TruckTicketType = TruckTicketType.WT;
        truckEntity.OilVolume = 30.0;
        truckEntity.WaterVolume = 30.0;
        truckEntity.SolidVolume = 40.0;
        var original = truckEntity.Clone();
        original.Status = TruckTicketStatus.Open;
        var context = new BusinessContext<TruckTicketEntity>(truckEntity, original) { Operation = Operation.Update };
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //Assert
        Assert.AreEqual(0, validationResults.Count);
    }

    [TestMethod]
    public async Task Task_ShouldRunWithNoValidation_WhenServiceTypeIsNULL_AllCutMInMaxGiven()
    {
        //arrange
        var scope = new DefaultScope();
        var truckEntity = GenFu.GenFu.New<TruckTicketEntity>();
        truckEntity.ServiceTypeId = default;
        var serviceTypeEntity = GenFu.GenFu.New<ServiceTypeEntity>();
        serviceTypeEntity.IncludesOil = true;
        serviceTypeEntity.OilMinValue = 10.0;
        serviceTypeEntity.OilMaxValue = 40.0;
        serviceTypeEntity.OilThresholdType = SubstanceThresholdType.Fixed;

        serviceTypeEntity.IncludesSolids = true;
        serviceTypeEntity.SolidMinValue = 10.0;
        serviceTypeEntity.SolidMaxValue = 40.0;
        serviceTypeEntity.SolidThresholdType = SubstanceThresholdType.Fixed;

        serviceTypeEntity.IncludesWater = true;
        serviceTypeEntity.WaterMinValue = 10.0;
        serviceTypeEntity.WaterMaxValue = 40.0;
        serviceTypeEntity.WaterThresholdType = SubstanceThresholdType.Fixed;

        scope.ServiceManagerMock.Setup(x => x.GetById(It.IsAny<Guid?>(), false))
             .ReturnsAsync(serviceTypeEntity);

        truckEntity.TruckTicketType = TruckTicketType.WT;
        truckEntity.OilVolume = 5.0;
        truckEntity.WaterVolume = 50.0;
        truckEntity.SolidVolume = 45.0;
        var original = truckEntity.Clone();
        original.Status = TruckTicketStatus.Open;
        var context = new BusinessContext<TruckTicketEntity>(truckEntity, original) { Operation = Operation.Update };
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //Assert
        Assert.AreEqual(0, validationResults.Count);
    }
}

public class DefaultScope : TestScope<TruckTicketVolumeRangeValidationRule>
{
    public DefaultScope()
    {
        InstanceUnderTest = new(ServiceManagerMock.Object);
    }

    public Mock<IManager<Guid, ServiceTypeEntity>> ServiceManagerMock { get; } = new();

    public void SetServiceManagerMock(ServiceTypeEntity serviceTypeEntity)
    {
        ServiceManagerMock.Setup(serviceType => serviceType.GetById(It.IsAny<Guid>(), It.IsAny<bool>()))
                          .ReturnsAsync(serviceTypeEntity);
    }
}
