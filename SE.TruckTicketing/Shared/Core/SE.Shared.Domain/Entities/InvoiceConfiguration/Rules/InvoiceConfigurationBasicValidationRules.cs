using System.Linq;

using FluentValidation;

using SE.Shared.Domain.Entities.InvoiceConfiguration.Tasks;
using SE.Shared.Domain.Rules;
using SE.TruckTicketing.Contracts;

using Trident.Business;

namespace SE.Shared.Domain.Entities.InvoiceConfiguration.Rules;

public class InvoiceConfigurationBasicValidationRules : FluentValidationRule<InvoiceConfigurationEntity, TTErrorCodes>
{
    private const string InvalidBillingConfigurationChecker = nameof(InvalidBillingConfigurationChecker);

    public override int RunOrder => 10;

    protected override void ConfigureRules(BusinessContext<InvoiceConfigurationEntity> context, InlineValidator<InvoiceConfigurationEntity> validator)
    {
        validator.RuleFor(config => config.Name)
                 .MaximumLength(150)
                 .WithTridentErrorCode(TTErrorCodes.InvoiceConfiguration_Name_Length);

        validator.RuleFor(config => config.Name)
                 .NotNull()
                 .WithTridentErrorCode(TTErrorCodes.InvoiceConfiguration_Name_Required);

        validator.RuleFor(config => config.Description)
                 .MaximumLength(500)
                 .WithTridentErrorCode(TTErrorCodes.InvoiceConfiguration_Description_Length);

        validator.RuleFor(config => config.CustomerId)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.InvoiceConfiguration_Customer_Required);

        validator.RuleFor(config => config)
                 .Must(config => config.Facilities != null && config.FacilityCode != null && config.Facilities.List.Any() && config.FacilityCode.List.Any())
                 .When(config => !config.AllFacilities)
                 .WithMessage("Facility should be selected if 'Select All' for facility not applied.")
                 .WithTridentErrorCode(TTErrorCodes.InvoiceConfiguration_Facility_Required);

        validator.RuleFor(config => config)
                 .Must(_ => !MultipleCatchAllForSameCustomer(context))
                 .WithMessage("Default Invoice Configuration with Catch-All already exist for selected customer.")
                 .WithState(new ValidationResultState<TTErrorCodes>(TTErrorCodes.InvoiceConfiguration_ExistingCatchAll_For_SelectedCustomer, nameof(InvoiceConfigurationEntity.CatchAll)));
    }

    private static bool MultipleCatchAllForSameCustomer(BusinessContext<InvoiceConfigurationEntity> context)
    {
        return context.GetContextBagItemOrDefault(InvoiceConfigurationSingleCatchAllForCustomerCheckerTask.ResultKey, false);
    }
}
