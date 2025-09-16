using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Common.Extensions;
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
using SE.TridentContrib.Extensions.Azure.Functions;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Domain.Entities.FacilityService;
using SE.TruckTicketing.Domain.Entities.SalesLine.Utils;
using SE.TruckTicketing.Domain.Entities.TruckTicket;

using Trident.Business;
using Trident.Contracts;
using Trident.Data.Contracts;
using Trident.IoC;
using Trident.Logging;
using Trident.Validation;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.SalesLine;

public interface ISalesLineManager : IManager<Guid, SalesLineEntity>
{
    Task<double?> GetPrice(SalesLinePriceRequest priceRequest);

    Task<List<SalesLineEntity>> GeneratePreviewSalesLines(SalesLinePreviewRequest request);

    Task<List<SalesLineEntity>> PriceRefresh(List<SalesLineEntity> salesLines);

    Task<IEnumerable<SalesLineEntity>> RemoveSalesLinesFromLoadConfirmationOrInvoice(IEnumerable<Guid> truckTicketIds);
}

public partial class SalesLineManager : ManagerBase<Guid, SalesLineEntity>, ISalesLineManager
{
    private const double MinPricingPercentageEpsilon = 0.011;

    private readonly IProvider<Guid, AccountEntity> _accountProvider;

    private readonly IProvider<Guid, AdditionalServicesConfigurationEntity> _additionalServiceConfigProvider;

    private readonly IProvider<Guid, BillingConfigurationEntity> _billingConfigurationProvider;

    private readonly IProvider<Guid, FacilityEntity> _facilityProvider;

    private readonly IManager<Guid, FacilityServiceSubstanceIndexEntity> _facilityServiceSubstanceManager;

    private readonly IProvider<Guid, LoadConfirmationEntity> _loadConfirmationProvider;

    private readonly IProvider<Guid, MaterialApprovalEntity> _materialApprovalProvider;

    private readonly IMatchPredicateRankManager _predicateRankManager;

    private readonly IPricingRuleManager _pricingRuleManager;

    private readonly IManager<Guid, ProductEntity> _productManager;

    private readonly ISalesLinesPublisher _salesLinesPublisher;

    private readonly IIoCServiceLocator _serviceLocator;

    private readonly IManager<Guid, ServiceTypeEntity> _serviceTypeManager;

    private readonly IProvider<Guid, SourceLocationEntity> _sourceLocationProvider;

    public SalesLineManager(ILog logger,
                            IProvider<Guid, SalesLineEntity> provider,
                            IManager<Guid, ProductEntity> productManager,
                            IPricingRuleManager pricingRuleManager,
                            IProvider<Guid, SourceLocationEntity> sourceLocationProvider,
                            IProvider<Guid, FacilityEntity> facilityProvider,
                            IManager<Guid, FacilityServiceSubstanceIndexEntity> facilityServiceSubstanceManager,
                            IProvider<Guid, AccountEntity> accountProvider,
                            IManager<Guid, ServiceTypeEntity> serviceTypeManager,
                            IProvider<Guid, MaterialApprovalEntity> materialApprovalProvider,
                            ISalesLinesPublisher salesLinesPublisher,
                            IProvider<Guid, AdditionalServicesConfigurationEntity> additionalServiceConfigProvider,
                            IMatchPredicateRankManager predicateRankManager,
                            IProvider<Guid, BillingConfigurationEntity> billingConfigurationProvider,
                            IIoCServiceLocator serviceLocator,
                            IProvider<Guid, LoadConfirmationEntity> loadConfirmationProvider,
                            IValidationManager<SalesLineEntity> validationManager = null,
                            IWorkflowManager<SalesLineEntity> workflowManager = null)
        : base(logger, provider, validationManager, workflowManager)
    {
        _productManager = productManager;
        _pricingRuleManager = pricingRuleManager;
        _sourceLocationProvider = sourceLocationProvider;
        _facilityProvider = facilityProvider;
        _facilityServiceSubstanceManager = facilityServiceSubstanceManager;
        _accountProvider = accountProvider;
        _serviceTypeManager = serviceTypeManager;
        _materialApprovalProvider = materialApprovalProvider;
        _salesLinesPublisher = salesLinesPublisher;
        _additionalServiceConfigProvider = additionalServiceConfigProvider;
        _predicateRankManager = predicateRankManager;
        _billingConfigurationProvider = billingConfigurationProvider;
        _serviceLocator = serviceLocator;
        _loadConfirmationProvider = loadConfirmationProvider;
    }

