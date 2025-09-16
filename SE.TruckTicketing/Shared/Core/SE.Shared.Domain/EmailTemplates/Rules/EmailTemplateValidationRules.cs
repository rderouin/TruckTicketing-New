using System;
using System.Collections.Generic;
using System.Linq;

using FluentValidation;

using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Rules;
using SE.TruckTicketing.Contracts;

using Trident.Business;

namespace SE.Shared.Domain.EmailTemplates.Rules;

public class EmailTemplateValidationRules : FluentValidationRule<EmailTemplateEntity, TTErrorCodes>
{
    private const string EmailRegex = @"^([\w-]+(?:\.[\w-]+)*)@((?:[\w-]+\.)*\w[\w-]{0,66})\.([a-z]{2,6}(?:\.[a-z]{2})?)$";

    public override int RunOrder => 300;

    protected override void ConfigureRules(BusinessContext<EmailTemplateEntity> context, InlineValidator<EmailTemplateEntity> validator)
    {
        validator.RuleFor(et => et.Name)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.EmailTemplate_Name_Required);

        validator.RuleFor(et => et.Name)
                 .MaximumLength(100)
                 .WithTridentErrorCode(TTErrorCodes.EmailTemplate_Name_Length);

        validator.RuleFor(et => et.EventId)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.EmailTemplate_EventId_Required);

        validator.RuleFor(et => et.Subject)
                 .MaximumLength(400)
                 .When(et => et.Subject.HasText())
                 .WithTridentErrorCode(TTErrorCodes.EmailTemplate_Subject_Length);

        validator.RuleFor(et => et.Body)
                 .MaximumLength(4000)
                 .When(et => et.Body.HasText())
                 .WithTridentErrorCode(TTErrorCodes.EmailTemplate_Body_Length);

        validator.RuleFor(et => et.CustomReplyEmail)
                 .Matches(EmailRegex)
                 .WithMessage("'Custom Reply Email' is not a valid email address")
                 .When(et => et.CustomReplyEmail.HasText())
                 .WithTridentErrorCode(TTErrorCodes.EmailTemplate_CustomReplyEmail_Invalid);

        validator.RuleFor(et => et.SplitCustomBccEmails)
                 .ForEach(email => email.Matches(EmailRegex))
                 .WithMessage("'Bcc Email List' contains an invalid email address")
                 .When(et => et.CustomBccEmails.HasText())
                 .WithTridentErrorCode(TTErrorCodes.EmailTemplate_CustomBccEmails_Invalid);

        validator.RuleFor(et => et.Name)
                 .Must((entity, _) => entity.HasUniqueName.GetValueOrDefault(true))
                 .WithMessage("A template already exists with the same 'Name'")
                 .WithTridentErrorCode(TTErrorCodes.EmailTemplate_Name_Unique);

        validator.RuleFor(et => et)
                 .Must(BeUnique)
                 .WithMessage("A template already exists with overlapping Facilities and Customers.")
                 .WithTridentErrorCode(TTErrorCodes.EmailTemplate_FacilitySiteIds_GloballyUnique);

        validator.RuleFor(et => et.SenderEmail)
                 .NotEmpty()
                 .When(template => template.UseCustomSenderEmail is true)
                 .WithMessage("Sender Email should not be empty when override sender is enabled.")
                 .WithTridentErrorCode(TTErrorCodes.EmailTemplate_OverrideSender_EmailRequired);

        validator.RuleFor(et => et.SenderEmail)
                 .Matches(EmailRegex)
                 .WithMessage("'Sender Email' is not a valid email address")
                 .When(template => template.SenderEmail.HasText())
                 .WithTridentErrorCode(TTErrorCodes.EmailTemplate_SenderEmail_ValidFormat);
    }

    public static bool BeUnique(EmailTemplateEntity entity)
    {
        var permutationsInSelf = GetFacilityAndCustomerPermutations(entity);
        var permutationsInSiblings = (entity.Siblings ?? Array.Empty<EmailTemplateEntity>()).SelectMany(GetFacilityAndCustomerPermutations).ToHashSet();

        var hasOverlaps = permutationsInSelf.Intersect(permutationsInSiblings).Any();
        return !hasOverlaps;
    }

    private static List<string> GetFacilityAndCustomerPermutations(EmailTemplateEntity emailTemplateEntity)
    {
        // safely get lists
        var facilitySiteIds = (emailTemplateEntity.FacilitySiteIds?.List ?? Enumerable.Empty<string>()).ToList();
        var customerIds = (emailTemplateEntity.AccountIds?.List ?? Enumerable.Empty<Guid>()).ToList();

        // no facilities, blank = global
        if (!facilitySiteIds.Any())
        {
            facilitySiteIds.Add(string.Empty);
        }

        // no customers, blank = global
        if (!customerIds.Any())
        {
            customerIds.Add(Guid.Empty);
        }

        // go over each of the records
        var permutations = new List<string>();
        foreach (var facilitySiteId in facilitySiteIds)
        {
            foreach (var customerId in customerIds)
            {
                permutations.Add($"{facilitySiteId}|{customerId}");
            }
        }

        // list of all permutations
        return permutations;
    }
}
