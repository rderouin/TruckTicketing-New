using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.EDIFieldDefinition;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.EDIFieldDefinition;

public class EdiDefinitionDataLoaderTask : WorkflowTaskBase<BusinessContext<EDIFieldDefinitionEntity>>
{
    public const string EdiDefinitionsKey = nameof(EdiDefinitionsKey);

    private readonly IProvider<Guid, EDIFieldDefinitionEntity> _ediFieldDefinitionProvider;

    public EdiDefinitionDataLoaderTask(IProvider<Guid, EDIFieldDefinitionEntity> ediFieldDefinitionProvider)
    {
        _ediFieldDefinitionProvider = ediFieldDefinitionProvider;
    }

    public override int RunOrder => 1;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<EDIFieldDefinitionEntity> context)
    {
        var customerId = context.Target.CustomerId;
        var ediFieldDefinitions = (await _ediFieldDefinitionProvider.Get(def => def.CustomerId == customerId && def.Id != context.Target.Id))?.ToList() ?? new();
        context.ContextBag.TryAdd(EdiDefinitionsKey, ediFieldDefinitions);

        return true;
    }

    public override Task<bool> ShouldRun(BusinessContext<EDIFieldDefinitionEntity> context)
    {
        return Task.FromResult(true);
    }
}