    public async Task<double?> GetPrice(SalesLinePriceRequest priceRequest)
    {
        var facility = await _facilityProvider.GetById(priceRequest.FacilityId);
        var account = await _accountProvider.GetById(priceRequest.CustomerId);

        return (await GetPricing(priceRequest.TruckTicketDate, account, priceRequest.ProductNumber, facility.SiteId, priceRequest.SourceLocation))?.Price;
    }

    public async Task<List<SalesLineEntity>> PriceRefresh(List<SalesLineEntity> salesLines)
    {
        var accounts = new Dictionary<Guid, AccountEntity>();
        
        foreach (var salesLine in salesLines)
        {
            var priceRefreshContext = new PriceRefreshContext { 
                CurrentSalesLine = salesLine,
                SalesLines = salesLines,
                AdditionalServicesConfigProvider = _additionalServiceConfigProvider,
                LoadConfirmationProvider = _loadConfirmationProvider,
                BillingConfigurationProvider = _billingConfigurationProvider,
                ServiceTypeManager = _serviceTypeManager,
            };

            var hasValidRule = await ShouldRefreshPricing(priceRefreshContext);
            if (!hasValidRule)
            {
                continue;
            }

            if (!accounts.ContainsKey(salesLine.CustomerId))
            {
                var newAccount = await _accountProvider.GetById(salesLine.CustomerId);
                accounts.Add(salesLine.CustomerId, newAccount);
            }

            var account = accounts[salesLine.CustomerId];
            if (account == null)
            {
                SalesLineManagerHelper.ErrorOutSalesLineAndSetStatusToException(salesLine);
                continue;
            }

            var pricing = await GetPricing(salesLine.TruckTicketDate, account, salesLine.ProductNumber, salesLine.FacilitySiteId, salesLine.SourceLocationFormattedIdentifier);

            if (pricing is null)
            {
                SalesLineManagerHelper.SetStatusToException(salesLine);
            }

            SalesLineManagerHelper.SetSalesLineRateAndValues(salesLine, pricing);
        }

        return salesLines;
    }

    public async Task<List<SalesLineEntity>> GeneratePreviewSalesLines(SalesLinePreviewRequest request)
    {
        // todo: guards if needed
        // facility id
        // billing configuration id
        // facilityServiceSubstance id
        var statusToExcludeForAdditionalServiceCheck = new List<SalesLineStatus>
        {
            SalesLineStatus.Void,
            SalesLineStatus.Posted,
            SalesLineStatus.Approved,
            SalesLineStatus.SentToFo,
        };

        var persistedAdditionalServicesSalesLines = (await Get(x => x.TruckTicketId == request.TruckTicket.Id &&
                                                                    x.IsAdditionalService && !statusToExcludeForAdditionalServiceCheck.Contains(x.Status)))
                                                  ?.Where(x => x.IsUserAddedAdditionalServices.HasValue && x.IsUserAddedAdditionalServices.Value)
                                                   .ToList() ??
                                                    new List<SalesLineEntity>();

        if (request.UseNew)
        {
            var context = await CreateRequestContext(request, persistedAdditionalServicesSalesLines);
            context.PersistedSalesLines = new(persistedAdditionalServicesSalesLines);
            var constructedSalesLines = ConstructSalesLines(context);
            return constructedSalesLines.ToList();
        }

        var facility = await _facilityProvider.GetById(request.FacilityId);
        var sourceLocation = await _sourceLocationProvider.GetById(request.SourceLocationId);
        var account = await _accountProvider.GetById(request.BillingCustomerId);

        var facilityServiceSubstance = await _facilityServiceSubstanceManager.GetById(request.FacilityServiceSubstanceIndexId);
        var serviceType = await _serviceTypeManager.GetById(facilityServiceSubstance.ServiceTypeId);

        var salesLines = await ConstructSalesLinesSlowly(serviceType, request, facility, sourceLocation, account, persistedAdditionalServicesSalesLines);

        return salesLines.ToList();
    }

