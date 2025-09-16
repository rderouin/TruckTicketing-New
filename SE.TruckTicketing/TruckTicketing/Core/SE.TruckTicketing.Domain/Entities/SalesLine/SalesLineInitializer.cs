using System;
using System.Linq;

using SE.Shared.Common.Extensions;
using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.AdditionalServicesConfiguration;
using SE.Shared.Domain.Entities.EDIFieldValue;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.ServiceType;
using SE.Shared.Domain.Entities.SourceLocation;
using SE.Shared.Domain.PricingRules;
using SE.Shared.Domain.Product;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Extensions;

namespace SE.TruckTicketing.Domain.Entities.SalesLine;

public class SalesLineInitializer
{
    private const double MinPricingPercentageEpsilon = 0.011;

    public static SalesLineInitializer Instance = new();

    public static SalesLineEntity InitializeTotalSalesLine(SalesLinePreviewRequestContext context,
                                                           SalesLinePreviewRequest request,
                                                           SalesLineEntity salesLineBase,
                                                           AdditionalServicesConfig additionalServicesConfig,
                                                           ProductEntity product)
    {
        var quantity = request.TotalVolume > 0 ? request.TotalVolume : salesLineBase.GrossWeight - salesLineBase.TareWeight;

        var quantityPercent = request.TotalVolumePercent;

        var price = additionalServicesConfig.ZeroTotal ? null : SalesLineManagerHelper.GetPricingResponseFromPricingProductMap(context, product.Number);

        return Instance.InitializeTotalSalesLine(salesLineBase, product, price, quantity, quantityPercent, additionalServicesConfig.ZeroTotal);
    }

    public SalesLineEntity InitializeTotalSalesLine(SalesLineEntity salesLine, ProductEntity product, ComputePricingResponse price, double quantity, double quantityPercent, bool isZeroTotal)
    {
        var totalSalesLine = Instance.InitializeTotalSalesLine(salesLine, product, price, quantity, quantityPercent);
        totalSalesLine.CanPriceBeRefreshed = SalesLineManagerHelper.CanPriceBeRefreshed(isZeroTotal, totalSalesLine);
        return totalSalesLine;
    }

    public SalesLineEntity InitializeTotalSalesLine(SalesLineEntity salesLineBase, ProductEntity product, ComputePricingResponse price, double quantity, double quantityPercent)
    {
        var salesLine = Instance.Initialize(salesLineBase, product, price, quantity, quantityPercent, false);
        salesLine.CutType = SalesLineCutType.Total;
        return salesLine;
    }

    public static SalesLineEntity InitializeSolidSalesLine(SalesLinePreviewRequestContext context,
                                                           SalesLinePreviewRequest request,
                                                           ServiceTypeEntity serviceType,
                                                           AdditionalServicesConfig additionalServicesConfig,
                                                           SalesLineEntity salesLineBase)
    {
        var solidQuantity = request.SolidVolume;
        var solidQuantityPercent = request.SolidVolumePercent;
        var solidProduct = context.ProductMap[serviceType.SolidItemId];
        var aboveMinPricingPercentageThreshold = solidQuantityPercent - serviceType.SolidMinPricingPercentage.GetValueOrDefault(0) >= MinPricingPercentageEpsilon;

        var solidPrice = aboveMinPricingPercentageThreshold && !additionalServicesConfig.ZeroSolids
                             ? SalesLineManagerHelper.GetPricingResponseFromPricingProductMap(context, solidProduct.Number)
                             : null;

        return Instance.InitializeSolidSalesLine(salesLineBase, solidProduct, solidPrice, solidQuantity, solidQuantityPercent);
    }

    public SalesLineEntity InitializeSolidSalesLine(SalesLineEntity salesLineBase, ProductEntity product, ComputePricingResponse price, double quantity, double quantityPercent)
    {
        var salesLine = Instance.Initialize(salesLineBase, product, price, quantity, quantityPercent, true);
        salesLine.CutType = SalesLineCutType.Solid;
        return salesLine;
    }

