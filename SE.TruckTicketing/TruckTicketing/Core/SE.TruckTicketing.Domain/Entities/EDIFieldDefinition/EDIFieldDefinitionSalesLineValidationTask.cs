using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.BillingConfiguration.Tasks;
using SE.Shared.Domain.Entities.EDIFieldDefinition;
using SE.Shared.Domain.Entities.SalesLine;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.EDIFieldDefinition;

public class EDIFieldDefinitionSalesLineValidationTask : WorkflowTaskBase<BusinessContext<EDIFieldDefinitionEntity>>
{
    public const string ResultKey = nameof(EDIFieldDefinitionSalesLineValidationTask) + nameof(ResultKey);

    private readonly IProvider<Guid, SalesLineEntity> _salesLineProvider;

    public EDIFieldDefinitionSalesLineValidationTask(IProvider<Guid, SalesLineEntity> salesLineProvider)
    {
        _salesLineProvider = salesLineProvider;
    }

    public override int RunOrder => 30;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<EDIFieldDefinitionEntity> context)
    {
        if (context.Target is null)
        {
            return false;
        }

        var ediFieldDefinition = context.Target;
        var ediFieldDefinitions = context.ContextBag[nameof(EdiDefinitionDataLoaderTask.EdiDefinitionsKey)] as List<EDIFieldDefinitionEntity>;
        ediFieldDefinitions!.Add(ediFieldDefinition);

        var salesLines = (await _salesLineProvider.Get(sl => sl.CustomerId == ediFieldDefinition.CustomerId &&
                                                             (sl.Status == SalesLineStatus.Preview ||
                                                              sl.Status == SalesLineStatus.Exception ||
                                                              sl.Status == SalesLineStatus.Approved))).ToArray(); // PK - XP for SL by customer

        foreach (var salesLine in salesLines)
        {
            var ediDefinitionValueMap = (salesLine.EdiFieldValues ?? new()).ToDictionary(ediValue => ediValue.EDIFieldDefinitionId);
            salesLine.IsEdiValid = true;

            foreach (var definition in ediFieldDefinitions)
            {
                var error = definition.Validate(ediDefinitionValueMap);
                if (error is not null)
                {
                    salesLine.IsEdiValid = false;
                }
            }

            await _salesLineProvider.Update(salesLine, true);
        }

        return true;
    }

    public override Task<bool> ShouldRun(BusinessContext<EDIFieldDefinitionEntity> context)
    {
        return Task.FromResult(true);
    }
}