    /// <summary>
    ///     Remove all sales lines associated with a truck ticket from invoice and load confirmation.  Mark those sales line as "preview".  Mark truck ticket as "open"
    /// </summary>
    /// <param name="truckTicketIds">List of truck ticket ids</param>
    /// <returns>All updated sales lines</returns>
    public async Task<IEnumerable<SalesLineEntity>> RemoveSalesLinesFromLoadConfirmationOrInvoice(IEnumerable<Guid> truckTicketIds)
    {
        var updatedSalesLines = new List<SalesLineEntity>();
        var newSalesLines = new List<SalesLineEntity>();

        foreach (var truckTicketId in truckTicketIds)
        {
            var salesLines = (await Get(s => s.TruckTicketId == truckTicketId && // PK - XP for SL by TT ID
                                             s.Status != SalesLineStatus.Void &&
                                             s.Status != SalesLineStatus.Posted)).ToList();

            foreach (var salesLine in salesLines)
            {
                var newSalesLine = salesLine.CloneAsNew();
                await Insert(newSalesLine, true);
                newSalesLines.Add(newSalesLine);

                // keep the historical ID permanently only when it's not initialized
                if (salesLine.HistoricalInvoiceId.HasValue == false && salesLine.InvoiceId.HasValue)
                {
                    salesLine.HistoricalInvoiceId = salesLine.InvoiceId;
                }

                // validation should catch trying to update a sales line that has status "posted"
                SalesLineManagerHelper.VoidTheSalesLine(salesLine);
                ;
                var updatedSalesLine = await Update(salesLine, true);
                updatedSalesLines.Add(updatedSalesLine);
            }
        }

        await SaveDeferred();

        await _salesLinesPublisher.PublishSalesLines(updatedSalesLines, includePreview: true);

        return newSalesLines;
    }

    private async Task<ComputePricingResponse> GetPricing(DateTimeOffset? loadDate,
                                                          AccountEntity account,
                                                          string productNumber,
                                                          string siteId,
                                                          string sourceLocation)
    {
        var pricingRuleRequest = PricingRequestInitializer.Instance.Initialize(account, loadDate, productNumber, siteId, sourceLocation);

        var pricingResponse = await _pricingRuleManager.ComputePrice(pricingRuleRequest);
        var price = pricingResponse?.Find(p => p.ProductNumber == productNumber);
        return price;
    }

