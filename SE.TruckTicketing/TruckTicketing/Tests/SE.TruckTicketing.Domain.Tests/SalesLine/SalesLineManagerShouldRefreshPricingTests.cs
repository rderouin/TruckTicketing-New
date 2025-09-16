using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.AdditionalServicesConfiguration;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.Shared.Domain.Entities.MaterialApproval;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.ServiceType;
using SE.Shared.Domain.Entities.SourceLocation;
using SE.Shared.Domain.PricingRules;
using SE.Shared.Domain.Product;
using SE.Shared.Domain.Tests.TestUtilities;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.FacilityService;
using SE.TruckTicketing.Domain.Entities.SalesLine;
using SE.TruckTicketing.Domain.Entities.TruckTicket;

using Trident.Contracts;
using Trident.Data.Contracts;
using Trident.IoC;
using Trident.Logging;

namespace SE.TruckTicketing.Domain.Tests.SalesLine;

[TestClass]
public class SalesLineManagerShouldRefreshPricingTests
{
    private readonly Mock<ILog> _loggerMock;
    private readonly Mock<IProvider<Guid, SalesLineEntity>> _salesLineEntityProviderMock;
    private readonly Mock<IManager<Guid, ProductEntity>> _productManagerMock;
    private readonly Mock<IPricingRuleManager> _pricingRuleManagerMock;
    private readonly Mock<IProvider<Guid, SourceLocationEntity>> _sourceLocationProviderMock;
    private readonly Mock<IProvider<Guid, FacilityEntity>> _facilityProviderMock;
    private readonly Mock<IManager<Guid, FacilityServiceSubstanceIndexEntity>> _facilityServiceSubstanceManagerMock;
    private readonly Mock<IProvider<Guid, AccountEntity>> _accountProviderMock;
    private readonly Mock<IManager<Guid, ServiceTypeEntity>> _serviceTypeManagerManagerMock;
    private readonly Mock<IProvider<Guid, MaterialApprovalEntity>> _materialApprovalProviderMock;
    private readonly Mock<ISalesLinesPublisher> _salesLinesPublisherMock;
    private readonly Mock<IProvider<Guid, AdditionalServicesConfigurationEntity>> _additionalServiceConfigProviderMock;
    private readonly Mock<IMatchPredicateRankManager> _predicateRankManagerMock;
    private readonly Mock<IProvider<Guid, BillingConfigurationEntity>> _billingConfigurationProviderMock;
    private readonly Mock<IIoCServiceLocator> _serviceLocatorMock;
    private readonly Mock<IProvider<Guid, LoadConfirmationEntity>> _loadConfirmationProviderMock;

    private readonly SalesLineManager _salesLineManager;

    public SalesLineManagerShouldRefreshPricingTests()
    {
        _loggerMock = new();
        _salesLineEntityProviderMock = new();
        _productManagerMock = new();
        _pricingRuleManagerMock = new();
        _sourceLocationProviderMock = new();
        _facilityProviderMock = new();
        _facilityServiceSubstanceManagerMock = new();
        _accountProviderMock = new();
        _serviceTypeManagerManagerMock = new();
        _materialApprovalProviderMock = new();
        _salesLinesPublisherMock = new();
        _additionalServiceConfigProviderMock = new();
        _predicateRankManagerMock = new();
        _billingConfigurationProviderMock = new();
        _serviceLocatorMock = new();
        _loadConfirmationProviderMock = new();

        _salesLineManager = new(_loggerMock.Object,
                                _salesLineEntityProviderMock.Object,
                                _productManagerMock.Object,
                                _pricingRuleManagerMock.Object,
                                _sourceLocationProviderMock.Object,
                                _facilityProviderMock.Object,
                                _facilityServiceSubstanceManagerMock.Object,
                                _accountProviderMock.Object,
                                _serviceTypeManagerManagerMock.Object,
                                _materialApprovalProviderMock.Object,
                                _salesLinesPublisherMock.Object,
                                _additionalServiceConfigProviderMock.Object,
                                _predicateRankManagerMock.Object,
                                _billingConfigurationProviderMock.Object,
                                _serviceLocatorMock.Object,
                                _loadConfirmationProviderMock.Object);

    }
    
    [TestMethod]
    public void BUG12508_ShouldRefreshPricing_SalesLineStatusPreview_CutTypeTotal_IsNotFieldTicket_OneAdditionalServiceOnTicket_ApplyZeroTotalVolumeFalse_ReturnsTrue()
    {
        //setup
        var billingConfigId = Guid.NewGuid();
        var billingConfig = new BillingConfigurationEntity() { Id = billingConfigId, FieldTicketsUploadEnabled = false, FieldTicketDeliveryMethod = FieldTicketDeliveryMethod.LoadConfirmationBatch };
        SaveToDatabase(billingConfig);

        var loadConfirmationId = Guid.NewGuid();
        var loadConfirmation = new LoadConfirmationEntity() { Id = loadConfirmationId, BillingConfigurationId = billingConfig.Id };
        SaveToDatabase(loadConfirmation);

        var serviceType = new ServiceTypeEntity { Id = Guid.NewGuid(), IncludesOil = true, IncludesSolids = true, IncludesWater = true, OilCreditMinVolume = 2 };
        SaveToDatabase(serviceType);

        SalesLineEntity salesLine = new SalesLineEntity
        {
            IsAdditionalService = true,
            Status = SalesLineStatus.SentToFo,
            LoadConfirmationId = loadConfirmation.Id,
            IsCutLine = true,
            Rate = 0,
            Quantity = 2.99,
            CutType = SalesLineCutType.Total,
            ServiceTypeId = serviceType.Id,
            ProductNumber = "90240",
            CustomerId = Guid.NewGuid(),
            FacilityId = Guid.NewGuid(),
            FacilitySiteId = "Test Site Id",
            CanPriceBeRefreshed = true
        };
        SaveToDatabase(salesLine);

        var additionalServicesConfig = new AdditionalServicesConfigurationEntity
        {
            CustomerId = salesLine.CustomerId,
            FacilityId = salesLine.FacilityId,
            SiteId = salesLine.FacilitySiteId,
            ApplyZeroTotalVolume = false
        };
        SaveToDatabase(additionalServicesConfig);

        //execute
        var result = _salesLineManager.ShouldRefreshPricing(GetPriceRefreshContext(salesLine));

        //assert
        AssertTaskResult(true, result, "Result should be True, YES refresh pricing.");
    }
    
