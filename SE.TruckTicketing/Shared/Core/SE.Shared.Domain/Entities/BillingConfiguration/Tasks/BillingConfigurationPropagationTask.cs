using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Autofac;

using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Contracts.Api;
using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.BillingConfiguration.Tasks;

public class BillingConfigurationPropagationTask : WorkflowTaskBase<BusinessContext<BillingConfigurationEntity>>
{
    private static readonly List<TruckTicketStatus> ApplicableStatuses = new()
    {
        TruckTicketStatus.Stub,
        TruckTicketStatus.Open,
        TruckTicketStatus.Approved,
    };

    private readonly IProvider<Guid, AccountEntity> _accountProvider;

    private readonly ILifetimeScope _lifetimeScope;

    private readonly ILog _log;

    private readonly IProvider<Guid, TruckTicketEntity> _truckTicketProvider;

    public BillingConfigurationPropagationTask(IProvider<Guid, AccountEntity> accountProvider,
                                               IProvider<Guid, TruckTicketEntity> truckTicketProvider,
                                               ILifetimeScope lifetimeScope,
                                               ILog log)
    {
        _accountProvider = accountProvider;
        _truckTicketProvider = truckTicketProvider;
        _lifetimeScope = lifetimeScope;
        _log = log;
    }

    public override int RunOrder => 120;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override Task<bool> ShouldRun(BusinessContext<BillingConfigurationEntity> context)
    {
        var shouldRun = false;

        // check for the billing contact change upon updating of the billing configuration
        if (context.Operation == Operation.Update)
        {
            shouldRun = context.Original.BillingContactId != context.Target.BillingContactId;
        }

        return Task.FromResult(shouldRun);
    }

    public override async Task<bool> Run(BusinessContext<BillingConfigurationEntity> context)
    {
        var billingConfig = context.Target;

        // fetch the parent account
        var account = await _accountProvider.GetById(billingConfig.BillingCustomerAccountId);
        if (account == null)
        {
            // an account must exist for this billing config
            return false;
        }

        // find the contact
        var contact = account.Contacts.FirstOrDefault(c => c.Id == billingConfig.BillingContactId);
        if (contact == null)
        {
            // a contact must exist on the entity
            return false;
        }

        // all affected truck tickets
        var truckTickets = await _truckTicketProvider.Get(tt => tt.BillingConfigurationId == billingConfig.Id &&
                                                                tt.BillingContact.AccountContactId == context.Original.BillingContactId &&
                                                                ApplicableStatuses.Contains(tt.Status));

        // update each truck ticket
        foreach (var truckTicket in truckTickets)
        {
            try
            {
                await UpdateTruckTicketSwiftly(truckTicket.Key, contact, _lifetimeScope);
            }
            catch (Exception e)
            {
                _log.Error(exception: e, messageTemplate: $"Unable to update the truck ticket '{truckTicket.TicketNumber}' with this key: {truckTicket.Key}");
            }
        }

        return true;
    }

    private static async Task UpdateTruckTicketSwiftly(CompositeKey<Guid> truckTicketKey, AccountContactEntity contact, ILifetimeScope parentScope)
    {
        // isolate updates
        await using var childScope = parentScope.BeginLifetimeScope();
        var truckTicketProvider = childScope.Resolve<IProvider<Guid, TruckTicketEntity>>();

        // fetch the fresh copy of the ticket
        var truckTicket = await truckTicketProvider.GetById(truckTicketKey);

        // update the contact
        truckTicket.BillingContact = new()
        {
            Id = Guid.NewGuid(),
            AccountContactId = contact.Id,
            Name = contact.GetFullName(),
            Address = contact.GetFullAddress(true),
            PhoneNumber = contact.PhoneNumber,
            Email = contact.Email,
        };

        // save the ticket
        await truckTicketProvider.Update(truckTicket);
    }
}