    public static SalesLineEntity InitializeWaterSalesLine(SalesLinePreviewRequestContext context,
                                                           SalesLinePreviewRequest request,
                                                           ServiceTypeEntity serviceType,
                                                           AdditionalServicesConfig additionalServicesConfig,
                                                           SalesLineEntity salesLineBase)
    {
        var waterQuantity = request.WaterVolume;
        var waterQuantityPercent = request.WaterVolumePercent;
        var waterProduct = context.ProductMap[serviceType.WaterItemId];
        var aboveMinPricingPercentageThreshold = waterQuantityPercent - serviceType.WaterMinPricingPercentage.GetValueOrDefault(0) >= MinPricingPercentageEpsilon;

        var waterPrice = aboveMinPricingPercentageThreshold && !additionalServicesConfig.ZeroWater
                             ? SalesLineManagerHelper.GetPricingResponseFromPricingProductMap(context, waterProduct.Number)
                             : null;

        return Instance.InitializeWaterSalesLine(salesLineBase, waterProduct, waterPrice, waterQuantity, waterQuantityPercent);
    }

    public SalesLineEntity InitializeWaterSalesLine(SalesLineEntity salesLineBase, ProductEntity product, ComputePricingResponse price, double quantity, double quantityPercent)
    {
        var salesLine = Instance.Initialize(salesLineBase, product, price, quantity, quantityPercent, true);
        salesLine.CutType = SalesLineCutType.Water;
        return salesLine;
    }

    public static SalesLineEntity InitializeOilSalesLine(SalesLinePreviewRequestContext context,
                                                         ServiceTypeEntity serviceType,
                                                         SalesLinePreviewRequest request,
                                                         AdditionalServicesConfig additionalServicesConfig,
                                                         SalesLineEntity salesLineBase)
    {
        var creditMultiplier = serviceType.OilItemReverse ? -1 : 1;
        var oilQuantity = request.OilVolume * creditMultiplier;
        var oilQuantityPercent = request.OilVolumePercent;
        var oilProduct = context.ProductMap[serviceType.OilItemId];

        var oilPrice = additionalServicesConfig.ZeroOil ? null : SalesLineManagerHelper.GetPricingResponseFromPricingProductMap(context, oilProduct.Number);

        // Do not grant oil credits if the oil volume is less than a set oil credit minimum volume

        if (serviceType.OilItemReverse && serviceType.OilCreditMinVolume > 0 && request.OilVolume < serviceType.OilCreditMinVolume)
        {
            oilPrice = null;
        }

        return Instance.InitializeOilSalesLine(salesLineBase, oilProduct, oilPrice, oilQuantity, oilQuantityPercent);
    }

    public SalesLineEntity InitializeOilSalesLine(SalesLineEntity salesLineBase, ProductEntity product, ComputePricingResponse price, double quantity, double quantityPercent)
    {
        var salesLine = Instance.Initialize(salesLineBase, product, price, quantity, quantityPercent, true);
        salesLine.CutType = SalesLineCutType.Oil;
        return salesLine;
    }

    public SalesLineEntity InitializeAdditionalService(SalesLineEntity salesLineBase, ProductEntity product, ComputePricingResponse price)
    {
        var salesLine = Instance.Initialize(salesLineBase, product, price, 1, 100, false);
        salesLine.IsAdditionalService = true;
        return salesLine;
    }

    public SalesLineEntity Initialize(SalesLineEntity salesLineBase, ProductEntity product, ComputePricingResponse price, double quantity, double quantityPercent, bool isCutLine)
    {
        var salesLine = salesLineBase.Clone();
        salesLine.Quantity = quantity;
        salesLine.QuantityPercent = quantityPercent;
        salesLine.Rate = SalesLineManagerHelper.GetPriceOrZero(price);
        salesLine.PricingRuleId = price?.PricingRuleId;
        salesLine.IsCutLine = isCutLine;
        salesLine.ProductId = product.Id;
        salesLine.ProductName = product.Name;
        salesLine.ProductNumber = product.Number;
        salesLine.UnitOfMeasure = product.UnitOfMeasure;
        salesLine.CanPriceBeRefreshed = true; //default this sucker

        if (price is null)
        {
            salesLine.Status = SalesLineStatus.Exception;
        }

        return salesLine;
    }