    private PriceRefreshContext GetPriceRefreshContext(SalesLineEntity salesLineEntity)
    {
        return new()
        {
            CurrentSalesLine = salesLineEntity,
            AdditionalServicesConfigProvider = _additionalServiceConfigProviderMock.Object,
            BillingConfigurationProvider = _billingConfigurationProviderMock.Object,
            ServiceTypeManager = _serviceTypeManagerManagerMock.Object,
            LoadConfirmationProvider = _loadConfirmationProviderMock.Object
        };

    }

    [TestMethod]
    public void BUG12508_ShouldRefreshPricing_SalesLineStatusPreview_CutTypeTotal_IsFieldTicket_ProductBeginsWithNine_ApplyZeroTotalVolumeFalse_ReturnsFalse()
    {
        //setup
        var billingConfigId = Guid.NewGuid();
        var billingConfig = new BillingConfigurationEntity() { Id = billingConfigId, FieldTicketsUploadEnabled = true, FieldTicketDeliveryMethod = FieldTicketDeliveryMethod.LoadConfirmationBatch };
        SaveToDatabase(billingConfig);

        var loadConfirmationId = Guid.NewGuid();
        var loadConfirmation = new LoadConfirmationEntity() { Id = loadConfirmationId, BillingConfigurationId = billingConfig.Id };
        SaveToDatabase(loadConfirmation);

        var serviceType = new ServiceTypeEntity { Id = Guid.NewGuid(), IncludesOil = true, IncludesSolids = true, IncludesWater = true, OilCreditMinVolume = 2 };
        SaveToDatabase(serviceType);

        SalesLineEntity salesLine = new SalesLineEntity
        {
            IsAdditionalService = true,
            Status = SalesLineStatus.SentToFo,
            LoadConfirmationId = loadConfirmation.Id,
            IsCutLine = true,
            Rate = 0,
            Quantity = 1.99,
            CutType = SalesLineCutType.Total,
            ServiceTypeId = serviceType.Id,
            ProductNumber = "902030",
            CustomerId = Guid.NewGuid(),
            FacilityId = Guid.NewGuid(),
            FacilitySiteId = "Test Site Id",
            CanPriceBeRefreshed = true
        };
        SaveToDatabase(salesLine);

        var additionalServicesConfig = new AdditionalServicesConfigurationEntity
        {
            CustomerId = salesLine.CustomerId,
            FacilityId = salesLine.FacilityId,
            SiteId = salesLine.FacilitySiteId,
            ApplyZeroTotalVolume = false
        };
        SaveToDatabase(additionalServicesConfig);

        //execute
        var result = _salesLineManager.ShouldRefreshPricing(GetPriceRefreshContext(salesLine));

        //assert
        AssertTaskResult(false, result, "Result should be False, DO NOT refresh pricing.");
    }

    [TestMethod]
    public void BUG12508_ShouldRefreshPricing_SalesLineStatusPreview_CutTypeTotal_IsFieldTicket_ProductBeginsWithSeven_ApplyZeroTotalVolumeFalse_ReturnsTrue()
    {
        //setup
        var billingConfigId = Guid.NewGuid();
        var billingConfig = new BillingConfigurationEntity() { Id = billingConfigId, FieldTicketsUploadEnabled = true, FieldTicketDeliveryMethod = FieldTicketDeliveryMethod.LoadConfirmationBatch };
        SaveToDatabase(billingConfig);

        var loadConfirmationId = Guid.NewGuid();
        var loadConfirmation = new LoadConfirmationEntity() { Id = loadConfirmationId, BillingConfigurationId = billingConfig.Id };
        SaveToDatabase(loadConfirmation);

        var serviceType = new ServiceTypeEntity { Id = Guid.NewGuid(), IncludesOil = true, IncludesSolids = true, IncludesWater = true, OilCreditMinVolume = 2 };
        SaveToDatabase(serviceType);

        SalesLineEntity salesLine = new SalesLineEntity
        {
            IsAdditionalService = true,
            Status = SalesLineStatus.SentToFo,
            LoadConfirmationId = loadConfirmation.Id,
            IsCutLine = true,
            Rate = 0,
            Quantity = 1.99,
            CutType = SalesLineCutType.Total,
            ServiceTypeId = serviceType.Id,
            ProductNumber = "702030",
            CustomerId = Guid.NewGuid(),
            FacilityId = Guid.NewGuid(),
            FacilitySiteId = "Test Site Id",
            CanPriceBeRefreshed = true
        };
        SaveToDatabase(salesLine);

        var additionalServicesConfig = new AdditionalServicesConfigurationEntity
        {
            CustomerId = salesLine.CustomerId,
            FacilityId = salesLine.FacilityId,
            SiteId = salesLine.FacilitySiteId,
            ApplyZeroTotalVolume = false
        };
        SaveToDatabase(additionalServicesConfig);

        //execute
        var result = _salesLineManager.ShouldRefreshPricing(GetPriceRefreshContext(salesLine));

        //assert
        AssertTaskResult(true, result, "Result should be True, YES refresh pricing.");
    }