    private async Task<List<SalesLineEntity>> ConstructSalesLinesSlowly(ServiceTypeEntity serviceTypeEntity,
                                                                        SalesLinePreviewRequest request,
                                                                        FacilityEntity facility,
                                                                        SourceLocationEntity sourceLocation,
                                                                        AccountEntity account,
                                                                        List<SalesLineEntity> persistedAdditionalServicesSalesLines)
    {
        var jobLocation = sourceLocation.FormattedIdentifier.HasText() ? sourceLocation.FormattedIdentifier : sourceLocation.SourceLocationName;

        var salesLines = new List<SalesLineEntity>();

        var salesLineBase = SalesLineInitializer.Instance.InitializeSalesLine(request, facility, sourceLocation, account);

        var additionalServiceConfigs = (await _additionalServiceConfigProvider.Get(config => config.IsActive &&
                                                                                             config.FacilityId == request.FacilityId &&
                                                                                             (config.CustomerId == Guid.Empty ||
                                                                                              config.CustomerId == request.BillingCustomerId)))?.ToList() ?? new();

        var additionalServices = SelectAdditionalServiceConfig(additionalServiceConfigs, request);

        var otherServices = await GetReadOnlyServices(request);

        var additionalServiceSalesLines = new List<SalesLineEntity>();

        foreach ((AdditionalServicesConfigurationAdditionalServiceEntity service, bool isReadOnly) service in additionalServices.AdditionalServices.Select(s => (s, false))
                                                                                                                                .Union(otherServices.Select(s => (s, true))))
        {
            var price = await GetPricing(request.LoadDate, account, service.service.Number, facility.SiteId, jobLocation);
            var additionalService =
                SalesLineInitializer.Instance.InitializeAdditionalService(salesLineBase, service.service, request, price, facility.Type, service.isReadOnly, additionalServices.ZeroTotal);

            additionalServiceSalesLines.Add(additionalService);
        }

        if (otherServices.All(s => s.ProductId != serviceTypeEntity.TotalItemId))
        {
            var totalSalesLineProduct = await _productManager.GetById(serviceTypeEntity.TotalItemId);

            var totalSalesLineQuantity = request.TotalVolume > 0 ? request.TotalVolume : salesLineBase.GrossWeight - salesLineBase.TareWeight;
            var totalSalesLineQuantityPercent = request.TotalVolumePercent;
            var totalSalesLinePrice = additionalServices.ZeroTotal ? null : await GetPricing(request.LoadDate, account, totalSalesLineProduct.Number, facility.SiteId, jobLocation);

            var totalSalesLine = SalesLineInitializer.Instance.InitializeTotalSalesLine(salesLineBase, totalSalesLineProduct, totalSalesLinePrice, totalSalesLineQuantity,
                                                                                        totalSalesLineQuantityPercent, additionalServices.ZeroTotal);

            salesLines.Add(totalSalesLine);
        }

        if (serviceTypeEntity.IncludesOil && request.OilVolume > 0)
        {
            var creditMultiplier = serviceTypeEntity.OilItemReverse ? -1 : 1;
            var oilQuantity = request.OilVolume * creditMultiplier;
            var oilQuantityPercent = request.OilVolumePercent;
            var oilProduct = await _productManager.GetById(serviceTypeEntity.OilItemId);
            var oilPrice = additionalServices.ZeroOil ? null : await GetPricing(request.LoadDate, account, oilProduct.Number, facility.SiteId, jobLocation);

            // Do not grant oil credits if the oil volume is less than a set oil credit minimum volume
            if (serviceTypeEntity.OilItemReverse && serviceTypeEntity.OilCreditMinVolume > 0 && request.OilVolume < serviceTypeEntity.OilCreditMinVolume)
            {
                oilPrice = null;
            }

            var oilSalesLine = SalesLineInitializer.Instance.InitializeOilSalesLine(salesLineBase, oilProduct, oilPrice, oilQuantity, oilQuantityPercent);
            salesLines.Add(oilSalesLine);
        }

        if (serviceTypeEntity.IncludesWater && request.WaterVolume > 0)
        {
            var waterQuantity = request.WaterVolume;
            var waterQuantityPercent = request.WaterVolumePercent;
            var waterProduct = await _productManager.GetById(serviceTypeEntity.WaterItemId);
            var aboveMinPricingPercentageThreshold = waterQuantityPercent - serviceTypeEntity.WaterMinPricingPercentage.GetValueOrDefault(0) >= MinPricingPercentageEpsilon;
            var waterPrice = aboveMinPricingPercentageThreshold && !additionalServices.ZeroWater
                                 ? await GetPricing(request.LoadDate, account, waterProduct.Number, facility.SiteId, jobLocation)
                                 : null;

            var waterSalesLine = SalesLineInitializer.Instance.InitializeWaterSalesLine(salesLineBase, waterProduct, waterPrice, waterQuantity, waterQuantityPercent);
            salesLines.Add(waterSalesLine);
        }

        if (serviceTypeEntity.IncludesSolids && request.SolidVolume > 0)
        {
            var solidQuantity = request.SolidVolume;
            var solidQuantityPercent = request.SolidVolumePercent;
            var solidProduct = await _productManager.GetById(serviceTypeEntity.SolidItemId);
            var aboveMinPricingPercentageThreshold = solidQuantityPercent - serviceTypeEntity.SolidMinPricingPercentage.GetValueOrDefault(0) >= MinPricingPercentageEpsilon;
            var solidPrice = aboveMinPricingPercentageThreshold && !additionalServices.ZeroSolids
                                 ? await GetPricing(request.LoadDate, account, solidProduct.Number, facility.SiteId, jobLocation)
                                 : null;

            var solidSalesLine = SalesLineInitializer.Instance.InitializeSolidSalesLine(salesLineBase, solidProduct, solidPrice, solidQuantity, solidQuantityPercent);
            salesLines.Add(solidSalesLine);
        }

        if (request.MaterialApprovalNumber.HasText())
        {
            var materialApproval = await _materialApprovalProvider.GetById(request.MaterialApprovalId);
            var additionalService = await _productManager.GetById(materialApproval.AdditionalService);
            if (materialApproval.AdditionalServiceAdded && additionalService is not null)
            {
                var additionalServicePrice = await GetPricing(request.LoadDate, account, additionalService.Number, facility.SiteId, jobLocation);
                var additionalServiceSalesLine = SalesLineInitializer.Instance.InitializeAdditionalService(salesLineBase, additionalService, additionalServicePrice, additionalServices.ZeroTotal);
                salesLines.Add(additionalServiceSalesLine);
            }
        }

        //Add price refresh logic for persisted Sales Line for old logic
        foreach (var persistedSalesLine in persistedAdditionalServicesSalesLines)
        {
            var additionalServiceSalesLine = await _productManager.GetById(serviceTypeEntity.SolidItemId);
            var additionalServiceSalesLinePrice = await GetPricing(request.LoadDate, account, additionalServiceSalesLine.Number, facility.SiteId, jobLocation);
            persistedSalesLine.Rate = SalesLineManagerHelper.GetPriceOrZero(additionalServiceSalesLinePrice);
        }

        salesLines.AddRange(additionalServiceSalesLines);
        salesLines.AddRange(persistedAdditionalServicesSalesLines);

        foreach (var salesLine in salesLines)
        {
            salesLine.TotalValue = salesLine.Rate * salesLine.Quantity;
            salesLine.ApplyFoRounding();
        }

        return salesLines;
    }

