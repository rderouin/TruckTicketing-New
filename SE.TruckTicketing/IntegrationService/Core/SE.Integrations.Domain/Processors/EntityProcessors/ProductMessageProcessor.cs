using System;
using System.Linq;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Constants;
using SE.Enterprise.Contracts.Models;
using SE.Shared.Domain.LegalEntity;
using SE.Shared.Domain.Product;

using Trident.Contracts;
using Trident.Data.Contracts;
using Trident.Logging;

namespace SE.Integrations.Domain.Processors.EntityProcessors;

[EntityProcessorFor(ServiceBusConstants.EntityMessageTypes.Product)]
public class ProductMessageProcessor : BaseEntityProcessor<FoProduct>
{
    private readonly IProvider<Guid, LegalEntityEntity> _legalEntityProvider;

    private readonly ILog _log;

    private readonly IManager<Guid, ProductEntity> _productManager;

    public ProductMessageProcessor(IManager<Guid, ProductEntity> productManager,
                                   IProvider<Guid, LegalEntityEntity> legalEntityProvider,
                                   ILog log)
    {
        _productManager = productManager;
        _legalEntityProvider = legalEntityProvider;
        _log = log;
    }

    public override async Task Process(EntityEnvelopeModel<FoProduct> message)
    {
        var existingProduct = await _productManager.GetById(message.EnterpriseId);
        var product = MapProductEntity(message.Payload, existingProduct ?? new());
        await EnrichLegalEntityInfo(message.Payload, product);

        if (existingProduct?.LastIntegrationTimestamp > message.MessageDate)
        {
            _log.Warning(messageTemplate: $"Message is outdated. (CorrelationId: {message.CorrelationId})");
            return;
        }

        product.LastIntegrationTimestamp = message.MessageDate;
        product.Id = message.EnterpriseId;

        await _productManager.Save(product);
    }

    private async Task EnrichLegalEntityInfo(FoProduct source, ProductEntity productEntity)
    {
        var legalEntity = (await _legalEntityProvider.Get(p => p.Code.ToLower() == source.DataAreaId.ToLower()))?.FirstOrDefault();

        productEntity.LegalEntityId = legalEntity?.Id ?? Guid.Empty;
        productEntity.LegalEntityCode = legalEntity?.Code ?? String.Empty;
    }

    private ProductEntity MapProductEntity(FoProduct product, ProductEntity entity)
    {
        entity.Id = product.Id;
        entity.Name = product.ItemName;
        entity.Number = product.ItemId;
        entity.UnitOfMeasure = product.SalesUnitId;
        entity.DisposalUnit = product.SalesUnitId;
        entity.AllowedSites = new()
        {
            List = product.DefaultOrderSettings.Select(setting => setting.SalesSite).ToList(),
        };

        entity.Categories = new()
        {
            List = product.ProductCategories.Select(category => category.CategoryId).ToList(),
        };

        entity.Substances = product.ProductVariants?.Select(variant => new ProductSubstanceEntity
        {
            SubstanceName = variant.Substance,
            WasteCode = variant.WasteCode,
        }).ToList();

        entity.IsActive = product.CanBeUsedInTT;

        return entity;
    }
}

public class FoProduct
{
    public Guid Id { get; set; }

    public string ItemId { get; set; }

    public string ItemType { get; set; }

    public string ItemName { get; set; }

    public string DataAreaId { get; set; }

    public string SalesUnitId { get; set; }

    public DefaultOrderSetting[] DefaultOrderSettings { get; set; } = Array.Empty<DefaultOrderSetting>();

    public ProductCategory[] ProductCategories { get; set; } = Array.Empty<ProductCategory>();

    public ProductVariant[] ProductVariants { get; set; } = Array.Empty<ProductVariant>();

    public bool CanBeUsedInTT { get; set; }

    public class DefaultOrderSetting
    {
        public string SalesSite { get; set; }

        public bool SalesStopped { get; set; }
    }

    public class ProductCategory
    {
        public string CategoryId { get; set; }

        public string CategoryHierarchyId { get; set; }
    }

    public class ProductVariant
    {
        public string Substance { get; set; }

        public string WasteCode { get; set; }
    }
}