    [TestMethod]
    public void BUG12508_ShouldRefreshPricing_SalesLineStatusPreview_ProductStatsWithNine_ReturnsFalse()
    {
        //setup
        var billingConfigId = Guid.NewGuid();
        var billingConfig = new BillingConfigurationEntity() { Id = billingConfigId, FieldTicketsUploadEnabled = true, FieldTicketDeliveryMethod = FieldTicketDeliveryMethod.LoadConfirmationBatch };
        SaveToDatabase(billingConfig);

        var loadConfirmationId = Guid.NewGuid();
        var loadConfirmation = new LoadConfirmationEntity() { Id = loadConfirmationId, BillingConfigurationId = billingConfig.Id };
        SaveToDatabase(loadConfirmation);

        var serviceType = new ServiceTypeEntity { Id = Guid.NewGuid(), IncludesOil = true, IncludesSolids = true, IncludesWater = true, OilCreditMinVolume = 2 };
        SaveToDatabase(serviceType);

        SalesLineEntity salesLine = new SalesLineEntity
        {
            Status = SalesLineStatus.SentToFo,
            LoadConfirmationId = loadConfirmation.Id,
            IsCutLine = true,
            Rate = 0,
            Quantity = 1.99,
            CutType = SalesLineCutType.Oil,
            ServiceTypeId = serviceType.Id,
            ProductNumber = "90240"
        };
        SaveToDatabase(salesLine);

        //execute
        var result = _salesLineManager.ShouldRefreshPricing(GetPriceRefreshContext(salesLine));

        //assert
        AssertTaskResult(false, result, "Result should be True, DO NOT refresh pricing.");
    }

    [TestMethod]
    public void BUG12508_ShouldRefreshPricing_FieldTicket_ProductStartsWithSeven_ReturnsTrue()
    {
        //setup
        var billingConfigId = Guid.NewGuid();
        var billingConfig = new BillingConfigurationEntity() { Id = billingConfigId, FieldTicketsUploadEnabled = true, FieldTicketDeliveryMethod = FieldTicketDeliveryMethod.LoadConfirmationBatch };
        SaveToDatabase(billingConfig);

        var loadConfirmationId = Guid.NewGuid();
        var loadConfirmation = new LoadConfirmationEntity() { Id = loadConfirmationId, BillingConfigurationId = billingConfig.Id };
        SaveToDatabase(loadConfirmation);

        var serviceType = new ServiceTypeEntity { Id = Guid.NewGuid(), IncludesOil = true, IncludesSolids = true, IncludesWater = true, OilCreditMinVolume = 1 };
        SaveToDatabase(serviceType);

        SalesLineEntity salesLine = new SalesLineEntity
        {
            Status = SalesLineStatus.SentToFo,
            LoadConfirmationId = loadConfirmation.Id,
            IsCutLine = true,
            Rate = 0,
            Quantity = 1.99,
            CutType = SalesLineCutType.Oil,
            ServiceTypeId = serviceType.Id,
            ProductNumber = "706060"
        };
        SaveToDatabase(salesLine);

        //execute
        var result = _salesLineManager.ShouldRefreshPricing(GetPriceRefreshContext(salesLine));

        //assert
        AssertTaskResult(true, result, "Result should be True, YES refresh pricing.");
    }

    [TestMethod]
    public void BUG12508_ShouldRefreshPricing_SalesLineStatusSentToFo_Oil_PriceIsZero_QtyLessThanMin_RefreshPricingFalse()
    {
        //setup
        var billingConfigId = Guid.NewGuid();
        var billingConfig = new BillingConfigurationEntity() { Id = billingConfigId, FieldTicketsUploadEnabled = false, FieldTicketDeliveryMethod = FieldTicketDeliveryMethod.LoadConfirmationBatch };
        SaveToDatabase(billingConfig);

        var loadConfirmationId = Guid.NewGuid();
        var loadConfirmation = new LoadConfirmationEntity() { Id = loadConfirmationId, BillingConfigurationId = billingConfig.Id };
        SaveToDatabase(loadConfirmation);

        var serviceType = new ServiceTypeEntity { Id = Guid.NewGuid(), IncludesOil = true, IncludesSolids = true, IncludesWater = true, OilCreditMinVolume = 2 };
        SaveToDatabase(serviceType);

        SalesLineEntity salesLine = new SalesLineEntity
        {
            Status = SalesLineStatus.SentToFo,
            LoadConfirmationId = loadConfirmation.Id,
            IsCutLine = true,
            Rate = 0,
            Quantity = 1.99,
            CutType = SalesLineCutType.Oil,
            ServiceTypeId = serviceType.Id,
        };
        SaveToDatabase(salesLine);

        //execute
        var result = _salesLineManager.ShouldRefreshPricing(GetPriceRefreshContext(salesLine));

        //assert
        AssertTaskResult(false, result, "Result should be True, DO NOT refresh pricing.");
    }

    [TestMethod]
    public void BUG12508_ShouldRefreshPricing_SalesLineStatusSentToFo_Oil_PriceIsZero_QtyEqualToMin_Condition5_RefreshPricingTrue()
    {
        //setup
        var billingConfigId = Guid.NewGuid();
        var billingConfig = new BillingConfigurationEntity() { Id = billingConfigId, FieldTicketsUploadEnabled = false, FieldTicketDeliveryMethod = FieldTicketDeliveryMethod.LoadConfirmationBatch };
        SaveToDatabase(billingConfig);

        var loadConfirmationId = Guid.NewGuid();
        var loadConfirmation = new LoadConfirmationEntity() { Id = loadConfirmationId, BillingConfigurationId = billingConfig.Id };
        SaveToDatabase(loadConfirmation);

        var serviceType = new ServiceTypeEntity { Id = Guid.NewGuid(), IncludesOil = true, IncludesSolids = true, IncludesWater = true, OilCreditMinVolume = 2 };
        SaveToDatabase(serviceType);

        SalesLineEntity salesLine = new SalesLineEntity
        {
            Status = SalesLineStatus.SentToFo,
            LoadConfirmationId = loadConfirmation.Id,
            IsCutLine = true,
            Rate = 0,
            Quantity = 2,
            CutType = SalesLineCutType.Oil,
            ServiceTypeId = serviceType.Id,
            CanPriceBeRefreshed = true
        };
        SaveToDatabase(salesLine);

        //execute
        var result = _salesLineManager.ShouldRefreshPricing(GetPriceRefreshContext(salesLine));

        //assert
        AssertTrueTaskResult(result);
    }


