using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using SE.Shared.Domain.Entities.Substance;

using Trident.Business;
using Trident.Contracts;
using Trident.Workflow;

namespace SE.Shared.Domain.Product.Tasks;

public class ProductSubstanceReferenceWorkflowTask : WorkflowTaskBase<BusinessContext<ProductEntity>>
{
    private readonly ILogger<ProductSubstanceReferenceWorkflowTask> _logger;

    private readonly IManager<Guid, SubstanceEntity> _substanceManager;

    public ProductSubstanceReferenceWorkflowTask(IManager<Guid, SubstanceEntity> substanceManager, ILogger<ProductSubstanceReferenceWorkflowTask> logger)
    {
        _substanceManager = substanceManager;
        _logger = logger;
    }

    // Ensure this is one of the first work-flow tasks to run.
    public override int RunOrder => 5;

    public override OperationStage Stage => OperationStage.BeforeInsert | OperationStage.BeforeUpdate;

    public override async Task<bool> Run(BusinessContext<ProductEntity> context)
    {
        //Handle SubstanceEntity create & Id update for receiving Substances for Product
        var product = context.Target;
        if (product.Substances == null || !product.Substances.Any())
        {
            return true;
        }

        product.Substances.ForEach(substance => substance.WasteCode ??= string.Empty);
        var substanceWasteCodeCombos = product.Substances?.Select(s => $"{s.SubstanceName}{s.WasteCode}").ToList();
        var existingMatchingSubstances =
            (await _substanceManager.Get(s => substanceWasteCodeCombos.Contains(s.SubstanceName + s.WasteCode))).ToDictionary(s => $"{s.SubstanceName}{s.WasteCode}");

        List<SubstanceEntity> productVarianceEntities = new();
        var deferredSubstanceChanges = false;
        foreach (var substance in product.Substances!)
        {
            if (!existingMatchingSubstances.TryGetValue($"{substance.SubstanceName}{substance.WasteCode}", out var substanceEntity))
            {
                deferredSubstanceChanges = true;

                substanceEntity = new()
                {
                    SubstanceName = substance.SubstanceName,
                    WasteCode = substance.WasteCode,
                };

                substanceEntity.ComputeSubstanceId();

                _logger.LogWarning("Could not find existing substance <{SubstanceName}> and waste code <{WasteCode}>. Inserting new with Id {SubstanceId}",
                                   substanceEntity.SubstanceName, substanceEntity.WasteCode, substanceEntity.Id);

                await _substanceManager.Insert(substanceEntity, true);
            }

            productVarianceEntities.Add(new()
            {
                Id = substanceEntity.Id,
                SubstanceName = substanceEntity.SubstanceName,
                WasteCode = substanceEntity.WasteCode,
            });
        }

        if (deferredSubstanceChanges)
        {
            await _substanceManager.SaveDeferred();
        }

        product.Substances = productVarianceEntities?.Select(variant => new ProductSubstanceEntity
        {
            Id = variant.Id,
            SubstanceName = variant.SubstanceName,
            WasteCode = variant.WasteCode,
        }).ToList();

        return true;
    }

    public override Task<bool> ShouldRun(BusinessContext<ProductEntity> context)
    {
        return Task.FromResult(true);
    }
}