    private async Task<List<AdditionalServicesConfigurationAdditionalServiceEntity>> GetReadOnlyServices(SalesLinePreviewRequest request)
    {
        // for service-only tickets, the ticket is already flagged as service-only
        if (request.TruckTicket.IsServiceOnlyTicket == true)
        {
            // fetch the service-only product
            var index = await _facilityServiceSubstanceManager.GetById(request.FacilityServiceSubstanceIndexId);
            var product = await _productManager.GetById(index.TotalProductId);

            // safety check to ensure the product is a service-only product
            if (product.IsServiceOnlyProduct())
            {
                // mimic the additional service configuration, however, flag it as read-only, a user must not delete or change it
                var service = AdditionalServicesConfigurationAdditionalServiceEntityInitializer.Instance.InitializeNew(product);

                return new(new[] { service });
            }
        }

        return new();
    }

    public async Task<bool> ShouldRefreshPricing(PriceRefreshContext priceRefreshContext)
    {
        var refreshPricingStrategy = ShouldRefreshPricingFactory.GetStrategy(priceRefreshContext);
        
        return await refreshPricingStrategy.ShouldRefreshPricing();
    }
    
    private static IEnumerable<SalesLineEntity> ConstructSalesLines(SalesLinePreviewRequestContext context)
    {
        var request = context.Request;

        var serviceType = context.ServiceType;

        var salesLines = new List<SalesLineEntity>();

        var previewStatusSalesLine = SalesLineInitializer.Instance.InitializeSalesLine(context.Request, context.Facility, context.SourceLocation, context.Account);

        var additionalServicesConfig = context.AdditionalServicesConfig;

        var totalProduct = context.ProductMap[serviceType.TotalItemId];
        
        if (request.TruckTicket.IsServiceOnlyTicket is true)
        {
            var additionalService = SalesLineInitializer.InitializeAdditionalServiceSalesLine(context, totalProduct, additionalServicesConfig, previewStatusSalesLine, request);
            salesLines.Add(additionalService);
        }
        else
        {
            var totalSalesLine = SalesLineInitializer.InitializeTotalSalesLine(context, request, previewStatusSalesLine, additionalServicesConfig, totalProduct);
            salesLines.Add(totalSalesLine);
        }

        if (context.ServiceType.IncludesOil && context.Request.OilVolume > 0)
        {
            var oilSalesLine = SalesLineInitializer.InitializeOilSalesLine(context, serviceType, request, additionalServicesConfig, previewStatusSalesLine);
            salesLines.Add(oilSalesLine);
        }

        if (serviceType.IncludesWater && request.WaterVolume > 0)
        {
            var waterSalesLine = SalesLineInitializer.InitializeWaterSalesLine(context, request, serviceType, additionalServicesConfig, previewStatusSalesLine);
            salesLines.Add(waterSalesLine);
        }

        if (serviceType.IncludesSolids && request.SolidVolume > 0)
        {
            var solidSalesLine = SalesLineInitializer.InitializeSolidSalesLine(context, request, serviceType, additionalServicesConfig, previewStatusSalesLine);
            salesLines.Add(solidSalesLine);
        }

        if (request.MaterialApprovalNumber.HasText())
        {
            var materialApproval = context.MaterialApproval;

            var additionalService = context.ProductMap.TryGetValue(materialApproval.AdditionalService, out var additionServicesEntity) ? additionServicesEntity : null;

            if (materialApproval.AdditionalServiceAdded && additionalService is not null)
            {
                var price = SalesLineManagerHelper.GetPricingResponseFromPricingProductMap(context, additionalService.Number);

                var additionalServiceSalesLine = SalesLineInitializer.Instance.InitializeAdditionalService(previewStatusSalesLine, additionalService, price, additionalServicesConfig.ZeroTotal);
                salesLines.Add(additionalServiceSalesLine);
            }
        }

        foreach (var additionalServicesConfigurationAdditionalServiceEntity in additionalServicesConfig.AdditionalServices)
        {
            var price = SalesLineManagerHelper.GetPricingResponseFromPricingProductMap(context, additionalServicesConfigurationAdditionalServiceEntity.Number);

            var additionalService = SalesLineInitializer.Instance.InitializeAdditionalService(previewStatusSalesLine, additionalServicesConfigurationAdditionalServiceEntity, request, price,
                                                                                              context.Facility.Type, true, additionalServicesConfig.ZeroTotal);

            salesLines.Add(additionalService);
        }

        foreach (var persistedSalesLine in context.PersistedSalesLines)
        {
            var salesLineProductNumber = context.ProductMap[persistedSalesLine.ProductId].Number;
            var price = SalesLineManagerHelper.GetPricingResponseFromPricingProductMap(context, salesLineProductNumber);
            var persistedService = SalesLineInitializer.Instance.InitializePersistedAdditionalService(previewStatusSalesLine, persistedSalesLine, price);

            salesLines.Add(persistedService);
        }

        salesLines.ForEach(salesLine => salesLine.ApplyFoRounding());

        return salesLines;
    }