    public SalesLineEntity InitializeSalesLine(SalesLinePreviewRequest request, FacilityEntity facility, SourceLocationEntity sourceLocation, AccountEntity customer)
    {
        var truckTicket = request.TruckTicket;
        var salesLine = new SalesLineEntity
        {
            // TruckTicket Data
            TruckTicketId = truckTicket.Id,
            WellClassification = truckTicket.WellClassification,
            TruckTicketDate = truckTicket.LoadDate ?? default,
            TruckTicketEffectiveDate = truckTicket.EffectiveDate ?? DateTime.Today,
            GeneratorId = truckTicket.GeneratorId,
            GeneratorName = truckTicket.GeneratorName,
            TruckTicketNumber = truckTicket.TicketNumber,
            EdiFieldValues = truckTicket.EdiFieldValues?.Select(v => new EDIFieldValueEntity
            {
                EDIFieldDefinitionId = v.EDIFieldDefinitionId,
                EDIFieldName = v.EDIFieldName,
                EDIFieldValueContent = v.EDIFieldValueContent,
                Id = v.Id,
            }).ToList(),
            IsEdiValid = truckTicket.IsEdiValid,
            DowNonDow = truckTicket.DowNonDow,
            BillOfLading = truckTicket.BillOfLading,
            ManifestNumber = truckTicket.ManifestNumber,
            TareWeight = request.TareWeight,
            GrossWeight = request.GrossWeight,
            MaterialApprovalId = request.MaterialApprovalId,
            MaterialApprovalNumber = request.MaterialApprovalNumber,
            ServiceTypeId = request.ServiceTypeId,
            ServiceTypeName = request.ServiceTypeName,
            TruckingCompanyId = request.TruckingCompanyId,
            TruckingCompanyName = request.TruckingCompanyName,
            Substance = truckTicket.SubstanceName,
            IsRateOverridden = false,
            SourceLocationId = sourceLocation?.Id ?? Guid.Empty,
            SourceLocationFormattedIdentifier = sourceLocation?.FormattedIdentifier.HasText() ?? false ? sourceLocation.FormattedIdentifier : sourceLocation?.SourceLocationName,
            SourceLocationIdentifier = sourceLocation?.Identifier,
            SourceLocationTypeName = sourceLocation?.SourceLocationTypeName,
            // Facility Data
            FacilityId = facility.Id,
            FacilitySiteId = facility.SiteId,
            BusinessUnit = facility.BusinessUnitId,
            Division = facility.Division,
            LegalEntity = facility.LegalEntity,

            // Account and/or Customer Data
            CustomerId = truckTicket.BillingCustomerId,
            CustomerName = truckTicket.BillingCustomerName,
            CustomerNumber = customer.CustomerNumber,
            AccountNumber = customer.AccountNumber,
            Attachments = truckTicket.Attachments?
                                     .Select(attachment => new SalesLineAttachmentEntity
                                      {
                                          Container = attachment.Container,
                                          Path = attachment.Path,
                                          File = attachment.File,
                                          Id = attachment.Id,
                                      })
                                     .ToList() ?? new(),
            IsAdditionalService = false,
            Status = SalesLineStatus.Preview,
            CanPriceBeRefreshed = true,
        };

        return salesLine;
    }

    public SalesLineEntity InitializeAdditionalService(SalesLineEntity salesLineBase, ProductEntity product, ComputePricingResponse price, bool isZeroTotal)
    {
        var additionalServiceSalesLine = Instance.InitializeAdditionalService(salesLineBase, product, price);
        additionalServiceSalesLine.CanPriceBeRefreshed = SalesLineManagerHelper.CanPriceBeRefreshed(isZeroTotal, additionalServiceSalesLine);
        return additionalServiceSalesLine;
    }

