using System;
using System.Linq;
using System.Threading.Tasks;

using SE.TruckTicketing.Contracts.Constants.SourceLocations;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.SourceLocation.Tasks;

public class SurfaceLocationContractOperatorUpdateToAssociatedWells : WorkflowTaskBase<BusinessContext<SourceLocationEntity>>
{
    private readonly IProvider<Guid, SourceLocationEntity> _sourceLocationProvider;

    public SurfaceLocationContractOperatorUpdateToAssociatedWells(IProvider<Guid, SourceLocationEntity> sourceLocationProvider)
    {
        _sourceLocationProvider = sourceLocationProvider;
    }

    public override int RunOrder => 30;

    public override OperationStage Stage => OperationStage.AfterUpdate;

    public override async Task<bool> Run(BusinessContext<SourceLocationEntity> context)
    {
        //9937 - When Contract Operator is updated on Surface Location, update linked Well SourceLocation Contract Operator
        //Capture all the associated Well Source Locations
        var sourceLocationEntity = context.Target;
        var wellSourceLocationsAssociatedToSurfaceLocations = await _sourceLocationProvider.Get(sl => sl.AssociatedSourceLocationId == sourceLocationEntity.Id &&
                                                                                                      sl.SourceLocationTypeCategory == SourceLocationTypeCategory.Well);

        var associatedWellSourceLocationEntities = wellSourceLocationsAssociatedToSurfaceLocations?.ToList();

        if (wellSourceLocationsAssociatedToSurfaceLocations == null || !associatedWellSourceLocationEntities.Any())
        {
            return await Task.FromResult(true);
        }

        foreach (var sourceLocation in associatedWellSourceLocationEntities)
        {
            sourceLocation.ContractOperatorId = sourceLocationEntity.ContractOperatorId;
            sourceLocation.ContractOperatorName = sourceLocationEntity.ContractOperatorName;
            sourceLocation.ContractOperatorProductionAccountContactId = null;
            await _sourceLocationProvider.Update(sourceLocation);
        }

        return await Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<SourceLocationEntity> context)
    {
        return Task.FromResult(context.Target.SourceLocationTypeCategory == SourceLocationTypeCategory.Surface && context.Target.ContractOperatorId != default &&
                               context.Original.ContractOperatorId != context.Target.ContractOperatorId);
    }
}
