using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.BillingConfiguration.Tasks;
using SE.Shared.Domain.Entities.EDIFieldDefinition;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.EDIFieldDefinition;

public class EDIFieldDefinitionBillingConfigValidationTask : WorkflowTaskBase<BusinessContext<EDIFieldDefinitionEntity>>
{
    private readonly IProvider<Guid, BillingConfigurationEntity> _billingConfigurationProvider;

    public EDIFieldDefinitionBillingConfigValidationTask(IProvider<Guid, BillingConfigurationEntity> billingConfigurationProvider)
    {
        _billingConfigurationProvider = billingConfigurationProvider;
    }

    public override int RunOrder => 10;

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

        var billingConfigurations = (await _billingConfigurationProvider.Get(bc => bc.BillingCustomerAccountId == ediFieldDefinition.CustomerId)).ToList();

        foreach (var billingConfiguration in billingConfigurations)
        {
            var ediValues = billingConfiguration.EDIValueData ?? new();
            var ediDefinitionValueMap = ediValues.ToDictionary(ediValue => ediValue.EDIFieldDefinitionId);
            billingConfiguration.IsEdiValid = true;

            foreach (var definition in ediFieldDefinitions)
            {
                var error = definition.Validate(ediDefinitionValueMap);
                if (error is not null)
                {
                    billingConfiguration.IsEdiValid = false;
                }
            }

            await _billingConfigurationProvider.Update(billingConfiguration, true);
        }

        return true;
    }

    public override Task<bool> ShouldRun(BusinessContext<EDIFieldDefinitionEntity> context)
    {
        return Task.FromResult(true);
    }
}
