using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.IdentityModel.Tokens;

using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.Sequences;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.SalesLine.Tasks;

public class SalesLineNumberGeneratorTask : WorkflowTaskBase<BusinessContext<SalesLineEntity>>
{
    private readonly ISequenceNumberGenerator _sequenceNumberGenerator;

    public SalesLineNumberGeneratorTask(ISequenceNumberGenerator sequenceNumberGenerator, IProvider<Guid, FacilityEntity> facilityProvider)
    {
        _sequenceNumberGenerator = sequenceNumberGenerator;
    }

    public override int RunOrder => 10;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<SalesLineEntity> context)
    {
        if (context.Target is null)
        {
            return false;
        }

        var salesLine = context.Target;
        if (!salesLine.FacilitySiteId.IsNullOrEmpty())
        {
            salesLine.SalesLineNumber = await _sequenceNumberGenerator.GenerateSequenceNumbers(SequenceTypes.SalesLine, salesLine.FacilitySiteId, 1).FirstAsync();
        }

        return true;
    }

    public override Task<bool> ShouldRun(BusinessContext<SalesLineEntity> context)
    {
        var shouldRun = string.IsNullOrEmpty(context.Target.SalesLineNumber);
        return Task.FromResult(shouldRun);
    }
}