    [TestMethod]
    public void BUG12508_ShouldRefreshPricing_SalesLineStatusSentToFo_Oil_PriceIsZero_QtyGreaterThanMin_RefreshPricingTrue()
    {
        //setup
        var billingConfigId = Guid.NewGuid();
        var billingConfig = new BillingConfigurationEntity() { Id = billingConfigId, FieldTicketsUploadEnabled = false, FieldTicketDeliveryMethod = FieldTicketDeliveryMethod.LoadConfirmationBatch };
        SaveToDatabase(billingConfig);

        var loadConfirmationId = Guid.NewGuid();
        var loadConfirmation = new LoadConfirmationEntity() { Id = loadConfirmationId, BillingConfigurationId = billingConfig.Id };
        SaveToDatabase(loadConfirmation);

        var serviceType = new ServiceTypeEntity { Id = Guid.NewGuid(), IncludesOil = true, IncludesSolids = true, IncludesWater = true, OilCreditMinVolume = 1 };
        SaveToDatabase(serviceType);

        SalesLineEntity salesLine = new SalesLineEntity
        {
            Status = SalesLineStatus.SentToFo,
            LoadConfirmationId = loadConfirmation.Id,
            IsCutLine = true,
            Rate = 0,
            Quantity = 2,
            CutType = SalesLineCutType.Oil,
            ServiceTypeId = serviceType.Id,
            CanPriceBeRefreshed = true
        };
        SaveToDatabase(salesLine);

        //execute
        var result = _salesLineManager.ShouldRefreshPricing(GetPriceRefreshContext(salesLine));

        //assert
        AssertTrueTaskResult(result);
    }

    [TestMethod]
    public void BUG12508_ShouldRefreshPricing_SalesLineStatusSentToFo_Oil_PriceIsNegativeTwo_AbsQtyLessThanMin_RefreshPricingFalse()
    {
        //setup
        var billingConfigId = Guid.NewGuid();
        var billingConfig = new BillingConfigurationEntity() { Id = billingConfigId, FieldTicketsUploadEnabled = false, FieldTicketDeliveryMethod = FieldTicketDeliveryMethod.LoadConfirmationBatch };
        SaveToDatabase(billingConfig);

        var loadConfirmationId = Guid.NewGuid();
        var loadConfirmation = new LoadConfirmationEntity() { Id = loadConfirmationId, BillingConfigurationId = billingConfig.Id };
        SaveToDatabase(loadConfirmation);

        var serviceType = new ServiceTypeEntity {Id = Guid.NewGuid(), IncludesOil = true, IncludesSolids = true, IncludesWater = true, OilCreditMinVolume = 2.1 };
        SaveToDatabase(serviceType);

        SalesLineEntity salesLine = new SalesLineEntity
        {
            Status = SalesLineStatus.SentToFo,
            LoadConfirmationId = loadConfirmation.Id,
            IsCutLine = true,
            Rate = 0,
            Quantity = -2,
            CutType = SalesLineCutType.Oil,
            ServiceTypeId = serviceType.Id,
        };
        SaveToDatabase(salesLine);

        //execute
        var result = _salesLineManager.ShouldRefreshPricing(GetPriceRefreshContext(salesLine));

        //assert
        AssertTaskResult(false, result, "Result should be False, DO NOT refresh pricing.");
    }

    [TestMethod]
    public void BUG12508_ShouldRefreshPricing_SalesLineStatusSentToFo_Water_PriceIsZero_QtyPctLessThanMin_RefreshPricingFalse()
    {
        //setup
        var billingConfigId = Guid.NewGuid();
        var billingConfig = new BillingConfigurationEntity() { Id = billingConfigId, FieldTicketsUploadEnabled = false, FieldTicketDeliveryMethod = FieldTicketDeliveryMethod.LoadConfirmationBatch };
        SaveToDatabase(billingConfig);

        var loadConfirmationId = Guid.NewGuid();
        var loadConfirmation = new LoadConfirmationEntity() { Id = loadConfirmationId, BillingConfigurationId = billingConfig.Id };
        SaveToDatabase(loadConfirmation);

        var serviceType = new ServiceTypeEntity { Id = Guid.NewGuid(), IncludesOil = true, IncludesSolids = true, IncludesWater = true, WaterMinPricingPercentage = 2 };
        SaveToDatabase(serviceType);

        SalesLineEntity salesLine = new SalesLineEntity
        {
            Status = SalesLineStatus.SentToFo,
            LoadConfirmationId = loadConfirmation.Id,
            IsCutLine = true,
            Rate = 0,
            QuantityPercent = 1.99,
            CutType = SalesLineCutType.Water,
            ServiceTypeId = serviceType.Id,
        };
        SaveToDatabase(salesLine);

        //execute
        var result = _salesLineManager.ShouldRefreshPricing(GetPriceRefreshContext(salesLine));

        //assert
        AssertTaskResult(false, result, "Result should be True, DO NOT refresh pricing.");
    }

