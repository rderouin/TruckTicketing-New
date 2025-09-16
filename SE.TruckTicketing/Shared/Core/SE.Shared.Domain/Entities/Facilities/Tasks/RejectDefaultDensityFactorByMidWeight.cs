using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Trident.Business;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.Facilities.Tasks;

public class RejectDefaultDensityFactorByMidWeight : WorkflowTaskBase<BusinessContext<FacilityEntity>>
{
    public override int RunOrder => 20;

    public override OperationStage Stage => OperationStage.BeforeInsert | OperationStage.BeforeUpdate;

    public override async Task<bool> Run(BusinessContext<FacilityEntity> context)
    {
        var validDefaultDensities = context.Target.MidWeightConversionParameters.Where(x => x.IsEnabled && (x.EndDate >= DateTimeOffset.Now.Date || x.EndDate == null)).ToList();
        var overlappingDefaultDensities = validDefaultDensities.Select(p => new
        {
            SourceLocation = p.SourceLocationId,
            FacilityServices = p.FacilityServiceId,
        }).GroupBy(p => p.SourceLocation).Where(group => group.Count() > 1);

        var defaultDensities = overlappingDefaultDensities.ToList();
        if (!defaultDensities.Any())
        {
            return await Task.FromResult(true);
        }

        foreach (var group in defaultDensities)
        {
            bool isOverlap = false;
            var sourceLocationId = group.Select(x => x.SourceLocation.GetValueOrDefault()).First();
            var facilityServices = group.Select(x => x.FacilityServices?.List).ToList();
            var noFacilityServices = facilityServices.Where(x => x == null).ToList();
            facilityServices = facilityServices.Where(x => x != null).OrderBy(x => x.Count).ToList();
            if (noFacilityServices.Any())
            {
                //Without FacilityServices
                var overlappingDensityRecordsWithoutFacilityService = validDefaultDensities.Where(x => sourceLocationId == Guid.Empty ? x.SourceLocationId == null : x.SourceLocationId == sourceLocationId)
                                                                                           .Where(x => x.FacilityServiceId == null || !x.FacilityServiceId.List.Any()).ToList();
                overlappingDensityRecordsWithoutFacilityService = FindOverlappingIntervals(overlappingDensityRecordsWithoutFacilityService);
                isOverlap = overlappingDensityRecordsWithoutFacilityService.Any();
            }

            //With FacilityServices
            var previousList = new List<Guid>();
            foreach (var facilityServiceList in facilityServices)
            {
                var intersection = previousList.Intersect(facilityServiceList).ToList();
                if (!intersection.Any())
                {

                    var overlappingDensityRecordsWithFacilityService = validDefaultDensities.Where(x => sourceLocationId == Guid.Empty ? x.SourceLocationId == null : x.SourceLocationId == sourceLocationId)
                                                                                            .Where(x => x.FacilityServiceId != null && x.FacilityServiceId.List.Any() && x.FacilityServiceId.List.Intersect(facilityServiceList).Any()).ToList();
                    overlappingDensityRecordsWithFacilityService = FindOverlappingIntervals(overlappingDensityRecordsWithFacilityService);
                    isOverlap = isOverlap || overlappingDensityRecordsWithFacilityService.Any();
                }

                previousList = previousList.Union(facilityServiceList).ToList();
            }

            if (isOverlap)
            {
                context.ContextBag.TryAdd(FacilityWorkflowContextBagKeys.OverlappingFacilityServiceForDefaultDensitiesByMidWeight, true);
            }
        }

        return await Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<FacilityEntity> context)
    {
        return Task.FromResult(context.Target is { MidWeightConversionParameters: { } } && context.Target.MidWeightConversionParameters.Any());
    }

    private List<PreSetDensityConversionParamsEntity> FindOverlappingIntervals(List<PreSetDensityConversionParamsEntity> densities)
    {
        var overlappingInterval = new List<PreSetDensityConversionParamsEntity>();
        var densityRecords = new List<PreSetDensityConversionParamsEntity>();
        densityRecords.AddRange(densities);

        densityRecords = densityRecords.OrderBy(x => x.StartDate).ToList();
        for (var i = 0; i < densityRecords.Count - 1; i++)
        {
            var endDate = densityRecords[i].EndDate == null
                       || densityRecords[i].EndDate == DateTimeOffset.MinValue || densityRecords[i].EndDate == default
                              ? DateTimeOffset.MaxValue
                              : densityRecords[i].EndDate;

            if (endDate > densityRecords[i + 1].StartDate)
            {
                overlappingInterval.Add(densityRecords[i]);
                overlappingInterval.Add(densityRecords[i + 1]);
            }
        }

        return overlappingInterval;
    }
}
