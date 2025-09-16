using System;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.MaterialApproval;
using SE.Shared.Domain.Entities.SourceLocation;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.SourceLocation;

public class SourceLocationIdentifierChangeMaterialApprovalReevaluateTask : WorkflowTaskBase<BusinessContext<SourceLocationEntity>>
{
    private readonly IProvider<Guid, MaterialApprovalEntity> _materialApprovalProvider;

    public SourceLocationIdentifierChangeMaterialApprovalReevaluateTask(IProvider<Guid, MaterialApprovalEntity> materialApprovalProvider)
    {
        _materialApprovalProvider = materialApprovalProvider;
    }

    public override int RunOrder => 60;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<SourceLocationEntity> context)
    {
        var materialApproval = await _materialApprovalProvider.Get(material => material.SourceLocationId == context.Target.Id);

        foreach (var material in materialApproval)
        {
            material.GeneratorId = context.Target.GeneratorId;
            material.GeneratorName = context.Target.GeneratorName;
            material.SourceLocationId = context.Target.Id;
            material.SourceLocation = context.Target.SourceLocationName;
            material.SourceLocationFormattedIdentifier = context.Target.FormattedIdentifier;
            material.SourceLocationUnformattedIdentifier = context.Target.Identifier;

            await _materialApprovalProvider.Update(material, true);
        }

        return true;
    }

    public override Task<bool> ShouldRun(BusinessContext<SourceLocationEntity> context)
    {
        return Task.FromResult(context.Original != null && context.Operation == Operation.Update
                                                        && (context.Target.GeneratorId != context.Original.GeneratorId
                                                           || context.Target.Identifier != context.Original.Identifier
                                                           || context.Target.SourceLocationName != context.Original.SourceLocationName)
                                                           );
    }
}