    public SalesLineEntity InitializeAdditionalService(SalesLineEntity salesLineBase,
                                                       AdditionalServicesConfigurationAdditionalServiceEntity additionalServiceConfig,
                                                       SalesLinePreviewRequest request,
                                                       ComputePricingResponse price,
                                                       FacilityType facilityType,
                                                       bool isReadOnlyLine,
                                                       bool isZeroTotal)
    {
        var additionalService = Instance.InitializeAdditionalService(salesLineBase, additionalServiceConfig, request, price, facilityType, isReadOnlyLine);
        additionalService.CanPriceBeRefreshed = SalesLineManagerHelper.CanPriceBeRefreshed(isZeroTotal, additionalService);
        return additionalService;
    }

    public static SalesLineEntity InitializeAdditionalServiceSalesLine(SalesLinePreviewRequestContext context,
                                                                       ProductEntity totalProduct,
                                                                       AdditionalServicesConfig additionalServicesConfig,
                                                                       SalesLineEntity salesLineBase,
                                                                       SalesLinePreviewRequest request)
    {
        var additionalService = AdditionalServicesConfigurationAdditionalServiceEntityInitializer.Instance.InitializeNew(totalProduct);

        var price = additionalServicesConfig.ZeroTotal ? null : SalesLineManagerHelper.GetPricingResponseFromPricingProductMap(context, additionalService.Number);

        var additionalServiceSalesLine = Instance.InitializeAdditionalService(salesLineBase, additionalService, request, price, context.Facility.Type, true);

        additionalServiceSalesLine.CanPriceBeRefreshed = SalesLineManagerHelper.CanPriceBeRefreshed(additionalServicesConfig.ZeroTotal, additionalServiceSalesLine);

        return additionalServiceSalesLine;
    }

    public SalesLineEntity InitializeAdditionalService(SalesLineEntity salesLineBase,
                                                       AdditionalServicesConfigurationAdditionalServiceEntity additionalService,
                                                       SalesLinePreviewRequest request,
                                                       ComputePricingResponse price,
                                                       FacilityType facilityType,
                                                       bool isReadOnlyLine)
    {
        var truckTicketQuantity = request.TruckTicket.TruckTicketType == TruckTicketType.LF || facilityType == FacilityType.Lf ? request.TruckTicket.NetWeight : request.TruckTicket.LoadVolume ?? 0.00;

        var quantity = additionalService.PullQuantityFromTicket.HasValue && additionalService.PullQuantityFromTicket.Value ? truckTicketQuantity : additionalService.Quantity;

        return InitAdditionalServiceBase(salesLineBase, price, additionalService.ProductId, additionalService.Name, additionalService.Number, additionalService.UnitOfMeasure, quantity,
                                         isReadOnlyLine);
    }

    public SalesLineEntity InitializePersistedAdditionalService(SalesLineEntity salesLineBase,
                                                                SalesLineEntity persistedSalesLineBase,
                                                                ComputePricingResponse price,
                                                                bool isReadOnlyLine = false)
    {
        //Persist same Id to update existing persisted line and not create duplicate lines.
        salesLineBase.Id = persistedSalesLineBase.Id;
        return InitAdditionalServiceBase(salesLineBase, price, persistedSalesLineBase.ProductId, persistedSalesLineBase.ProductName, persistedSalesLineBase.ProductNumber,
                                         persistedSalesLineBase.UnitOfMeasure, persistedSalesLineBase.Quantity, isReadOnlyLine);
    }

    private static SalesLineEntity InitAdditionalServiceBase(SalesLineEntity salesLineBase,
                                                             ComputePricingResponse price,
                                                             Guid productId,
                                                             string productName,
                                                             string productNumber,
                                                             string unitOfMeasure,
                                                             double quantity,
                                                             bool isReadOnlyLine)
    {
        var salesLine = salesLineBase.Clone();
        salesLine.Quantity = quantity;
        salesLine.Rate = SalesLineManagerHelper.GetPriceOrZero(price);
        salesLine.PricingRuleId = price?.PricingRuleId;
        salesLine.IsCutLine = false;
        salesLine.IsAdditionalService = true;
        salesLine.ProductId = productId;
        salesLine.ProductName = productName;
        salesLine.ProductNumber = productNumber;
        salesLine.UnitOfMeasure = unitOfMeasure;
        salesLine.IsReadOnlyLine = isReadOnlyLine;
        salesLine.CanPriceBeRefreshed = true;
        return salesLine;
    }
}