    private AdditionalServicesConfig SelectAdditionalServiceConfig(IReadOnlyCollection<AdditionalServicesConfigurationEntity> additionalServiceConfigs, SalesLinePreviewRequest request)
    {
        var rankConfigs = additionalServiceConfigs.SelectMany(config => config.MatchCriteria.Select(criteria => new RankConfiguration
        {
            EntityId = config.Id,
            Name = config.Name,
            Predicates = AdditionalServicesConfigurationHelper.BuildMatchPredicates(criteria),
        })).ToList();

        var values = new[] { $"WellClassification:{request.WellClassification}", $"SourceLocation:{request.SourceLocationId}", $"FacilityServiceSubstance:{request.FacilityServiceSubstanceIndexId}" };
        var weights = new Dictionary<string, int>
        {
            ["WellClassification"] = 1,
            ["SourceLocation"] = 1,
            ["FacilityServiceSubstance"] = 1,
        };

        var validConfigIds = _predicateRankManager.EvaluatePredicateRank(rankConfigs, values, weights, "*", false, true).Select(config => config.EntityId).ToHashSet();
        var validConfigs = additionalServiceConfigs.Where(config => validConfigIds.Contains(config.Id)).ToList();

        return AdditionalServicesConfigurationEntityInitializer.Instance.Initialize(validConfigs);
    }

