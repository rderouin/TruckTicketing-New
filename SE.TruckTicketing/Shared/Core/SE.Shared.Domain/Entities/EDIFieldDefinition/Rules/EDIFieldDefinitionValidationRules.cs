using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentValidation;

using SE.Shared.Domain.Rules;
using SE.TruckTicketing.Contracts;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Validation;

namespace SE.Shared.Domain.Entities.EDIFieldDefinition.Rules;

public class EDIFieldDefinitionValidationRules : FluentValidationRule<EDIFieldDefinitionEntity, TTErrorCodes>
{
    private const string EDIFieldNameUniqueConstraintChecker = nameof(EDIFieldNameUniqueConstraintChecker);

    private const string ResultKey = EDIFieldNameUniqueConstraintChecker;

    private readonly IProvider<Guid, EDIFieldDefinitionEntity> _provider;

    public EDIFieldDefinitionValidationRules(IProvider<Guid, EDIFieldDefinitionEntity> provider)
    {
        _provider = provider;
    }

    public override int RunOrder => 1200;

    public override async Task Run(BusinessContext<EDIFieldDefinitionEntity> context, List<ValidationResult> errors)
    {
        if (!string.IsNullOrWhiteSpace(context.Target.EDIFieldLookupId))
        {
            var isDuplicate = await _provider.Get(x => 
                                                x.Id != context.Target.Id && 
                                                x.EDIFieldLookupId == context.Target.EDIFieldLookupId &&
                                                x.CustomerId == context.Target.CustomerId);

            context.ContextBag.TryAdd(ResultKey, !isDuplicate.Any());
        }

        await base.Run(context, errors);
    }

    protected override void ConfigureRules(BusinessContext<EDIFieldDefinitionEntity> context, InlineValidator<EDIFieldDefinitionEntity> validator)
    {
        validator.RuleFor(ediDefinition => ediDefinition.EDIFieldLookupId)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.EDIFieldDefinitionEntity_FieldLookupRequired);

        validator.RuleFor(ediDefinition => ediDefinition.EDIFieldName)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.EDIFieldDefinitionEntity_FieldNameRequired);

        validator.RuleFor(ediDefinition => ediDefinition.ValidationPattern)
                 .NotEmpty()
                 .When(ediDefinition => ediDefinition.ValidationRequired)
                 .WithTridentErrorCode(TTErrorCodes.EDIFieldDefinitionEntity_ValidationPatternRequired);

        validator.RuleFor(ediDefinition => ediDefinition.ValidationErrorMessage)
                 .NotEmpty()
                 .When(ediDefinition => ediDefinition.ValidationRequired)
                 .WithTridentErrorCode(TTErrorCodes.EDIFieldDefinitionEntity_ValidationErrorMessageRequired);

        validator.RuleFor(ediDefinition => ediDefinition)
                 .Must(_ => BeUniqueEDIFieldName(context))
                 .WithMessage("EDI Field already exists.")
                 .WithState(new ValidationResultState<TTErrorCodes>(TTErrorCodes.EDIFieldDefinitionEntity_EDIFieldAlreadyExists, nameof(EDIFieldDefinitionEntity.EDIFieldLookupId)));
    }

    private static bool BeUniqueEDIFieldName(BusinessContext<EDIFieldDefinitionEntity> context)
    {
        return context.GetContextBagItemOrDefault(EDIFieldNameUniqueConstraintChecker, true);
    }
}
