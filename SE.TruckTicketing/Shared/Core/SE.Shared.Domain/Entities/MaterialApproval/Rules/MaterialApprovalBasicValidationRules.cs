using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using FluentValidation;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Rules;
using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Validation;

namespace SE.Shared.Domain.Entities.MaterialApproval.Rules;

public class MaterialApprovalBasicValidationRules : FluentValidationRule<MaterialApprovalEntity, TTErrorCodes>
{
    private static readonly Regex WlafNumberRegex = new(@"^WLAF-\w{5}$", RegexOptions.Compiled);
    private readonly IProvider<Guid, FacilityEntity> _facilityProvider;
    const string FacilityTypeKey = "FacilityType";
    const string ShowHazNonHazKey = "ShowHazNonHaz";

    public MaterialApprovalBasicValidationRules(IProvider<Guid, FacilityEntity> facilityProvider)
    {
        _facilityProvider = facilityProvider;
    }

    public override int RunOrder => 1100;

    public override async Task Run(BusinessContext<MaterialApprovalEntity> context, List<ValidationResult> errors)
    {
        var facility = await _facilityProvider.GetById(context.Target.FacilityId);

        if (facility != null)
        {
            context.ContextBag.TryAdd(FacilityTypeKey, facility.Type);
            context.ContextBag.TryAdd(ShowHazNonHazKey, facility.ShowHazNonHaz);
        }

        await base.Run(context, errors);

    }

    protected override void ConfigureRules(BusinessContext<MaterialApprovalEntity> context, InlineValidator<MaterialApprovalEntity> validator)
    {
        FacilityType facilityType = context.GetContextBagItemOrDefault(FacilityTypeKey, FacilityType.Unknown);
        bool showHazNonHaz = context.GetContextBagItemOrDefault(ShowHazNonHazKey, false);

        validator.RuleFor(materialApproval => materialApproval.HazardousNonhazardous)
                 .NotEqual(HazardousClassification.Undefined)
                 .When(_ => facilityType == FacilityType.Lf && showHazNonHaz)
                 .WithTridentErrorCode(TTErrorCodes.MaterialApproval_HazardousClassification)
                 .WithMessage("Landfill facilities must select either Hazardous or Non-Hazardous Hazard State");

        validator.RuleFor(materialApproval => materialApproval.FacilityId)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.MaterialApproval_Facility);

        validator.RuleFor(materialApproval => materialApproval.Description)
                 .MaximumLength(50)
                 .WithTridentErrorCode(TTErrorCodes.MaterialApproval_Description);

        validator.RuleFor(materialApproval => materialApproval.GeneratorId)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.MaterialApproval_Generator);

        validator.RuleFor(materialApproval => materialApproval.BillingCustomerId)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.MaterialApproval_BillingCustomer);

        validator.RuleFor(materialApproval => materialApproval.BillingCustomerContactId)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.MaterialApproval_BillingCustomerContact);

        validator.RuleFor(materialApproval => materialApproval.BillingCustomerContactAddress)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.MaterialApproval_BillingCustomerContactAddress);

        validator.RuleFor(materialApproval => materialApproval.FacilityServiceNumber)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.MaterialApproval_FacilityServiceNumber);

        validator.RuleFor(materialApproval => materialApproval.FacilityServiceName)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.MaterialApproval_FacilityServiceName);

        validator.RuleFor(materialApproval => materialApproval.AnalyticalExpiryDate)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.MaterialApproval_AnalyticalExpiryDate);

        validator.RuleFor(materialApproval => materialApproval.ScaleOperatorNotes)
                 .MaximumLength(100)
                 .WithTridentErrorCode(TTErrorCodes.MaterialApproval_Notes);

        validator.RuleFor(materialApproval => materialApproval.IsActive)
                 .NotEqual(false)
                 .When(materialApproval => materialApproval.EndDate == default);

        validator.RuleFor(materialApproval => materialApproval)
                 .Must(ma => ma.ApplicantSignatories.Count(x => x.ReceiveLoadSummary) <= 2)
                 .When(materialApproval => materialApproval.ApplicantSignatories != null && materialApproval.ApplicantSignatories.Any())
                 .WithMessage("Only up to 2 Signatories are allowed to be selected")
                 .WithTridentErrorCode(TTErrorCodes.MaterialApproval_Signatories_Constraint);

        validator.RuleFor(materialApproval => materialApproval.WLAFNumber)
                 .Matches(WlafNumberRegex)
                 .When(materialApproval => !string.IsNullOrEmpty(materialApproval.WLAFNumber))
                 .WithTridentErrorCode(TTErrorCodes.MaterialApproval_WlafNumber);
    }
}