    [TestMethod]
    public void BUG12508_ShouldRefreshPricing_SalesLineStatusSentToFo_Water_PriceIsZero_QtyPctEqualToMin_Condition5_RefreshPricingTrue()
    {
        //setup
        var billingConfigId = Guid.NewGuid();
        var billingConfig = new BillingConfigurationEntity() { Id = billingConfigId, FieldTicketsUploadEnabled = false, FieldTicketDeliveryMethod = FieldTicketDeliveryMethod.LoadConfirmationBatch };
        SaveToDatabase(billingConfig);

        var loadConfirmationId = Guid.NewGuid();
        var loadConfirmation = new LoadConfirmationEntity() { Id = loadConfirmationId, BillingConfigurationId = billingConfig.Id };
        SaveToDatabase(loadConfirmation);

        var serviceType = new ServiceTypeEntity {Id = Guid.NewGuid(), IncludesOil = true, IncludesSolids = true, IncludesWater = true, WaterMinPricingPercentage = 2 };
        SaveToDatabase(serviceType);

        SalesLineEntity salesLine = new SalesLineEntity
        {
            Status = SalesLineStatus.SentToFo,
            LoadConfirmationId = loadConfirmation.Id,
            IsCutLine = true,
            Rate = 0,
            QuantityPercent = 2,
            CutType = SalesLineCutType.Water,
            ServiceTypeId = serviceType.Id,
            CanPriceBeRefreshed = true
        };
        SaveToDatabase(salesLine);

        //execute
        var result = _salesLineManager.ShouldRefreshPricing(GetPriceRefreshContext(salesLine));

        //assert
        AssertTrueTaskResult(result);
    }

    [TestMethod]
    public void BUG12508_ShouldRefreshPricing_SalesLineStatusSentToFo_Water_PriceIsZero_QtyPctGreaterThanMin_RefreshPricingTrue()
    {
        //setup
        var billingConfigId = Guid.NewGuid();
        var billingConfig = new BillingConfigurationEntity() { Id = billingConfigId, FieldTicketsUploadEnabled = false, FieldTicketDeliveryMethod = FieldTicketDeliveryMethod.LoadConfirmationBatch };
        SaveToDatabase(billingConfig);

        var loadConfirmationId = Guid.NewGuid();
        var loadConfirmation = new LoadConfirmationEntity() { Id = loadConfirmationId, BillingConfigurationId = billingConfig.Id };
        SaveToDatabase(loadConfirmation);

        var serviceType = new ServiceTypeEntity {Id = Guid.NewGuid(), IncludesOil = true, IncludesSolids = true, IncludesWater = true, WaterMinPricingPercentage = 1 };
        SaveToDatabase(serviceType);

        SalesLineEntity salesLine = new SalesLineEntity
        {
            Status = SalesLineStatus.SentToFo,
            LoadConfirmationId = loadConfirmation.Id,
            IsCutLine = true,
            Rate = 0,
            QuantityPercent = 2,
            CutType = SalesLineCutType.Water,
            ServiceTypeId = serviceType.Id,
            CanPriceBeRefreshed = true
        };
        SaveToDatabase(salesLine);

        //execute
        var result = _salesLineManager.ShouldRefreshPricing(GetPriceRefreshContext(salesLine));

        //assert
        AssertTaskResult(true, result, "Result should be True, YES refresh pricing.");
    }

    [TestMethod]
    public void BUG12508_ShouldRefreshPricing_SalesLineStatusSentToFo_Solid_PriceIsZero_QtyPctLessThanMin_RefreshPricingFalse()
    {
        //setup
        var billingConfigId = Guid.NewGuid();
        var billingConfig = new BillingConfigurationEntity() { Id = billingConfigId, FieldTicketsUploadEnabled = false, FieldTicketDeliveryMethod = FieldTicketDeliveryMethod.LoadConfirmationBatch };
        SaveToDatabase(billingConfig);

        var loadConfirmationId = Guid.NewGuid();
        var loadConfirmation = new LoadConfirmationEntity() { Id = loadConfirmationId, BillingConfigurationId = billingConfig.Id };
        SaveToDatabase(loadConfirmation);

        var serviceType = new ServiceTypeEntity { Id = Guid.NewGuid(), IncludesOil = true, IncludesSolids = true, IncludesWater = true, SolidMinPricingPercentage = 2 };
        SaveToDatabase(serviceType);

        SalesLineEntity salesLine = new SalesLineEntity
        {
            Status = SalesLineStatus.SentToFo,
            LoadConfirmationId = loadConfirmation.Id,
            IsCutLine = true,
            Rate = 0,
            QuantityPercent = 1.99,
            CutType = SalesLineCutType.Solid,
            ServiceTypeId = serviceType.Id,
        };
        SaveToDatabase(salesLine);

        //execute
        var result = _salesLineManager.ShouldRefreshPricing(GetPriceRefreshContext(salesLine));

        //assert
        AssertTaskResult(false, result, "Result should be True, DO NOT refresh pricing.");
    }

