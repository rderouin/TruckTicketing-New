using System;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Product;
using SE.TruckTicketing.Domain.Entities.FacilityService;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.Product.Tasks;

public class ProductFacilityServiceSubstanceIndexUpdateTask : WorkflowTaskBase<BusinessContext<ProductEntity>>
{
    private readonly IProvider<Guid, FacilityServiceSubstanceIndexEntity> _facilityServiceSubstanceIndexProvider;

    public ProductFacilityServiceSubstanceIndexUpdateTask(IProvider<Guid, FacilityServiceSubstanceIndexEntity> facilityServiceSubstanceIndexProvider)
    {
        _facilityServiceSubstanceIndexProvider = facilityServiceSubstanceIndexProvider;
    }

    public override int RunOrder => 10;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<ProductEntity> context)
    {
        var currentProduct = context.Target;
        var existingIndices = context.Original != null ? (await _facilityServiceSubstanceIndexProvider.Get(index => index.TotalProductId == currentProduct.Id))?.ToList() ?? new() : new();
        if (!existingIndices.Any())
        {
            return true;
        }

        foreach (var index in existingIndices)
        {
            index.TotalProductName = currentProduct.Name;
            index.TotalProductCategories = currentProduct.Categories?.Clone().With(c => c.Key = Guid.NewGuid()) ?? new();
            index.UnitOfMeasure = currentProduct.UnitOfMeasure;
            await _facilityServiceSubstanceIndexProvider.Update(index, true);
        }

        return true;
    }

    public override Task<bool> ShouldRun(BusinessContext<ProductEntity> context)
    {
        return Task.FromResult(context.Operation == Operation.Update && IsProductUpdated(context));
    }

    private bool IsProductUpdated(BusinessContext<ProductEntity> context)
    {
        var originalProduct = context.Original == null
                                  ? string.Empty
                                  : new
                                  {
                                      context.Original.Name,
                                      context.Original.UnitOfMeasure,
                                      context.Original.Categories.Raw,
                                  }.ToJson();

        var targetProduct = context.Target == null
                                ? string.Empty
                                : new
                                {
                                    context.Target.Name,
                                    context.Target.UnitOfMeasure,
                                    context.Target.Categories.Raw,
                                }.ToJson();

        return string.CompareOrdinal(originalProduct, targetProduct) != 0;
    }
}