    private async Task<SalesLinePreviewRequestContext> CreateRequestContext(SalesLinePreviewRequest request, List<SalesLineEntity> persistedAdditionalServices)
    {
        var requestContext = new SalesLinePreviewRequestContext { Request = request };

        await LoadOperationalEntities(requestContext);
        await LoadProducts(requestContext);
        await LoadPricing(requestContext);

        return requestContext;

        async Task LoadOperationalEntities(SalesLinePreviewRequestContext context)
        {
            await Task.WhenAll(LoadFacility(context),
                               LoadSourceLocation(context),
                               LoadAccount(context),
                               LoadServiceType(context),
                               LoadMaterialApproval(context),
                               LoadAdditionalServicesConfig(context));
        }

        async Task LoadFacility(SalesLinePreviewRequestContext context)
        {
            // create a new scope to isolate changes
            using var scope = _serviceLocator.CreateChildLifetimeScope();

            // init app-insights logger
            InitAppInsight(scope);

            // save in the separate context since this upgrade method can be called from anywhere, including read/mixed methods
            var facilityProvider = scope.Get<IProvider<Guid, FacilityEntity>>();

            // save the upgraded index
            context.Facility = await facilityProvider.GetById(request.FacilityId);
        }

        async Task LoadSourceLocation(SalesLinePreviewRequestContext context)
        {
            // create a new scope to isolate changes
            using var scope = _serviceLocator.CreateChildLifetimeScope();

            // init app-insights logger
            InitAppInsight(scope);

            // save in the separate context since this upgrade method can be called from anywhere, including read/mixed methods
            var sourceLocationProvider = scope.Get<IProvider<Guid, SourceLocationEntity>>();

            // save the upgraded index
            context.SourceLocation = await sourceLocationProvider.GetById(request.SourceLocationId);
        }

        async Task LoadAccount(SalesLinePreviewRequestContext context)
        {
            // create a new scope to isolate changes
            using var scope = _serviceLocator.CreateChildLifetimeScope();

            // init app-insights logger
            InitAppInsight(scope);

            // save in the separate context since this upgrade method can be called from anywhere, including read/mixed methods
            var accountProvider = scope.Get<IProvider<Guid, AccountEntity>>();

            // save the upgraded index
            context.Account = await accountProvider.GetById(request.BillingCustomerId);
        }

        async Task LoadServiceType(SalesLinePreviewRequestContext context)
        {
            // create a new scope to isolate changes
            using var scope = _serviceLocator.CreateChildLifetimeScope();

            // init app-insights logger
            InitAppInsight(scope);

            // save in the separate context since this upgrade method can be called from anywhere, including read/mixed methods
            var serviceTypeProvider = scope.Get<IProvider<Guid, ServiceTypeEntity>>();

            // save the upgraded index
            context.ServiceType = await serviceTypeProvider.GetById(request.ServiceTypeId);
        }

        async Task LoadMaterialApproval(SalesLinePreviewRequestContext context)
        {
            if (!context.Request.MaterialApprovalNumber.HasText())
            {
                return;
            }

            // create a new scope to isolate changes
            using var scope = _serviceLocator.CreateChildLifetimeScope();

            // init app-insights logger
            InitAppInsight(scope);

            // save in the separate context since this upgrade method can be called from anywhere, including read/mixed methods
            var materialApprovalProvider = scope.Get<IProvider<Guid, MaterialApprovalEntity>>();

            // save the upgraded index
            context.MaterialApproval = await materialApprovalProvider.GetById(request.MaterialApprovalId);
        }

        async Task LoadAdditionalServicesConfig(SalesLinePreviewRequestContext context)
        {
            // create a new scope to isolate changes
            using var scope = _serviceLocator.CreateChildLifetimeScope();

            // init app-insights logger
            InitAppInsight(scope);

            // save in the separate context since this upgrade method can be called from anywhere, including read/mixed methods
            var additionalServicesConfigProvider = scope.Get<IProvider<Guid, AdditionalServicesConfigurationEntity>>();

            // save the upgraded index
            var additionalServicesConfigs = await additionalServicesConfigProvider.Get(config => config.IsActive &&
                                                                                                 config.FacilityId == request.FacilityId &&
                                                                                                 (config.CustomerId == Guid.Empty || config.CustomerId == request.BillingCustomerId));

            context.AdditionalServicesConfig = SelectAdditionalServiceConfig(additionalServicesConfigs.ToList(), request);
        }

        async Task LoadProducts(SalesLinePreviewRequestContext context)
        {
            var productIds = new List<Guid>();

            // Get products from main service type
            var serviceType = context.ServiceType;
            productIds.Add(serviceType.TotalItemId);
            productIds.Add(serviceType.OilItemId);
            productIds.Add(serviceType.SolidItemId);
            productIds.Add(serviceType.WaterItemId);

            // Get additional service product id from material approval
            var materialApproval = context.MaterialApproval;
            if (materialApproval is not null && materialApproval.AdditionalServiceAdded)
            {
                productIds.Add(materialApproval.AdditionalService);
            }

            // Get additional service product ids from additional services config
            var additionalServicesConfig = context.AdditionalServicesConfig;
            productIds.AddRange(additionalServicesConfig.AdditionalServices.Select(service => service.ProductId));

            //Get additional service product ids from manually added additional services on truckticket
            productIds.AddRange(persistedAdditionalServices.Select(additional => additional.ProductId));

            // Now load all products in one fell swoop
            var productIdsToFetch = productIds.Distinct().Where(id => id != Guid.Empty).ToList();

            // create a new scope to isolate changes
            using var scope = _serviceLocator.CreateChildLifetimeScope();

            // init app-insights logger
            InitAppInsight(scope);

            // save in the separate context since this upgrade method can be called from anywhere, including read/mixed methods
            var productProvider = scope.Get<IProvider<Guid, ProductEntity>>();
            var products = await productProvider.Get(product => productIdsToFetch.Contains(product.Id));
            context.ProductMap = products.ToDictionary(product => product.Id);
        }

        async Task LoadPricing(SalesLinePreviewRequestContext context)
        {
            var account = context.Account;
            var products = context.ProductMap.Values.ToList();

            var pricingRequest = new ComputePricingRequest
            {
                Date = request.LoadDate ?? DateTimeOffset.UtcNow,
                ProductNumber = products.Select(product => product.Number).ToList(),
                SiteId = context.Facility.SiteId,
                CustomerGroup = account.CustomerNumber,
                CustomerNumber = account.PriceGroup.HasText() ? account.PriceGroup : account.CustomerNumber,
                TieredPriceGroup = account.TmaGroup,
                SourceLocation = context.SourceLocation.GetCountryAgnosticIdentifier(),
            };

            var response = await _pricingRuleManager.ComputePrice(pricingRequest);
            context.PricingByProductNumberMap = response.ToDictionary(pricing => pricing.ProductNumber);
        }
    }

    private static void InitAppInsight(IIoCServiceLocator scope)
    {
        // init app-insights logger
        var logger = scope.Get<ILog>();
        var functionContextAccessor = new FunctionContextAccessor();
        logger.SetCallContext(functionContextAccessor.FunctionContext);
    }
}

public record PriceRefreshContext
{
    public IProvider<Guid, AdditionalServicesConfigurationEntity> AdditionalServicesConfigProvider { get; set; }
    public SalesLineEntity CurrentSalesLine { get; set; }
    public List<SalesLineEntity> SalesLines { get; set; }
    public IProvider<Guid, LoadConfirmationEntity> LoadConfirmationProvider { get; set; }
    public IProvider<Guid, BillingConfigurationEntity> BillingConfigurationProvider { get; set; }
    public IManager<Guid, ServiceTypeEntity> ServiceTypeManager { get; set; }
}
