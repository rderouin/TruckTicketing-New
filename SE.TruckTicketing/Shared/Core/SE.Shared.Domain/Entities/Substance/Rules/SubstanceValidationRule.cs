using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using FluentValidation;

using SE.Shared.Domain.Rules;
using SE.TruckTicketing.Contracts;

using Trident.Business;
using Trident.Contracts.Configuration;
using Trident.Data.Contracts;
using Trident.Validation;

namespace SE.Shared.Domain.Entities.Substance.Rules;

public class SubstanceValidationRule : FluentValidationRule<SubstanceEntity, TTErrorCodes>
{
    private const string DuplicateSubstanceConstraintChecker = nameof(DuplicateSubstanceConstraintChecker);

    private readonly IAppSettings _appSettings;

    private readonly IProvider<Guid, SubstanceEntity> _substanceProvider;

    public SubstanceValidationRule(IAppSettings appSettings, IProvider<Guid, SubstanceEntity> substanceProvider)
    {
        _appSettings = appSettings;
        _substanceProvider = substanceProvider;
    }

    public override int RunOrder => 100;

    public override async Task Run(BusinessContext<SubstanceEntity> context, List<ValidationResult> errors)
    {
        if (context.Target != null && context.Operation == Operation.Insert)
        {
            var existingSubstance = await _substanceProvider.GetById(context.Target.Id);
            if (existingSubstance != null)
            {
                context.ContextBag.TryAdd(DuplicateSubstanceConstraintChecker, true);
            }
        }
    }

    protected override void ConfigureRules(BusinessContext<SubstanceEntity> context, InlineValidator<SubstanceEntity> validator)
    {
        validator.RuleFor(substance => substance)
                 .Must(_ => !GetDuplicateChecker(context))
                 .WithMessage("Substance with Substance Name and Waste Code combination already exist.")
                 .WithState(new ValidationResultState<TTErrorCodes>(TTErrorCodes.Duplicate_Substance_Already_Exist, nameof(SubstanceEntity.SubstanceName)));
    }

    private static bool GetDuplicateChecker(BusinessContext<SubstanceEntity> context)
    {
        return context.GetContextBagItemOrDefault(DuplicateSubstanceConstraintChecker, false);
    }
}
