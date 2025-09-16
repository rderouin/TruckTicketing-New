using System;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.SourceLocation;

using Trident.Business;
using Trident.Contracts;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.Account.Tasks;

public class UpdateSourceLocationStatusForInActiveGeneratorTask : WorkflowTaskBase<BusinessContext<AccountEntity>>
{
    private readonly IManager<Guid, SourceLocationEntity> _sourceLocationManager;

    public UpdateSourceLocationStatusForInActiveGeneratorTask(IManager<Guid, SourceLocationEntity> sourceLocationManager)
    {
        _sourceLocationManager = sourceLocationManager;
    }

    public override int RunOrder => 20;

    public override OperationStage Stage => OperationStage.AfterUpdate;

    public override async Task<bool> Run(BusinessContext<AccountEntity> context)
    {
        var associatedSourceLocationForGenerator = await _sourceLocationManager.Get(sl => sl.GeneratorId == context.Target.Id);
        var sourceLocationForGenerator = associatedSourceLocationForGenerator?.ToList();
        if (associatedSourceLocationForGenerator == null || !sourceLocationForGenerator.ToList().Any())
        {
            return await Task.FromResult(true);
        }

        sourceLocationForGenerator.ToList().ForEach(sl => sl.IsActive = context.Target.IsAccountActive);
        foreach (var updatedSourceLocation in sourceLocationForGenerator)
        {
            await _sourceLocationManager.Save(updatedSourceLocation);
        }

        return await Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<AccountEntity> context)
    {
        return Task.FromResult(context.Target is { AccountTypes: { } } && context.Target.AccountTypes.List.Any() && context.Target.AccountTypes.List.Contains(AccountTypes.Generator.ToString())
                            && context.Original.IsAccountActive != context.Target.IsAccountActive);
    }
}
