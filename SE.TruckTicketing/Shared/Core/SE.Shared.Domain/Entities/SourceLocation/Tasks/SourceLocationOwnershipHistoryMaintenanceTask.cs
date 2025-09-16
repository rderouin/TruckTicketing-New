using System.Threading.Tasks;

using Trident.Business;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.SourceLocation.Tasks;

public class SourceLocationOwnershipHistoryMaintenanceTask : WorkflowTaskBase<BusinessContext<SourceLocationEntity>>
{
    public override int RunOrder => 10;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override Task<bool> Run(BusinessContext<SourceLocationEntity> context)
    {
        var previousOwners = context.Target.OwnershipHistory ??= new();

        previousOwners.Add(new()
        {
            StartDate = context.Original.GeneratorStartDate,
            EndDate = context.Target.GeneratorStartDate.AddMinutes(-1),
            GeneratorId = context.Original.GeneratorId,
            GeneratorAccountNumber = context.Original.GeneratorAccountNumber,
            GeneratorName = context.Original.GeneratorName,
            ProductionAccountContactId = context.Original.GeneratorProductionAccountContactId,
            ProductionAccountContactName = context.Original.GeneratorProductionAccountContactName,
        });

        return Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<SourceLocationEntity> context)
    {
        return Task.FromResult(context.Operation == Operation.Update && context.Target.GeneratorId != context.Original.GeneratorId);
    }
}
