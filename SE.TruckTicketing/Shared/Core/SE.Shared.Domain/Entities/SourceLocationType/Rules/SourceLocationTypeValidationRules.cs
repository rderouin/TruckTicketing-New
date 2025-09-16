using FluentValidation;
using SE.Shared.Domain.Entities.SourceLocationType.Tasks;
using SE.Shared.Domain.Rules;
using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;

namespace SE.Shared.Domain.Entities.SourceLocationType.Rules;

public class SourceLocationTypeValidationRules : FluentValidationRule<SourceLocationTypeEntity, TTErrorCodes>
{
    public override int RunOrder => 10;

    protected override void ConfigureRules(BusinessContext<SourceLocationTypeEntity> context, InlineValidator<SourceLocationTypeEntity> validator)
    {
        validator.RuleFor(sourceLocationType => sourceLocationType.Name)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.SourceLocationType_NameRequired);

        validator.RuleFor(sourceLocationType => sourceLocationType.CountryCode)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.SourceLocationType_CountryCodeRequired);

        validator.RuleFor(sourceLocationType => sourceLocationType.Category)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.SourceLocationType_CategoryRequired);

        validator.RuleFor(sourceLocationType => sourceLocationType.ShortFormCode)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.SourceLocationType_ShortFormCodeRequired)
                 .When(sourceLocationType => sourceLocationType?.CountryCode == CountryCode.CA);

        validator.RuleFor(sourceLocationType => sourceLocationType.DefaultDownHoleType)
                 .NotEmpty()
                 .When(sourceLocationType => sourceLocationType?.CountryCode == CountryCode.US)
                 .WithTridentErrorCode(TTErrorCodes.SourceLocationType_DownHoleTypeDefaultRequired);

        validator.RuleFor(sourceLocationType => sourceLocationType.DefaultDeliveryMethod)
                 .NotEmpty()
                 .When(sourceLocationType => sourceLocationType?.CountryCode == CountryCode.US)
                 .WithTridentErrorCode(TTErrorCodes.SourceLocationType_DeliveryMethodDefaultRequired);

        validator.RuleFor(sourceLocationType => sourceLocationType)
                 .Must(_ => BeUniqueInEachCountry(context))
                 .WithMessage("The Name you have entered already exists for the selected Country. Change the Country or enter a new Name that does not already exist for the selected Country.")
                 .WithState(new ValidationResultState<TTErrorCodes>(TTErrorCodes.SourceLocationType_NameMustBeUniqueInEachCountry, nameof(SourceLocationTypeEntity.CountryCode),
                                                                    nameof(SourceLocationTypeEntity.Name)));
    }

    private static bool BeUniqueInEachCountry(BusinessContext<SourceLocationTypeEntity> context)
    {
        return context.GetContextBagItemOrDefault(SourceLocationTypeUniqueConstraintCheckerTask.ResultKey, true);
    }
}
