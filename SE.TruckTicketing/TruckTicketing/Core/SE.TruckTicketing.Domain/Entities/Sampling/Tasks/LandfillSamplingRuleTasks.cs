using System;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.Facilities;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.Sampling.Strategies;

using Trident.Workflow;
using Trident.Business;
using Trident.Contracts;
using Trident.Data.Contracts;

namespace SE.TruckTicketing.Domain.Entities.Sampling.Tasks;

public class LandfillSamplingCreateTask : WorkflowTaskBase<BusinessContext<LandfillSamplingRuleEntity>>
{
    private readonly IProvider<Guid, FacilityEntity> _facilityProvider;
    private readonly IManager<Guid, LandfillSamplingEntity> _landfillSamplingManager;
    

    public LandfillSamplingCreateTask(
        IProvider<Guid, FacilityEntity> facilityProvider,
        IManager<Guid, LandfillSamplingEntity> landfillSamplingManager
        )
    {
        _facilityProvider = facilityProvider;
        _landfillSamplingManager = landfillSamplingManager;
    }

    public override int RunOrder { get; } = 1;
    public override OperationStage Stage { get; } = OperationStage.AfterInsert;

    public override async Task<bool> Run(BusinessContext<LandfillSamplingRuleEntity> context)
    {
        var sampleTrackingStrategy = SampleTrackingStrategy.GetTrackingStrategy(context.Target.SamplingRuleType);
        
        var facilities = await _facilityProvider.Get(facility =>
                                                   facility.CountryCode == context.Target.CountryCode &&
                                                   (context.Target.Province == StateProvince.Unspecified || facility.Province == context.Target.Province));

        var newSamplings = facilities.Select(facility => new LandfillSamplingEntity()
                                   {
                                       Id = Guid.NewGuid(),
                                       SamplingRuleId = context.Target.Id,
                                       Threshold = context.Target.Threshold,
                                       WarningThreshold = context.Target.WarningThreshold,
                                       Value = sampleTrackingStrategy.InitialThresholdValue(),
                                       SamplingRuleType = context.Target.SamplingRuleType,
                                       FacilityId = facility.Id,
                                       ProductNumberFilter = context.Target.ProductNumberFilter,
                                       WellClassificationFilters = context.Target.WellClassificationFilters,
                                   })
                                  .ToList();

        await _landfillSamplingManager.BulkSave(newSamplings);
        return await Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<LandfillSamplingRuleEntity> context)
    {
        return Task.FromResult(true);
    }
}

public class LandfillSamplingDeleteTask : WorkflowTaskBase<BusinessContext<LandfillSamplingRuleEntity>>
{
    private readonly IManager<Guid, LandfillSamplingEntity> _landfillSamplingManager;

    public LandfillSamplingDeleteTask(IManager<Guid, LandfillSamplingEntity> landfillSamplingManager)
    {
        _landfillSamplingManager = landfillSamplingManager;
    }

    public override int RunOrder { get; } = 1;
    public override OperationStage Stage { get; } = OperationStage.AfterDelete;

    public override async Task<bool> Run(BusinessContext<LandfillSamplingRuleEntity> context)
    {
        var samplingsToDelete = await _landfillSamplingManager.Get(sampling => sampling.SamplingRuleId == context.Target.Id);
        await _landfillSamplingManager.BulkDelete(samplingsToDelete);
        
        return await Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<LandfillSamplingRuleEntity> context)
    {
        return Task.FromResult(true);
    }
}
