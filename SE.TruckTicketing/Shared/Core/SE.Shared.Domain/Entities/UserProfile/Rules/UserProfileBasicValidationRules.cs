using Trident.Business;
using Trident.Validation;

namespace SE.Shared.Domain.Entities.UserProfile.Rules;

public class UserProfileBasicValidationRules : PropertyExpressionValidationRule<BusinessContext<UserProfileEntity>, UserProfileEntity, ErrorCodes>
{
    public override int RunOrder => 100;

    protected override void ConfigureRules(BusinessContext<UserProfileEntity> context)
    {
        AddRule(nameof(UserProfileEntity.DisplayName), x => !string.IsNullOrWhiteSpace(x.DisplayName),
                errorCode: ErrorCodes.UserProfile_DisplayNameRequired);

        AddRule(nameof(UserProfileEntity.Email), x => !string.IsNullOrWhiteSpace(x.Email),
                errorCode: ErrorCodes.UserProfile_EmailRequired);

        AddRule(nameof(UserProfileEntity.ExternalAuthId), x => !string.IsNullOrWhiteSpace(x.ExternalAuthId),
                errorCode: ErrorCodes.UserProfile_EmailRequired);

        AddRule(nameof(UserProfileEntity.Roles), x => x.Roles != null,
                errorCode: ErrorCodes.UserProfile_RolesRequired);

        AddRule(nameof(UserProfileEntity.SpecificFacilityAccessAssignments), x => x.SpecificFacilityAccessAssignments != null,
                errorCode: ErrorCodes.UserProfile_SpecifcFacilityAccessListRequired);
    }
}
