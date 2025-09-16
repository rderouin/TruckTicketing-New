using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.SourceLocationType.Tasks;

public class SourceLocationTypeUniqueConstraintCheckerTask : WorkflowTaskBase<BusinessContext<SourceLocationTypeEntity>>
{
    public const string ResultKey = nameof(SourceLocationTypeUniqueConstraintCheckerTask) + nameof(ResultKey);

    private readonly IProvider<Guid, SourceLocationTypeEntity> _provider;

    public SourceLocationTypeUniqueConstraintCheckerTask(IProvider<Guid, SourceLocationTypeEntity> provider)
    {
        _provider = provider;
    }

    public override int RunOrder => 10;

    public override OperationStage Stage => OperationStage.BeforeInsert | OperationStage.BeforeUpdate;

    public override async Task<bool> Run(BusinessContext<SourceLocationTypeEntity> context)
    {
        var isDuplicate = await _provider.Get(type =>
                                                  type.Id != context.Target.Id &&
                                                  type.CountryCode == context.Target.CountryCode &&
                                                  type.Name == context.Target.Name);

        context.ContextBag.TryAdd(ResultKey, !isDuplicate.Any());

        return true;
    }

    public override Task<bool> ShouldRun(BusinessContext<SourceLocationTypeEntity> context)
    {
        return Task.FromResult(context.Target.CountryCode != CountryCode.Undefined &&
                               !string.IsNullOrEmpty(context.Target.Name) &&
                               context.Original?.Name != context.Target.Name);
    }
}
