using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentValidation;

using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Rules;
using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Validation;

namespace SE.Shared.Domain.Entities.Invoices.Rules;

public class InvoiceValidationRules : FluentValidationRule<InvoiceEntity, TTErrorCodes>
{
    private const string ActiveLoadConfirmationsOnInvoice = nameof(ActiveLoadConfirmationsOnInvoice);

    private const string ActiveSalesLinesOnInvoice = nameof(ActiveSalesLinesOnInvoice);

    private const string PendingSalesLinesRemoval = nameof(PendingSalesLinesRemoval);

    private const string ResultKey = ActiveLoadConfirmationsOnInvoice;

    private readonly IProvider<Guid, LoadConfirmationEntity> _loadConfirmationProvider;

    private readonly IProvider<Guid, SalesLineEntity> _salesLineEntityProvider;

    public InvoiceValidationRules(IProvider<Guid, LoadConfirmationEntity> loadConfirmationProvider, IProvider<Guid, SalesLineEntity> salesLineEntityProvider)
    {
        _loadConfirmationProvider = loadConfirmationProvider;
        _salesLineEntityProvider = salesLineEntityProvider;
    }

    public override int RunOrder => 1200;

    public override async Task Run(BusinessContext<InvoiceEntity> context, List<ValidationResult> errors)
    {
        if (context.Target.Status == InvoiceStatus.Void && context.Original.Status != InvoiceStatus.Void && context.Target.Status != context.Original.Status)
        {
            var inactiveLoadConfirmationStatus = new List<LoadConfirmationStatus>
            {
                LoadConfirmationStatus.Void,
            };

            var loadConfirmationEntities = await _loadConfirmationProvider.Get(x => x.InvoiceId == context.Target.Id && !inactiveLoadConfirmationStatus.Contains(x.Status));
            var isActiveLoadConfirmationExist = loadConfirmationEntities != null && loadConfirmationEntities.Any();
            context.ContextBag.TryAdd(ResultKey, isActiveLoadConfirmationExist);
        }

        if (context.Original != null)
        {
            if (context.Target.Status == InvoiceStatus.Posted && context.Original.Status != InvoiceStatus.Posted)
            {
                var activeSalesLineExist = await _salesLineEntityProvider.Exists(sl => sl.InvoiceId == context.Target.Id && sl.Status != SalesLineStatus.Void);
                context.ContextBag.TryAdd(ActiveSalesLinesOnInvoice, activeSalesLineExist);

                var pendingRemoval = await _salesLineEntityProvider.Exists(sl => (sl.InvoiceId == context.Target.Id || sl.HistoricalInvoiceId == context.Target.Id) && sl.AwaitingRemovalAcknowledgment == true);
                pendingRemoval = false;
                context.ContextBag.TryAdd(PendingSalesLinesRemoval, pendingRemoval);
            }
        }

        await base.Run(context, errors);
    }

    protected override void ConfigureRules(BusinessContext<InvoiceEntity> context, InlineValidator<InvoiceEntity> validator)
    {
        validator.RuleFor(e => e.Status)
                 .Must(_ => !context.GetContextBagItemOrDefault(ActiveLoadConfirmationsOnInvoice, false))
                 .When(e => e.Status == InvoiceStatus.Void)
                 .WithMessage("Active Load Confirmation(s) found while setting Invoice to Void!")
                 .WithState(new ValidationResultState<TTErrorCodes>(TTErrorCodes.InvoiceVoid_ActiveLoadConfirmations_Exist, nameof(InvoiceEntity.Status)));

        validator.RuleFor(e => e.Status)
                 .Must(_ => context.GetContextBagItemOrDefault(ActiveSalesLinesOnInvoice, true))
                 .When(e => e.Status == InvoiceStatus.Posted)
                 .WithMessage("No Active Sales Lines found while posting Invoice!")
                 .WithState(new ValidationResultState<TTErrorCodes>(TTErrorCodes.InvoicePosted_NoActiveSalesLines_Exist, nameof(InvoiceEntity.Status)));

        validator.RuleFor(e => e.Status)
                 .Must(_ => !context.GetContextBagItemOrDefault(PendingSalesLinesRemoval, false))
                 .When(e => e.Status == InvoiceStatus.Posted)
                 .WithMessage("There are sales lines that are waiting for acknowledgement from FO.")
                 .WithState(new ValidationResultState<TTErrorCodes>(TTErrorCodes.InvoicePosted_PendingSalesLines_Exist, nameof(InvoiceEntity.Status)));
    }
}
