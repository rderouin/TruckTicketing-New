using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentValidation;

using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Rules;
using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Validation;

namespace SE.Shared.Domain.Entities.LoadConfirmation.Rules;

public class LoadConfirmationValidationRules : FluentValidationRule<LoadConfirmationEntity, TTErrorCodes>
{
    private const string ActiveSalesLinesOnLoadConfirmation = nameof(ActiveSalesLinesOnLoadConfirmation);

    private const string ResultKey = ActiveSalesLinesOnLoadConfirmation;

    private readonly IProvider<Guid, SalesLineEntity> _salesLineEntityProvider;

    public LoadConfirmationValidationRules(IProvider<Guid, SalesLineEntity> salesLineEntityProvider)
    {
        _salesLineEntityProvider = salesLineEntityProvider;
    }

    public override int RunOrder => 1200;

    public override async Task Run(BusinessContext<LoadConfirmationEntity> context, List<ValidationResult> errors)
    {
        if (context.Target.Status == LoadConfirmationStatus.Void && context.Original.Status != LoadConfirmationStatus.Void && context.Target.Status != context.Original.Status)
        {
            var inactiveSalesLineStatus = new List<SalesLineStatus>
            {
                SalesLineStatus.Void,
            };

            var salesLineEntities = await _salesLineEntityProvider.Get(x => x.LoadConfirmationId == context.Target.Id && !inactiveSalesLineStatus.Contains(x.Status));
            var isActiveSalesLinesExist = salesLineEntities != null && salesLineEntities.Any();
            context.ContextBag.TryAdd(ResultKey, isActiveSalesLinesExist);
        }

        await base.Run(context, errors);
    }

    protected override void ConfigureRules(BusinessContext<LoadConfirmationEntity> context, InlineValidator<LoadConfirmationEntity> validator)
    {
        validator.RuleFor(lc => lc.Status)
                 .Must(_ => !ActiveSalesLinesPresentOnVoid(context))
                 .When(x => x.Status == LoadConfirmationStatus.Void)
                 .WithMessage("Active SalesLines found while setting Load Confirmation to Void!")
                 .WithState(new ValidationResultState<TTErrorCodes>(TTErrorCodes.LoadConfirmationVoid_ActiveSalesLines_Exist, nameof(LoadConfirmationEntity.Status)));
    }

    private static bool ActiveSalesLinesPresentOnVoid(BusinessContext<LoadConfirmationEntity> context)
    {
        return context.GetContextBagItemOrDefault(ActiveSalesLinesOnLoadConfirmation, false);
    }
}
