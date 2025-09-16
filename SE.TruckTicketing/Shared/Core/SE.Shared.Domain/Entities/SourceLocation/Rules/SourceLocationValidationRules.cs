using System.Collections.Generic;
using System.Threading.Tasks;

using FluentValidation;

using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Rules;
using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Constants.SourceLocations;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Validation;

namespace SE.Shared.Domain.Entities.SourceLocation.Rules;

public class SourceLocationValidationRules : FluentValidationRule<SourceLocationEntity, TTErrorCodes>
{
    private const string SourceLocationCodeMaskPattern = nameof(SourceLocationCodeMaskPattern);

    public override int RunOrder => 10;

    public override async Task Run(BusinessContext<SourceLocationEntity> context, List<ValidationResult> errors)
    {
        if (context.Target.SourceLocationType is { Category: SourceLocationTypeCategory.Surface, CountryCode: CountryCode.CA, EnforceSourceLocationCodeMask: true } &&
            context.Target.SourceLocationType.SourceLocationCodeMask.HasText())
        {
            var sourceLocationCodePattern = context.Target.SourceLocationType.SourceLocationCodeMask?
                                                   .Replace("#", "[0-9]")
                                                   .Replace("@", "[A-z]")
                                                   .Replace("*", ".")
                                                   .Replace("/", "\\/");

            context.ContextBag.TryAdd(SourceLocationCodeMaskPattern, string.Concat(new[] { "^", sourceLocationCodePattern, "$" }));
        }

        await base.Run(context, errors);
    }

    protected override void ConfigureRules(BusinessContext<SourceLocationEntity> context, InlineValidator<SourceLocationEntity> validator)
    {
        validator.RuleFor(sourceLocation => sourceLocation.Identifier)
                 .NotEmpty()
                 .When(sourceLocation => sourceLocation.CountryCode == CountryCode.CA)
                 .WithTridentErrorCode(TTErrorCodes.SourceLocation_IdentifierRequired);

        validator.RuleFor(sourceLocation => sourceLocation.FormattedIdentifier)
                 .Matches(sourceLocation => sourceLocation.FormattedIdentifierPattern)
                 .When(sourceLocation => sourceLocation.CountryCode == CountryCode.CA && sourceLocation.FormattedIdentifierPattern.HasText())
                 .WithTridentErrorCode(TTErrorCodes.SourceLocation_FormattedIdentifierInvalid);

        validator.RuleFor(sourceLocation => sourceLocation.SourceLocationCode)
                 .Matches(GetSourceLocationCodePattern(context))
                 .When(sourceLocation => sourceLocation.CountryCode == CountryCode.CA &&
                                         sourceLocation.SourceLocationTypeCategory == SourceLocationTypeCategory.Surface &&
                                         sourceLocation.SourceLocationType is { EnforceSourceLocationCodeMask: true } &&
                                         (sourceLocation.SourceLocationCode ?? string.Empty).Length != 20)
                 .WithTridentErrorCode(TTErrorCodes.SourceLocation_SourceLocationCodeInvalid);

        validator.RuleFor(sourceLocation => sourceLocation.GeneratorId)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.SourceLocation_GeneratorRequired);

        validator.RuleFor(sourceLocation => sourceLocation.GeneratorStartDate)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.SourceLocation_GeneratorStartDateRequired);

        validator.RuleFor(sourceLocation => sourceLocation.GeneratorStartDate)
                 .Must(_ => BeAfterLastGeneratorStartDate(context))
                 .WithMessage("The 'Start Date' for the new Generator must be after the old Generator's 'Start Date'")
                 .When(_ => TheGeneratorIsChanging(context))
                 .WithTridentErrorCode(TTErrorCodes.SourceLocation_GeneratorStartDateMustBeLaterThanPreviousStartDate);

        validator.RuleFor(sourceLocation => sourceLocation.ContractOperatorId)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.SourceLocation_ContractOperatorRequired);

        validator.RuleFor(sourceLocation => sourceLocation.LicenseNumber)
                 .MinimumLength(5)
                 .When(sourceLocation => sourceLocation.LicenseNumber.HasText() && sourceLocation.CountryCode == CountryCode.CA)
                 .WithTridentErrorCode(TTErrorCodes.SourceLocation_LicenseNumberInvalid);

        validator.RuleFor(sourceLocation => sourceLocation.LicenseNumber)
                 .MaximumLength(20)
                 .When(sourceLocation => sourceLocation.LicenseNumber.HasText() && sourceLocation.CountryCode == CountryCode.CA)
                 .WithTridentErrorCode(TTErrorCodes.SourceLocation_LicenseNumberInvalid);

        validator.RuleFor(sourceLocation => sourceLocation.ProvinceOrState)
                 .NotEmpty()
                 .When(sourceLocation => sourceLocation.CountryCode == CountryCode.CA)
                 .WithTridentErrorCode(TTErrorCodes.SourceLocationType_ProvinceRequired);

        validator.RuleFor(sourceLocation => sourceLocation)
                 .Must(sourceLocation => sourceLocation.IsUnique)
                 .When(sourceLocation => sourceLocation.CountryCode == CountryCode.CA)
                 .WithMessage("The 'Formatted Identifier' you have entered already exists for the selected 'Source Location Type'.")
                 .WithState(new ValidationResultState<TTErrorCodes>(TTErrorCodes.SourceLocation_IdentifierMustBeUnique, nameof(SourceLocationEntity.FormattedIdentifier),
                                                                    nameof(SourceLocationEntity.SourceLocationTypeId)));
    }

    private static bool BeAfterLastGeneratorStartDate(BusinessContext<SourceLocationEntity> context)
    {
        return context.Target.GeneratorStartDate > context.Original.GeneratorStartDate;
    }

    private static bool TheGeneratorIsChanging(BusinessContext<SourceLocationEntity> context)
    {
        return context.Original != null &&
               context.Target.GeneratorId != context.Original.GeneratorId;
    }

    private static string GetSourceLocationCodePattern(BusinessContext<SourceLocationEntity> context)
    {
        return context.GetContextBagItemOrDefault(SourceLocationCodeMaskPattern, string.Empty);
    }
}
