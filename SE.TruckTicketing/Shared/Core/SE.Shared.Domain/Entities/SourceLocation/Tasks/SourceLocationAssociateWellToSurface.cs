using System;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Contracts.Constants.SourceLocations;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.SourceLocation.Tasks;

public class SourceLocationAssociateWellToSurface : WorkflowTaskBase<BusinessContext<SourceLocationEntity>>
{
    private readonly IProvider<Guid, SourceLocationEntity> _sourceLocationProvider;

    public SourceLocationAssociateWellToSurface(IProvider<Guid, SourceLocationEntity> sourceLocationProvider)
    {
        _sourceLocationProvider = sourceLocationProvider;
    }

    public override int RunOrder => 25;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<SourceLocationEntity> context)
    {
        //Associate Well SourceLocation to Surface Location based on SourceLocationCode setup on Well SourceLocation
        //Associate Surface Location to Well SourceLocation on SourceLocationCode setup on Surface SourceLocation

        switch (context.Target.SourceLocationTypeCategory)
        {
            case SourceLocationTypeCategory.Well:
                await AssociateWellToSurface(context);
                break;
            case SourceLocationTypeCategory.Surface:
                await AssociateSurfaceToWell(context);
                break;
        }

        return await Task.FromResult(true);
    }

    private async Task AssociateWellToSurface(BusinessContext<SourceLocationEntity> context)
    {
        //Current SourceLocation is of type Well
        var surfaceLocations =
            await _sourceLocationProvider.Get(sl => sl.SourceLocationCode == context.Target.SourceLocationCode && sl.SourceLocationTypeCategory == SourceLocationTypeCategory.Surface);

        var surfaceLocationEntities = surfaceLocations?.ToList();
        if (surfaceLocations != null && surfaceLocationEntities.Any())
        {
            context.Target.AssociatedSourceLocationId = surfaceLocationEntities.First().Id;
            context.Target.AssociatedSourceLocationCode = surfaceLocationEntities.First().SourceLocationCode;
        }
    }

    private async Task AssociateSurfaceToWell(BusinessContext<SourceLocationEntity> context)
    {
        //Current SourceLocation is of type Surface
        var wellSourceLocations =
            await _sourceLocationProvider.Get(sl => sl.SourceLocationCode == context.Target.SourceLocationCode && sl.SourceLocationTypeCategory == SourceLocationTypeCategory.Well);

        var wellLocationEntities = wellSourceLocations?.ToList();
        if (wellSourceLocations != null && wellLocationEntities.Any())
        {
            foreach (var wellSourceLocation in wellLocationEntities)
            {
                wellSourceLocation.AssociatedSourceLocationId = context.Target.Id;
                wellSourceLocation.AssociatedSourceLocationCode = context.Target.SourceLocationCode;
                await _sourceLocationProvider.Update(wellSourceLocation);
            }
        }
    }

    public override Task<bool> ShouldRun(BusinessContext<SourceLocationEntity> context)
    {
        var isSourceLocationCodeUpdated = context.Operation == Operation.Insert || (context.Operation == Operation.Update && context.Original.SourceLocationCode != context.Target.SourceLocationCode);
        return Task.FromResult(context.Target.SourceLocationTypeCategory != SourceLocationTypeCategory.Undefined && context.Target.SourceLocationCode.HasText() && isSourceLocationCodeUpdated);
    }
}
