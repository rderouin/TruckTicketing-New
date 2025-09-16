using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Entities.EDIFieldDefinition;
using SE.Shared.Domain.Entities.EDIFieldValue;
using SE.TruckTicketing.Contracts;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.BillingConfiguration.Tasks;

public class BillingConfigurationEDIFieldValueValidationCheckerTask : WorkflowTaskBase<BusinessContext<BillingConfigurationEntity>>
{
    public const string ResultKey = nameof(BillingConfigurationEDIFieldValueValidationCheckerTask) + nameof(ResultKey);

    private readonly IProvider<Guid, EDIFieldDefinitionEntity> _ediFieldDefinitionProvider;

    private readonly Dictionary<EDIFieldDefinitionEntity, TTErrorCodes> _ediFieldValueValidationCheck;

    public BillingConfigurationEDIFieldValueValidationCheckerTask(IProvider<Guid, EDIFieldDefinitionEntity> ediFieldDefinitionProvider)
    {
        _ediFieldDefinitionProvider = ediFieldDefinitionProvider;
        _ediFieldValueValidationCheck = new();
    }

    public override int RunOrder => 20;

    public override OperationStage Stage => OperationStage.BeforeInsert | OperationStage.BeforeUpdate;

    public override async Task<bool> Run(BusinessContext<BillingConfigurationEntity> context)
    {
        if (context.Target is null)
        {
            return false;
        }

        var billingConfiguration = context.Target;
        var ediFieldDefinitions = (await _ediFieldDefinitionProvider.Get(def => def.CustomerId == billingConfiguration.BillingCustomerAccountId))?.ToArray();
        billingConfiguration.IsEdiValid = true;

        if (ediFieldDefinitions != null)
        {
            var ediDefinitionValueMap = (billingConfiguration.EDIValueData ?? new()).ToDictionary(ediValue => ediValue.EDIFieldDefinitionId);
            foreach (var ediFieldDefinition in ediFieldDefinitions)
            {
                var error = ediFieldDefinition.Validate(ediDefinitionValueMap);
                if (error is not null)
                {
                    billingConfiguration.IsEdiValid = false;
                    _ediFieldValueValidationCheck.TryAdd(ediFieldDefinition, error.Value);
                }
            }
        }

        context.ContextBag?.TryAdd(ResultKey, _ediFieldValueValidationCheck);
        return true;
    }

    public override Task<bool> ShouldRun(BusinessContext<BillingConfigurationEntity> context)
    {
        return Task.FromResult(true);
    }
}

public static class EdiValidationExtensions
{
    public static TTErrorCodes? Validate(this EDIFieldDefinitionEntity fieldDefinition, Dictionary<Guid, EDIFieldValueEntity> ediDefinitionValueMap)
    {
        ediDefinitionValueMap.TryGetValue(fieldDefinition.Id, out var value);
        var valueString = value?.EDIFieldValueContent ?? string.Empty;
        if (fieldDefinition.IsRequired && !valueString.HasText())
        {
            return TTErrorCodes.EDIFieldDefinition_EDIFieldValue_Required;
        }

        try
        {
            if (fieldDefinition.ValidationRequired && !Regex.IsMatch(valueString, fieldDefinition.ValidationPattern))
            {
                return TTErrorCodes.EDIFieldDefinition_EDIFieldValue_ValidationFailed;
            }
        }
        catch (ArgumentException)
        {
            return TTErrorCodes.EDIFieldDefinition_EDIFieldValue_ValidationFailed;
        }

        return null;
    }
}