    [TestMethod]
    public void BUG12508_ShouldRefreshPricing_SalesLineStatusSentToFo_Solid_PriceIsZero_QtyPctEqualToMin_Condition5_RefreshPricingTrue()
    {
        //setup
        var billingConfigId = Guid.NewGuid();
        var billingConfig = new BillingConfigurationEntity() { Id = billingConfigId, FieldTicketsUploadEnabled = false, FieldTicketDeliveryMethod = FieldTicketDeliveryMethod.LoadConfirmationBatch };
        SaveToDatabase(billingConfig);

        var loadConfirmationId = Guid.NewGuid();
        var loadConfirmation = new LoadConfirmationEntity() { Id = loadConfirmationId, BillingConfigurationId = billingConfig.Id };
        SaveToDatabase(loadConfirmation);

        var serviceType = new ServiceTypeEntity { Id = Guid.NewGuid(), IncludesOil = true, IncludesSolids = true, IncludesWater = true, SolidMinPricingPercentage = 2 };
        SaveToDatabase(serviceType);

        SalesLineEntity salesLine = new SalesLineEntity
        {
            Status = SalesLineStatus.SentToFo,
            LoadConfirmationId = loadConfirmation.Id,
            IsCutLine = true,
            Rate = 0,
            QuantityPercent = 2,
            CutType = SalesLineCutType.Solid,
            ServiceTypeId = serviceType.Id,
            CanPriceBeRefreshed = true
        };
        SaveToDatabase(salesLine);

        //execute
        var result = _salesLineManager.ShouldRefreshPricing(GetPriceRefreshContext(salesLine));

        //assert
        AssertTrueTaskResult(result);
    }

    [TestMethod]
    public void BUG12508_ShouldRefreshPricing_SalesLineStatusSentToFo_Solid_PriceIsZero_QtyPctGreaterThanMin_Condition5_RefreshPricingTrue()
    {
        //setup
        var billingConfigId = Guid.NewGuid();
        var billingConfig = new BillingConfigurationEntity() { Id = billingConfigId, FieldTicketsUploadEnabled = false, FieldTicketDeliveryMethod = FieldTicketDeliveryMethod.LoadConfirmationBatch };
        SaveToDatabase(billingConfig);

        var loadConfirmationId = Guid.NewGuid();
        var loadConfirmation = new LoadConfirmationEntity() { Id = loadConfirmationId, BillingConfigurationId = billingConfig.Id };
        SaveToDatabase(loadConfirmation);

        var serviceType = new ServiceTypeEntity { Id = Guid.NewGuid(), IncludesOil = true, IncludesSolids = true, IncludesWater = true, SolidMinPricingPercentage = 1 };
        SaveToDatabase(serviceType);

        SalesLineEntity salesLine = new SalesLineEntity
        {
            Status = SalesLineStatus.SentToFo,
            LoadConfirmationId = loadConfirmation.Id,
            IsCutLine = true,
            Rate = 0,
            QuantityPercent = 2,
            CutType = SalesLineCutType.Solid,
            ServiceTypeId = serviceType.Id,
            CanPriceBeRefreshed = true
        };
        SaveToDatabase(salesLine);

        //execute
        var result = _salesLineManager.ShouldRefreshPricing(GetPriceRefreshContext(salesLine));

        //assert
        AssertTaskResult(true, result, "Result should be True, YES refresh pricing.");
    }

    [TestMethod]
    public void BUG12508_ShouldRefreshPricing_SalesLineStatusSentToFo_Oil_PriceIsNotZero_ConditionFive_ReturnsFalse()
    {
        //setup
        var billingConfigId = Guid.NewGuid();
        var billingConfig = new BillingConfigurationEntity() { Id = billingConfigId, FieldTicketsUploadEnabled = false, FieldTicketDeliveryMethod = FieldTicketDeliveryMethod.LoadConfirmationBatch };
        SaveToDatabase(billingConfig);

        var loadConfirmationId = Guid.NewGuid();
        var loadConfirmation = new LoadConfirmationEntity() { Id = loadConfirmationId, BillingConfigurationId = billingConfig.Id };
        SaveToDatabase(loadConfirmation);

        var serviceType = new ServiceTypeEntity { Id = Guid.NewGuid(), IncludesOil = true, IncludesSolids = true, IncludesWater = true };
        SaveToDatabase(serviceType);

        SalesLineEntity salesLine = new SalesLineEntity
        {
            Status = SalesLineStatus.SentToFo,
            LoadConfirmationId = loadConfirmation.Id,
            IsCutLine = true,
            Rate = -0.1,
            CutType = SalesLineCutType.Oil,
            ServiceTypeId = serviceType.Id,
            CanPriceBeRefreshed = false
        };
        SaveToDatabase(salesLine);

        //execute
        var result = _salesLineManager.ShouldRefreshPricing(GetPriceRefreshContext(salesLine));

        //assert
        AssertTaskResultFalse(result);
    }

    private static void AssertTaskResultFalse(Task<bool> result)
    {
        AssertTaskResult(false, result, "Result should be False, DO NOT refresh pricing.");
    }

    [TestMethod]
    public void BUG12508_ShouldRefreshPricing_SalesLineStatusSentToFo_WaterMinPricingPercentageNull_PriceIsNotZero_Condition5_ReturnsTrue()
    {
        //setup
        var billingConfigId = Guid.NewGuid();
        var billingConfig = new BillingConfigurationEntity() { Id = billingConfigId, FieldTicketsUploadEnabled = false, FieldTicketDeliveryMethod = FieldTicketDeliveryMethod.LoadConfirmationBatch };
        SaveToDatabase(billingConfig);

        var loadConfirmationId = Guid.NewGuid();
        var loadConfirmation = new LoadConfirmationEntity() { Id = loadConfirmationId, BillingConfigurationId = billingConfig.Id };
        SaveToDatabase(loadConfirmation);

        var serviceType = new ServiceTypeEntity { Id = Guid.NewGuid(), IncludesOil = true, IncludesSolids = true, IncludesWater = true };
        SaveToDatabase(serviceType);

        SalesLineEntity salesLine = new SalesLineEntity
        {
            Status = SalesLineStatus.SentToFo,
            LoadConfirmationId = loadConfirmation.Id,
            IsCutLine = true,
            Rate = 1,
            CutType = SalesLineCutType.Water,
            ServiceTypeId = serviceType.Id,
            CanPriceBeRefreshed = true
        };
        SaveToDatabase(salesLine);

        //execute
        var result = _salesLineManager.ShouldRefreshPricing(GetPriceRefreshContext(salesLine));

        //assert
        AssertTrueTaskResult(result);
    }

