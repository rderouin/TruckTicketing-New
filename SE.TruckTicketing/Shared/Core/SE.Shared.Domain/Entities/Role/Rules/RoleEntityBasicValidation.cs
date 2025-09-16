using FluentValidation;

using SE.Shared.Domain.Rules;
using SE.TruckTicketing.Contracts;

using Trident.Business;

namespace SE.Shared.Domain.Entities.Role.Rules;

public class RoleEntityBasicValidation : FluentValidationRule<RoleEntity, TTErrorCodes>
{
    public override int RunOrder => 100;

    protected override void ConfigureRules(BusinessContext<RoleEntity> context, InlineValidator<RoleEntity> validator)
    {
        validator.RuleFor(role => role.Name)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.Role_Name);
    }
}
