using System;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.ServiceType;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.FacilityService.Tasks;

public class FacilityServiceDataLoaderTask : WorkflowTaskBase<BusinessContext<FacilityServiceEntity>>
{
    private readonly IProvider<Guid, FacilityEntity> _facilityProvider;

    private readonly IProvider<Guid, FacilityServiceEntity> _facilityServiceProvider;

    private readonly IProvider<Guid, ServiceTypeEntity> _serviceTypeProvider;

    public FacilityServiceDataLoaderTask(IProvider<Guid, FacilityEntity> facilityProvider,
                                         IProvider<Guid, FacilityServiceEntity> facilityServiceProvider,
                                         IProvider<Guid, ServiceTypeEntity> serviceTypeProvider)
    {
        _facilityProvider = facilityProvider;
        _facilityServiceProvider = facilityServiceProvider;
        _serviceTypeProvider = serviceTypeProvider;
    }

    public override int RunOrder => 5;

    public override OperationStage Stage => OperationStage.BeforeInsert | OperationStage.BeforeUpdate;

    public override async Task<bool> Run(BusinessContext<FacilityServiceEntity> context)
    {
        if (context.Target is null)
        {
            return false;
        }

        await EnrichFacilityData(context.Target);
        await EnrichServiceTypeData(context.Target);
        await EnrichUniqueFlag(context.Target);
        return true;
    }

    private async Task EnrichFacilityData(FacilityServiceEntity entity)
    {
        var facility = await _facilityProvider.GetById(entity.FacilityId);
        entity.SiteId = facility?.SiteId;
        entity.FacilityServiceNumber = $"{entity.SiteId}-{entity.ServiceNumber}";
    }

    private async Task EnrichUniqueFlag(FacilityServiceEntity entity)
    {
        var duplicates = await _facilityServiceProvider.Get(facilityService => facilityService.FacilityId == entity.FacilityId &&
                                                                               facilityService.ServiceNumber == entity.ServiceNumber &&
                                                                               facilityService.Id != entity.Id);

        entity.IsUnique = !duplicates.Any();
    }

    private async Task EnrichServiceTypeData(FacilityServiceEntity entity)
    {
        var serviceType = await _serviceTypeProvider.GetById(entity.ServiceTypeId);
        entity.OilItem = serviceType?.OilItemName;
        entity.WaterItem = serviceType?.WaterItemName;
        entity.SolidsItem = serviceType?.SolidItemName;
        entity.TotalItem = serviceType?.TotalItemName;
        entity.ServiceTypeName = serviceType?.Name;
        entity.TotalItemProductId = serviceType?.TotalItemId;
    }

    public override Task<bool> ShouldRun(BusinessContext<FacilityServiceEntity> context)
    {
        return Task.FromResult(true);
    }
}