    [TestMethod]
    public void BUG12508_ShouldRefreshPricing_SalesLineStatusSentToFo_SolidMinPricingPercentageNull_PriceIsNotZero_Condition5_ReturnsTrue()
    {
        //setup
        var billingConfigId = Guid.NewGuid();
        var billingConfig = new BillingConfigurationEntity() { Id = billingConfigId, FieldTicketsUploadEnabled = false, FieldTicketDeliveryMethod = FieldTicketDeliveryMethod.LoadConfirmationBatch };
        SaveToDatabase(billingConfig);

        var loadConfirmationId = Guid.NewGuid();
        var loadConfirmation = new LoadConfirmationEntity() { Id = loadConfirmationId, BillingConfigurationId = billingConfig.Id };
        SaveToDatabase(loadConfirmation);

        var serviceType = new ServiceTypeEntity { Id = Guid.NewGuid(), IncludesOil = true, IncludesSolids = true, IncludesWater = true };
        SaveToDatabase(serviceType);

        SalesLineEntity salesLine = new SalesLineEntity
        {
            Status = SalesLineStatus.SentToFo,
            LoadConfirmationId = loadConfirmation.Id,
            IsCutLine = true,
            Rate = 0.1,
            CutType = SalesLineCutType.Solid,
            ServiceTypeId = serviceType.Id,
            CanPriceBeRefreshed = true
        };
        SaveToDatabase(salesLine);

        //execute
        var result = _salesLineManager.ShouldRefreshPricing(GetPriceRefreshContext(salesLine));

        //assert
        AssertTaskResult(true, result, "Result should be True, YES refresh pricing.");
    }
    
    [TestMethod]
    public void BUG12508_ShouldRefreshPricing_SalesLineStatusApproved_BillingConfig_IsFieldTicket_LoadConfBatchDeliveryMethod_ReturnsFalse()
    {
        //setup
        var billingConfigId = Guid.NewGuid();
        var billingConfig = new BillingConfigurationEntity() { Id = billingConfigId, FieldTicketsUploadEnabled = true, FieldTicketDeliveryMethod = FieldTicketDeliveryMethod.LoadConfirmationBatch };
        SaveToDatabase(billingConfig);

        var loadConfirmationId = Guid.NewGuid();
        var loadConfirmation = new LoadConfirmationEntity() { Id = loadConfirmationId, BillingConfigurationId = billingConfig.Id };
        SaveToDatabase(loadConfirmation);

        SalesLineEntity salesLine = new SalesLineEntity { Status = SalesLineStatus.Approved, LoadConfirmationId = loadConfirmation.Id };
        SaveToDatabase(salesLine);

        //execute
        var result = _salesLineManager.ShouldRefreshPricing(GetPriceRefreshContext(salesLine));

        //assert
        AssertTaskResult(false, result, "Result should be False, DO NOT refresh pricing.");
    }

    [TestMethod]
    public void BUG12508_ShouldRefreshPricing_SalesLineStatusApproved_BillingConfig_IsFieldTicket_TicketByTicketDeliveryMethod_ReturnsFalse()
    {
        //setup
        var billingConfigId = Guid.NewGuid();
        var billingConfig = new BillingConfigurationEntity() { Id = billingConfigId, FieldTicketsUploadEnabled = true, FieldTicketDeliveryMethod = FieldTicketDeliveryMethod.TicketByTicket };
        SaveToDatabase(billingConfig);

        var loadConfirmationId = Guid.NewGuid();
        var loadConfirmation = new LoadConfirmationEntity() { Id = loadConfirmationId, BillingConfigurationId = billingConfig.Id };
        SaveToDatabase(loadConfirmation);

        SalesLineEntity salesLine = new SalesLineEntity { Status = SalesLineStatus.Approved, LoadConfirmationId = loadConfirmation.Id };
        SaveToDatabase(salesLine);

        //execute
        var result = _salesLineManager.ShouldRefreshPricing(GetPriceRefreshContext(salesLine));

        //assert
        AssertTaskResult(false, result, "Result should be False, DO NOT refresh pricing.");
    }

    [TestMethod]
    public void BUG12508_ShouldRefreshPricing_SalesLineStatusApproved_BillingConfigNull_ReturnsTrue()
    {
        //setup
        var loadConfirmationId = Guid.NewGuid();
        var loadConfirmation = new LoadConfirmationEntity() { Id = loadConfirmationId };
        SaveToDatabase(loadConfirmation);

        SalesLineEntity salesLine = new SalesLineEntity { Status = SalesLineStatus.Approved, LoadConfirmationId = loadConfirmation.Id };
        SaveToDatabase(salesLine);

        //execute
        var result = _salesLineManager.ShouldRefreshPricing(GetPriceRefreshContext(salesLine));

        //assert
        AssertTaskResult(true, result, "Result should be True, refresh pricing.");
    }

    [TestMethod]
    public void BUG12508_ShouldRefreshPricing_SalesLineStatusApproved_LoadConfirmationNull_ReturnsTrue()
    {
        //setup
        SalesLineEntity salesLine = new SalesLineEntity() { Status = SalesLineStatus.Approved };

        //execute
        var result = _salesLineManager.ShouldRefreshPricing(GetPriceRefreshContext(salesLine));

        //assert
        AssertTaskResult(true, result, "Result should be True, refresh pricing.");
    }

