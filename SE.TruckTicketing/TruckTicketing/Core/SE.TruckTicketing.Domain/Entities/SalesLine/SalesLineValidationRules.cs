using FluentValidation;

using SE.Shared.Domain;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Rules;
using SE.TruckTicketing.Contracts;

using Trident.Business;

namespace SE.TruckTicketing.Domain.Entities.SalesLine;

public class SalesLineValidationRules : FluentValidationRule<SalesLineEntity, TTErrorCodes>
{
    public override int RunOrder => 1;

    protected override void ConfigureRules(BusinessContext<SalesLineEntity> context, InlineValidator<SalesLineEntity> validator)
    {
    }
}
