using FluentValidation;

using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Rules;
using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;

namespace SE.Shared.Domain.Entities.SourceLocation.Rules;

public class UsSourceLocationValidationRules : FluentValidationRule<SourceLocationEntity, TTErrorCodes>
{
    public override int RunOrder => 20;

    protected override void ConfigureRules(BusinessContext<SourceLocationEntity> context, InlineValidator<SourceLocationEntity> validator)
    {
        if (context.Target.CountryCode != CountryCode.US)
        {
            return;
        }

        validator.RuleFor(sourceLocation => sourceLocation.SourceLocationName)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.SourceLocation_NameRequiredForUSLocations);

        validator.RuleFor(sourceLocation => sourceLocation)
                 .Must(_ => BeUnique(context))
                 .WithMessage("The 'Source Location Name' you have entered already exists for the selected 'Source Location Type'.")
                 .WithState(new ValidationResultState<TTErrorCodes>(TTErrorCodes.SourceLocation_NameMustBeUnique, nameof(SourceLocationEntity.SourceLocationName),
                                                                    nameof(SourceLocationEntity.SourceLocationTypeId)));

        validator.RuleFor(sourceLocation => sourceLocation.PlsNumber)
                 .NotEmpty()
                 .When(sourceLocation => sourceLocation!.SourceLocationType!.RequiresPlsNumber &&
                                         sourceLocation!.SourceLocationType!.IsPlsNumberVisible.HasValue &&
                                         sourceLocation!.SourceLocationType!.IsPlsNumberVisible.Value)
                 .WithTridentErrorCode(TTErrorCodes.SourceLocation_PlsNumberIsRequired);

        validator.RuleFor(sourceLocation => sourceLocation.ApiNumber)
                 .NotEmpty()
                 .When(sourceLocation => sourceLocation!.SourceLocationType!.RequiresApiNumber &&
                                         sourceLocation!.SourceLocationType!.IsApiNumberVisible.HasValue &&
                                         sourceLocation!.SourceLocationType!.IsApiNumberVisible.Value)
                 .WithTridentErrorCode(TTErrorCodes.SourceLocation_ApiNumberIsRequired);

        validator.RuleFor(sourceLocation => sourceLocation.WellFileNumber)
                 .NotEmpty()
                 .When(sourceLocation => sourceLocation!.SourceLocationType!.RequiresWellFileNumber &&
                                         sourceLocation!.SourceLocationType!.IsWellFileNumberVisible.HasValue &&
                                         sourceLocation!.SourceLocationType!.IsWellFileNumberVisible.Value)
                 .WithTridentErrorCode(TTErrorCodes.SourceLocation_WellFileNumberIsRequired);

        validator.RuleFor(sourceLocation => sourceLocation.CtbNumber)
                 .NotEmpty()
                 .When(sourceLocation => sourceLocation!.SourceLocationType!.RequiresCtbNumber &&
                                         sourceLocation!.SourceLocationType!.IsCtbNumberVisible.HasValue &&
                                         sourceLocation!.SourceLocationType!.IsCtbNumberVisible.Value)
                 .WithTridentErrorCode(TTErrorCodes.SourceLocation_CtbNumberIsRequired);

        validator.RuleFor(sourceLocation => sourceLocation.DownHoleType)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.SourceLocation_DownHoleTypeRequired);

        validator.RuleFor(sourceLocation => sourceLocation.DeliveryMethod)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.SourceLocation_DeliveryMethodRequired);

        validator.RuleFor(sourceLocation => sourceLocation.SourceLocationCode)
                 .NotEmpty()
                 .When(sourceLocation => sourceLocation.SourceLocationType.ShortFormCode.HasText())
                 .WithTridentErrorCode(TTErrorCodes.SourceLocation_SourceLocationCode_Required);
    }

    private static bool BeUnique(BusinessContext<SourceLocationEntity> context)
    {
        return context.Target.IsUnique;
    }
}