    [TestMethod]
    public void BUG12508_ShouldRefreshPricing_SalesLineStatusApproved_NoManualPriceChange_BillingConfigIsEmptyList_ReturnsTrue()
    {
        //setup
        SalesLineEntity salesLine = new SalesLineEntity() { Status = SalesLineStatus.Approved };

        _billingConfigurationProviderMock.SetupEntities(new List<BillingConfigurationEntity>());

        //execute
        var result = _salesLineManager.ShouldRefreshPricing(GetPriceRefreshContext(salesLine));

        //assert
        AssertTaskResult(true, result, "Result should be True, refresh pricing.");
    }

    [TestMethod]
    public void BUG12508_ShouldRefreshPricing_SalesLineStatusSentToFo_HasBeenManualPriceChange_ReturnsFalse()
    {
        //setup
        SalesLineEntity salesLine = new SalesLineEntity() { Status = SalesLineStatus.SentToFo, PriceChangeDate = DateTimeOffset.UtcNow, PriceChangeUserName = "mcarrick" };

        //execute
        var result = _salesLineManager.ShouldRefreshPricing(GetPriceRefreshContext(salesLine));

        //assert
        AssertFalseTaskResult(result, "Result should be False, DO NOT refresh pricing.");
    }

    [TestMethod]
    public void BUG12508_ShouldRefreshPricing_SalesLineStatusApproved_HasBeenManualPriceChange_ReturnsFalse()
    {
        //setup
        SalesLineEntity salesLine = new SalesLineEntity() { Status = SalesLineStatus.Approved, PriceChangeDate = DateTimeOffset.UtcNow, PriceChangeUserName = "mcarrick" };

        //execute
        var result = _salesLineManager.ShouldRefreshPricing(GetPriceRefreshContext(salesLine));

        //assert
        AssertFalseTaskResult(result, "Result should be False, DO NOT refresh pricing.");
    }

    [TestMethod]
    public void BUG12508_ShouldRefreshPricing_SalesLineStatusPreview_HasBeenManualPriceChange_ReturnsFalse()
    {
        //setup
        SalesLineEntity salesLine = new SalesLineEntity() { Status = SalesLineStatus.Preview, PriceChangeDate = DateTimeOffset.UtcNow, PriceChangeUserName = "mcarrick" };

        //execute
        var result = _salesLineManager.ShouldRefreshPricing(GetPriceRefreshContext(salesLine));

        //assert
        AssertFalseTaskResult(result, "Result should be False, DO NOT refresh pricing.");
    }

    [TestMethod]
    public void BUG12508_ShouldRefreshPricing_SalesLineStatusPosted_ReturnsFalse()
    {
        //setup
        SalesLineEntity salesLine = new SalesLineEntity() { Status = SalesLineStatus.Void };

        //execute
        var result = _salesLineManager.ShouldRefreshPricing(GetPriceRefreshContext(salesLine));

        //assert
        AssertFalseTaskResult(result, "Result should be False, DO NOT refresh pricing.");
    }

    [TestMethod]
    public void BUG12508_ShouldRefreshPricing_SalesLineStatusVoid_ReturnsFalse()
    {
        //setup
        SalesLineEntity salesLine = new SalesLineEntity() { Status = SalesLineStatus.Void };

        //execute
        var result = _salesLineManager.ShouldRefreshPricing(GetPriceRefreshContext(salesLine));

        //assert
        AssertFalseTaskResult(result, "Result should be False, DO NOT refresh pricing.");
    }

    [TestMethod]
    public void BUG12508_ShouldRefreshPricing_SalesLineStatusException_ReturnsTrue()
    {
        //setup
        SalesLineEntity salesLine = new SalesLineEntity() { Status = SalesLineStatus.Exception };

        //execute
        var result = _salesLineManager.ShouldRefreshPricing(GetPriceRefreshContext(salesLine));

        //assert
        AssertTrueTaskResult(result, "Result should be True, refresh pricing.");
    }

    [TestMethod]
    public void BUG12508_ShouldRefreshPricing_NullSalesLineReturnsTrue()
    {
        //setup

        //execute
        var result = _salesLineManager.ShouldRefreshPricing(GetPriceRefreshContext(null));

        //assert
        AssertTrueTaskResult(result, "Default should be True, refresh pricing.");
    }

    private static void AssertTaskResult(bool theBoolean, Task<bool> result, string message)
    {
        if (theBoolean)
        {
            AssertTrueTaskResult(result, message);
        }
        else
        {
            AssertFalseTaskResult(result, message);
        }
    }

    private static void AssertTrueTaskResult(Task<bool> result, string message)
    {
        Assert.IsTrue(result.Result, message);
    }

    private static void AssertFalseTaskResult(Task<bool> result, string message)
    {
        Assert.IsFalse(result.Result, message);
    }

    private void SaveToDatabase(SalesLineEntity salesLine)
    {
        _salesLineEntityProviderMock.SetupEntities(new List<SalesLineEntity>() { salesLine });
    }

    private void SaveToDatabase(LoadConfirmationEntity loadConfirmation)
    {
        _loadConfirmationProviderMock.SetupEntities(new List<LoadConfirmationEntity>() { loadConfirmation });
    }

    private void SaveToDatabase(BillingConfigurationEntity billingConfig)
    {
        _billingConfigurationProviderMock.SetupEntities(new List<BillingConfigurationEntity>() { billingConfig });
    }

    private void SaveToDatabase(AdditionalServicesConfigurationEntity additionalServicesConfig)
    {
        _additionalServiceConfigProviderMock.SetupEntities(new List<AdditionalServicesConfigurationEntity>() { additionalServicesConfig });
    }

    private void SaveToDatabase(ServiceTypeEntity servType)
    {
        _serviceTypeManagerManagerMock.SetupEntities(new List<ServiceTypeEntity>() { servType });
    }

    private static void AssertTrueTaskResult(Task<bool> result)
    {
        AssertTaskResult(true, result, "Result should be True, YES refresh pricing.");
    }
}
