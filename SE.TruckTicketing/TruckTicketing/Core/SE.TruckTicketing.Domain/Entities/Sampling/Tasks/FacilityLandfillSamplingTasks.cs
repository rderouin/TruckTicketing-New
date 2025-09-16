using System;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.Facilities;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.Sampling.Strategies;

using Trident.Business;
using Trident.Contracts;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.Sampling.Tasks;

public class FacilityCreateLandfillSamplingTask : WorkflowTaskBase<BusinessContext<FacilityEntity>>
{
    private readonly IProvider<Guid, LandfillSamplingRuleEntity> _landfillSamplingRuleProvider;
    private readonly IManager<Guid, LandfillSamplingEntity> _landfillSamplingManager;

    public FacilityCreateLandfillSamplingTask(
        IProvider<Guid, LandfillSamplingRuleEntity> landfillSamplingRuleProvider,
        IManager<Guid, LandfillSamplingEntity> landfillSamplingManager)
    {
        _landfillSamplingRuleProvider = landfillSamplingRuleProvider;
        _landfillSamplingManager = landfillSamplingManager;
    }

    public override int RunOrder { get; } = 1;
    public override OperationStage Stage { get; } = OperationStage.AfterInsert;

    public override async Task<bool> Run(BusinessContext<FacilityEntity> context)
    {
        var facilitySamplingRules = await _landfillSamplingRuleProvider.Get(r =>
                                                                          r.CountryCode == context.Target.CountryCode &&
                                                                          (r.Province == StateProvince.Unspecified || r.Province == context.Target.Province));

        foreach (var samplingRule in facilitySamplingRules)
        {
            var sampling = BuildSampling(context.Target, samplingRule);
            await _landfillSamplingManager.Insert(sampling);
        }

        return true;
    }

    public override Task<bool> ShouldRun(BusinessContext<FacilityEntity> context)
    {
        return Task.FromResult(true);
    }

    private static LandfillSamplingEntity BuildSampling(FacilityEntity facility, LandfillSamplingRuleEntity samplingRule)
    {
        var sampleTrackingStrategy = SampleTrackingStrategy.GetTrackingStrategy(samplingRule.SamplingRuleType); 
        
        return new()
        {
            Id = Guid.NewGuid(),
            Threshold = samplingRule.Threshold,
            Value = sampleTrackingStrategy.InitialThresholdValue(),
            SamplingRuleType = samplingRule.SamplingRuleType,
            FacilityId = facility.Id,
            WarningThreshold = samplingRule.WarningThreshold,
            SamplingRuleId = samplingRule.Id,
        };
    }
}

public class FacilityDeleteLandfillSamplingTask : WorkflowTaskBase<BusinessContext<FacilityEntity>>
{
    private readonly IProvider<Guid, LandfillSamplingEntity> _landfillSamplingProvider;

    public FacilityDeleteLandfillSamplingTask(
        IProvider<Guid, LandfillSamplingEntity> landfillSamplingProvider)
    {
        _landfillSamplingProvider = landfillSamplingProvider;
    }

    public override int RunOrder { get; } = 1;
    public override OperationStage Stage { get; } = OperationStage.AfterDelete;

    public override async Task<bool> Run(BusinessContext<FacilityEntity> context)
    {
        var samplingsToDelete = await _landfillSamplingProvider.Get(s => s.FacilityId == context.Original.Id);

        foreach (var sampling in samplingsToDelete)
        {
            await _landfillSamplingProvider.Delete(sampling);
        }

        return true;
    }

    public override Task<bool> ShouldRun(BusinessContext<FacilityEntity> context)
    {
        return Task.FromResult(true);
    }
}
