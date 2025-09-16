using System.Collections.Generic;
using System.Threading.Tasks;

using Trident.Business;
using Trident.Validation;

namespace SE.Shared.Domain.Entities.UserProfile.Rules;

public class UserProfileComplexValidationRules : ValidationRuleBase<BusinessContext<UserProfileEntity>>
{
    public override int RunOrder => 200;

    public override Task Run(BusinessContext<UserProfileEntity> context, List<ValidationResult> errors)
    {
        if (!context.GetContextBagItemOrDefault(UserProfileBusinessContextBagKeys.UserProfileExternalAuthIdIsUnique, true))
        {
            errors.Add(new ValidationResult<ErrorCodes>("ExternalAuthID must be unique.", ErrorCodes.UserProfile_ExternalAuthIdUnique));
        }

        if (context.Original != null && context.Original.ExternalAuthId != context.Target.ExternalAuthId)
        {
            errors.Add(new ValidationResult<ErrorCodes>("ExternalAuthID cannot be changed.", ErrorCodes.UserProfile_ExternalAuthIdImmutable));
        }

        return Task.CompletedTask;
    }
}
