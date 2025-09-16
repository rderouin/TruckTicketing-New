using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.Invoices;
using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.Accounts;

public class CustomerCreditAndWatchlistStatusReferenceUpdateTask : WorkflowTaskBase<BusinessContext<AccountEntity>>
{
    private readonly IProvider<Guid, InvoiceEntity> _invoiceProvider;

    private readonly IProvider<Guid, LoadConfirmationEntity> _loadConfirmationProvider;

    private readonly IProvider<Guid, TruckTicketEntity> _truckTicketProvider;

    public CustomerCreditAndWatchlistStatusReferenceUpdateTask(IProvider<Guid, TruckTicketEntity> truckTicketProvider,
                                                               IProvider<Guid, InvoiceEntity> invoiceProvider,
                                                               IProvider<Guid, LoadConfirmationEntity> loadConfirmationProvider)
    {
        _truckTicketProvider = truckTicketProvider;
        _invoiceProvider = invoiceProvider;
        _loadConfirmationProvider = loadConfirmationProvider;
    }

    public override int RunOrder => 30;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<AccountEntity> context)
    {
        var invalidTruckTicketStatus = new List<TruckTicketStatus>
        {
            TruckTicketStatus.Void,
            TruckTicketStatus.Invoiced,
        };

        var invalidInvoiceStatus = new List<InvoiceStatus>
        {
            InvoiceStatus.Posted,
            InvoiceStatus.UnPosted,
            InvoiceStatus.AgingUnSent,
        };

        var invalidLoadConfirmationStatus = new List<LoadConfirmationStatus>
        {
            LoadConfirmationStatus.Void,
            LoadConfirmationStatus.Posted,
        };

        var targetAccountEntity = context.Target;
        var truckTicketEntities = await _truckTicketProvider.Get(x => x.BillingCustomerId == targetAccountEntity.Id && !invalidTruckTicketStatus.Contains(x.Status)); // PK - XP for TT by customer

        var ticketEntities = truckTicketEntities?.ToList();
        if (truckTicketEntities != null && ticketEntities.Any())
        {
            foreach (var truckTicketEntity in ticketEntities)
            {
                truckTicketEntity.CustomerCreditStatus = targetAccountEntity.CreditStatus;
                truckTicketEntity.CustomerWatchListStatus = targetAccountEntity.WatchListStatus;
                await _truckTicketProvider.Update(truckTicketEntity, true);
            }
        }

        var invoices = await _invoiceProvider.Get(x => x.CustomerId == targetAccountEntity.Id && !invalidInvoiceStatus.Contains(x.Status)); // PK - XP for invoices by customer
        var invoiceEntities = invoices?.ToList();
        if (invoiceEntities == null || !invoiceEntities.Any())
        {
            return await Task.FromResult(true);
        }

        foreach (var invoiceEntity in invoiceEntities)
        {
            invoiceEntity.CustomerCreditStatus = targetAccountEntity.CreditStatus;
            invoiceEntity.CustomerWatchListStatus = targetAccountEntity.WatchListStatus;
            await _invoiceProvider.Update(invoiceEntity, true);
            var loadConfirmationEntities = await _loadConfirmationProvider.Get(x => x.InvoiceId == invoiceEntity.Id && !invalidLoadConfirmationStatus.Contains(x.Status)); // PK - XP for LC by invoice
            var loadConfirmations = loadConfirmationEntities?.ToList();
            if (loadConfirmations == null || !loadConfirmations.Any())
            {
                continue;
            }

            foreach (var loadConfirmationEntity in loadConfirmations)
            {
                loadConfirmationEntity.CustomerCreditStatus = targetAccountEntity.CreditStatus;
                loadConfirmationEntity.CustomerWatchListStatus = targetAccountEntity.WatchListStatus;
                await _loadConfirmationProvider.Update(loadConfirmationEntity, true);
            }
        }

        return await Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<AccountEntity> context)
    {
        if (context.Operation != Operation.Update)
        {
            return Task.FromResult(false);
        }

        var isCreditOrWatchlistStatusUpdated = context.Original.CreditStatus != context.Target.CreditStatus || context.Original.WatchListStatus != context.Target.WatchListStatus;
        return Task.FromResult(context.Target.AccountTypes.List.Contains(AccountTypes.Customer) && isCreditOrWatchlistStatusUpdated);
    }
}
