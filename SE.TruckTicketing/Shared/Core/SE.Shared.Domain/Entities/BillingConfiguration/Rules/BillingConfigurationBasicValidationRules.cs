using System;
using System.Collections.Generic;
using System.Linq;

using FluentValidation;

using SE.Shared.Common.Lookups;
using SE.Shared.Common.Utilities;
using SE.Shared.Domain.Entities.BillingConfiguration.Tasks;
using SE.Shared.Domain.Entities.EDIFieldDefinition;
using SE.Shared.Domain.Rules;
using SE.TruckTicketing.Contracts;

using Trident.Business;

namespace SE.Shared.Domain.Entities.BillingConfiguration.Rules;

public class BillingConfigurationBasicValidationRules : FluentValidationRule<BillingConfigurationEntity, TTErrorCodes>
{
    public override int RunOrder => 10;

    protected override void ConfigureRules(BusinessContext<BillingConfigurationEntity> context, InlineValidator<BillingConfigurationEntity> validator)
    {
        validator.RuleFor(config => config.StartDate)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.BillingConfiguration_StartDate_Required);

        validator.RuleFor(config => config.Name)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.BillingConfiguration_Name_Required);

        validator.RuleFor(config => config.EndDate)
                 .GreaterThan(config => config.StartDate)
                 .When(config => config.StartDate != default)
                 .WithTridentErrorCode(TTErrorCodes.BillingConfiguration_EndDate_GreaterThan_StartDate);

        validator.RuleFor(config => config)
                 .Must(HaveValidMatchCriteria)
                 .When(config => config.MatchCriteria.Count > 0)
                 .WithMessage("MatchCriteria contains invalid date range(s)")
                 .WithTridentErrorCode(TTErrorCodes.BillingConfiguration_MatchCriteria_Dates_WithIn);

        validator.RuleFor(config => config)
                 .Must(billingConfig => billingConfig.Signatories != null && billingConfig.Signatories.Any())
                 .When(config => config.IsSignatureRequired && !config.IsDefaultConfiguration)
                 .WithMessage("Signatories for signature approval email should be added.")
                 .WithTridentErrorCode(TTErrorCodes.BillingConfiguration_Signatories_Required);

        validator.RuleForEach(config => config.MatchCriteria)
                 .ChildRules(predicate => predicate.RuleFor(x => x.WellClassificationState)
                                                   .Must(x => x != MatchPredicateValueState.Unspecified)
                                                   .WithMessage("MatchCriteria WellClassificationState should be set to valid value")
                                                   .WithTridentErrorCode(TTErrorCodes.BillingConfiguration_MatchCriteria_Invalid_WellClassificationState))
                 .When(config => config.MatchCriteria is { Count: > 0 });

        validator.RuleForEach(config => config.MatchCriteria)
                 .ChildRules(predicate => predicate.RuleFor(x => x.SourceLocationValueState)
                                                   .Must(x => x != MatchPredicateValueState.Unspecified)
                                                   .WithMessage("MatchCriteria SourceLocationValueState should be set to valid value")
                                                   .WithTridentErrorCode(TTErrorCodes.BillingConfiguration_MatchCriteria_Invalid_SourceLocationValueState))
                 .When(config => config.MatchCriteria is { Count: > 0 });

        validator.RuleForEach(config => config.MatchCriteria)
                 .ChildRules(predicate => predicate.RuleFor(x => x.SubstanceValueState)
                                                   .Must(x => x != MatchPredicateValueState.Unspecified)
                                                   .WithMessage("MatchCriteria SubstanceValueState should be set to valid value")
                                                   .WithTridentErrorCode(TTErrorCodes.BillingConfiguration_MatchCriteria_Invalid_SubstanceValueState))
                 .When(config => config.MatchCriteria is { Count: > 0 });

        validator.RuleForEach(config => config.MatchCriteria)
                 .ChildRules(predicate => predicate.RuleFor(x => x.StreamValueState)
                                                   .Must(x => x != MatchPredicateValueState.Unspecified)
                                                   .WithMessage("MatchCriteria StreamValueState should be set to valid value")
                                                   .WithTridentErrorCode(TTErrorCodes.BillingConfiguration_MatchCriteria_Invalid_StreamValueState))
                 .When(config => config.MatchCriteria is { Count: > 0 });

        validator.RuleForEach(config => config.MatchCriteria)
                 .ChildRules(predicate => predicate.RuleFor(x => x.ServiceTypeValueState)
                                                   .Must(x => x != MatchPredicateValueState.Unspecified)
                                                   .WithMessage("MatchCriteria ServiceTypeValueState should be set to valid value")
                                                   .WithTridentErrorCode(TTErrorCodes.BillingConfiguration_MatchCriteria_Invalid_ServiceTypeValueState))
                 .When(config => config.MatchCriteria is { Count: > 0 });

        validator.RuleFor(config => config.IncludeForAutomation)
                 .Must(x => x == false)
                 .When(config => !config.IsDefaultConfiguration && (config.MatchCriteria == null || !config.MatchCriteria.Any()))
                 .WithMessage("Billing Configuration can't set to Include For Automation with no match predicate added")
                 .WithTridentErrorCode(TTErrorCodes.BillingConfiguration_IncludeForAutomation_SetToFalse_NoMatchPredicate);

        var ediFieldValueValidation = EDIFieldValueValidation(context);

        if (ediFieldValueValidation.Count > 0)
        {
            foreach (var ediFieldValueValidationResult in ediFieldValueValidation)
            {
                validator.RuleFor(config => config)
                         .Must(_ => ediFieldValueValidation.Count == 0)
                         .WithMessage($"'{ediFieldValueValidationResult.Key.EDIFieldName}' {ediFieldValueValidationResult.Value.ToString().GetEnumDescription<TTErrorCodes>()}")
                         .WithState(new ValidationResultState<TTErrorCodes>(ediFieldValueValidationResult.Value,
                                                                            ediFieldValueValidationResult.Key.EDIFieldName));
            }
        }

        var duplicateMatchPredicates = DuplicateMatchPredicates(context);
        if (duplicateMatchPredicates.Any())
        {
            var billingConfigurations = new List<string>();
            duplicateMatchPredicates.ForEach(x => billingConfigurations.Add(x.Item2.Name));
            validator.RuleFor(config => config)
                     .Must(_ => duplicateMatchPredicates.Count == 0)
                     .WithMessage($"Duplicate match predicate exists in BillingConfiguration: {string.Join(", ", billingConfigurations)}");
        }
    }

    private bool HaveValidMatchCriteria(BillingConfigurationEntity billingConfigurationEntity)
    {
        var validator = MatchPredicateValidator(billingConfigurationEntity.StartDate, billingConfigurationEntity.EndDate);
        return billingConfigurationEntity.MatchCriteria
                                         .TrueForAll(predicate => validator.Validate(predicate).IsValid);
    }

    private InlineValidator<MatchPredicateEntity> MatchPredicateValidator(DateTimeOffset? billingStartDate, DateTimeOffset? billingEndDate)
    {
        var validator = new InlineValidator<MatchPredicateEntity>();
        validator.RuleFor(config => config.EndDate)
                 .GreaterThan(config => config.StartDate)
                 .When(config => config.StartDate != default);

        validator.RuleFor(config => config.EndDate)
                 .Empty()
                 .When(config => config.StartDate == default);

        validator.RuleFor(config => config.StartDate)
                 .GreaterThanOrEqualTo(billingStartDate.GetValueOrDefault())
                 .When(config => config.StartDate != default && billingStartDate != default);

        validator.RuleFor(config => config.EndDate)
                 .LessThanOrEqualTo(billingEndDate.GetValueOrDefault())
                 .When(config => config.EndDate != default && billingEndDate != default);

        return validator;
    }

    private static Dictionary<EDIFieldDefinitionEntity, TTErrorCodes> EDIFieldValueValidation(BusinessContext<BillingConfigurationEntity> context)
    {
        return context.GetContextBagItemOrDefault<Dictionary<EDIFieldDefinitionEntity, TTErrorCodes>>(BillingConfigurationEDIFieldValueValidationCheckerTask.ResultKey, new());
    }

    private static bool UniqueMatchPredicate(BusinessContext<BillingConfigurationEntity> context)
    {
        return context.GetContextBagItemOrDefault(BillingConfigurationWorkflowContextBagKeys.MatchPredicateHashIsUnique, true);
    }

    private static List<(MatchPredicateEntity, BillingConfigurationEntity)> DuplicateMatchPredicates(BusinessContext<BillingConfigurationEntity> context)
    {
        return context.GetContextBagItemOrDefault(BillingConfigurationWorkflowContextBagKeys.DuplicateMatchPredicates, new List<(MatchPredicateEntity, BillingConfigurationEntity)>());
    }
}
