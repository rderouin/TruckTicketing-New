using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Entities.ServiceType;
using SE.Shared.Domain.Product;

using Trident.Business;
using Trident.Contracts;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.FacilityService.Tasks;

public class FacilityServiceSubstanceIndexer : WorkflowTaskBase<BusinessContext<FacilityServiceEntity>>
{
    private readonly IManager<Guid, FacilityServiceSubstanceIndexEntity> _indexManager;

    private readonly IProvider<Guid, ProductEntity> _productProvider;

    private readonly IProvider<Guid, ServiceTypeEntity> _serviceTypeProvider;

    public FacilityServiceSubstanceIndexer(IManager<Guid, FacilityServiceSubstanceIndexEntity> indexManager,
                                           IProvider<Guid, ProductEntity> productProvider,
                                           IProvider<Guid, ServiceTypeEntity> serviceTypeProvider)
    {
        _productProvider = productProvider;
        _serviceTypeProvider = serviceTypeProvider;
        _indexManager = indexManager;
    }

    public override int RunOrder => 5;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<FacilityServiceEntity> context)
    {
        var facilityService = context.Target;
        var serviceType = await _serviceTypeProvider.GetById(facilityService.ServiceTypeId);
        var product = await _productProvider.GetById(facilityService.TotalItemProductId);
        if (product is null || serviceType is null)
        {
            return false;
        }

        var existingIndices = context.Original != null ? (await _indexManager.Get(index => index.FacilityServiceId == facilityService.Id))?.ToList() ?? new() : new();
        var existingSubstanceIds = new HashSet<Guid>(existingIndices.Select(index => index.SubstanceId));

        var newIndices = product.Substances.Where(substance => !existingSubstanceIds.Contains(substance.Id))
                                .Select(substance => new FacilityServiceSubstanceIndexEntity
                                 {
                                     Id = Guid.NewGuid(),
                                     FacilityId = facilityService.FacilityId,
                                     FacilityServiceId = facilityService.Id,
                                     FacilityServiceNumber = facilityService.FacilityServiceNumber,
                                     ServiceTypeId = facilityService.ServiceTypeId,
                                     ServiceTypeName = facilityService.ServiceTypeName,
                                     TotalProductId = product.Id,
                                     TotalProductName = product.Name,
                                     TotalProductCategories = product.Categories?.Clone().With(c => c.Key = Guid.NewGuid()) ?? new(),
                                     Substance = substance.SubstanceName,
                                     SubstanceId = substance.Id,
                                     WasteCode = substance.WasteCode,
                                     Stream = serviceType.Stream.ToString(),
                                     UnitOfMeasure = product.UnitOfMeasure,
                                     IndexVersion = FacilityServiceSubstanceIndexEntity.LatestIndexVersion,
                                 });

        var indices = existingIndices.Concat(newIndices).ToArray();
        var authorizedSubstances = context.Target.AuthorizedSubstances?.List ?? new();
        foreach (var index in indices)
        {
            index.IsAuthorized = context.Target.IsActive && authorizedSubstances.Contains(index.SubstanceId);
        }

        await _indexManager.BulkSave(indices);
        return true;
    }

    public override Task<bool> ShouldRun(BusinessContext<FacilityServiceEntity> context)
    {
        var shouldRun = context.Operation == Operation.Insert ||
                        (context.Operation == Operation.Update && (context.Target.IsActive != context.Original.IsActive ||
                                                                   context.Target.AuthorizedSubstances?.Raw != context.Original.AuthorizedSubstances?.Raw));

        return Task.FromResult(shouldRun);
    }
}
