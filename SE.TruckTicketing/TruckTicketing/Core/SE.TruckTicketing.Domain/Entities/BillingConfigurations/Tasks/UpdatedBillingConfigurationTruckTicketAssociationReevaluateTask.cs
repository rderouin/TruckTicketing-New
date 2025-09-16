using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.SalesLine;
using SE.TruckTicketing.Domain.Entities.TruckTicket;

using Trident.Business;
using Trident.Extensions;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.BillingConfigurations.Tasks;

public class UpdatedBillingConfigurationTruckTicketAssociationReevaluateTask : WorkflowTaskBase<BusinessContext<BillingConfigurationEntity>>
{
    private readonly IMatchPredicateRankManager _matchPredicateRankManager;

    private readonly ISalesLineManager _salesLineManager;

    private readonly ITruckTicketManager _truckTicketManager;

    public UpdatedBillingConfigurationTruckTicketAssociationReevaluateTask(ITruckTicketManager truckTicketManager,
                                                                           ISalesLineManager salesLineManager,
                                                                           IMatchPredicateRankManager matchPredicateRankManager)
    {
        _truckTicketManager = truckTicketManager;
        _salesLineManager = salesLineManager;
        _matchPredicateRankManager = matchPredicateRankManager;
    }

    public override int RunOrder => 90;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<BillingConfigurationEntity> context)
    {
        List<TruckTicketEntity> inValidTruckTicketEntities = new();
        //Find TruckTicket Associated to current updated billing configuration

        var truckTickets = await _truckTicketManager.Get(ticket => ticket.BillingConfigurationId == context.Target.Id && ticket.Status != TruckTicketStatus.Invoiced &&
                                                                   ticket.Status != TruckTicketStatus.Approved);

        var truckTicketEntities = truckTickets?.ToList();
        //if TruckTickets associated to current billing configuration exists; check if it's a still match
        if (truckTicketEntities != null && truckTicketEntities.Any())
        {
            truckTicketEntities.ForEach(ticket =>
                                        {
                                            var isValid = false;
                                            foreach (var matchCriteria in context.Target.MatchCriteria)
                                            {
                                                isValid = _matchPredicateRankManager.IsBillingConfigurationMatch(ticket, matchCriteria);
                                            }

                                            if (!isValid)
                                            {
                                                inValidTruckTicketEntities.Add(ticket);
                                            }
                                        });
        }

        if (!inValidTruckTicketEntities.Any())
        {
            return true;
        }

        //Find matching billing configurations if current billing configuration is invalid for TruckTicket associated to current billing configuration
        foreach (var truckTicket in inValidTruckTicketEntities)
        {
            await UpdateBillingConfiguration(truckTicket);
        }

        async Task UpdateBillingConfiguration(TruckTicketEntity ticket)
        {
            var matchingBillingConfigurations = await _truckTicketManager.GetMatchingBillingConfigurations(ticket);
            //Select billing configuration to associate with truck ticket;
            //If Automatic billing configuration found - user that to associate
            //Else use default billing configuration
            var selectedBillingConfiguration = matchingBillingConfigurations != null
                                                   ? matchingBillingConfigurations.Any(x => x.IncludeForAutomation)
                                                         ? matchingBillingConfigurations.First(x => x.IncludeForAutomation)
                                                         : matchingBillingConfigurations.Any(x => x.IsDefaultConfiguration)
                                                             ? matchingBillingConfigurations.First(x => x.IsDefaultConfiguration)
                                                             : new()
                                                   : new();

            await UpdateTruckTicket(ticket, selectedBillingConfiguration);
        }

        async Task UpdateTruckTicket(TruckTicketEntity ticket, BillingConfigurationEntity billingConfiguration)
        {
            if (billingConfiguration.Id != default)
            {
                ticket.BillingConfigurationId = billingConfiguration.Id;

                ticket.BillingContact = new()
                {
                    AccountContactId = billingConfiguration.BillingContactId ?? Guid.Empty,
                };

                ticket.EdiFieldValues = billingConfiguration
                                       .EDIValueData?.Select(e => e.Clone())
                                       .ToList();

                ticket.Signatories = billingConfiguration
                                    .Signatories?.Where(e => e.IsAuthorized)
                                    .Select(e => new SignatoryEntity
                                     {
                                         AccountContactId = e.AccountContactId,
                                         ContactEmail = e.Email,
                                         ContactPhoneNumber = e.PhoneNumber,
                                         ContactAddress = e.Address,
                                         ContactName = e.FirstName + " " + e.LastName,
                                     })
                                    .ToList();

                if (billingConfiguration.BillingCustomerAccountId != ticket.BillingCustomerId)
                {
                    ticket.BillingCustomerId = billingConfiguration.BillingCustomerAccountId;

                    //Update pricing, set SalesLine InPreview & TruckTicket on Open

                    var salesLinesResults = await _salesLineManager.Search(new()
                    {
                        Filters = new()
                        {
                            { nameof(SalesLineEntity.TruckTicketId), ticket.Id },
                        },
                    });

                    var salesLines = salesLinesResults?.Results?.ToList() ?? new();

                    if (salesLines.Any())
                    {
                        salesLines.ForEach(sl =>
                                           {
                                               sl.Status = SalesLineStatus.Preview;
                                               sl.CustomerId = ticket.BillingCustomerId;
                                           });

                        salesLines = await _salesLineManager.PriceRefresh(salesLines);

                        if (salesLines != null && salesLines.Any())
                        {
                            foreach (var salesLine in salesLines)
                            {
                                await UpdateSalesLine(salesLine);
                            }
                        }
                    }
                }
            }
            else
            {
                ticket.BillingConfigurationId = default;

                ticket.BillingCustomerId = default;

                ticket.BillingContact = new();

                ticket.EdiFieldValues = new();

                ticket.Signatories = new();

                //Delete SalesLine when de-associating Billing Configuration from truckticket

                var salesLinesResults = await _salesLineManager.Search(new()
                {
                    Filters = new()
                    {
                        { nameof(SalesLineEntity.TruckTicketId), ticket.Id },
                    },
                });

                var salesLines = salesLinesResults?.Results?.ToList() ?? new();
                if (salesLines.Any())
                {
                    foreach (var salesLine in salesLines)
                    {
                        await DeleteSalesLine(salesLine);
                    }
                }
            }

            ticket.Status = TruckTicketStatus.Open;

            await _truckTicketManager.Update(ticket, true);
        }

        return true;
    }

    private async Task UpdateSalesLine(SalesLineEntity salesLine)
    {
        await _salesLineManager.Update(salesLine, true);
    }

    private async Task DeleteSalesLine(SalesLineEntity saleLine)
    {
        await _salesLineManager.Delete(saleLine, true);
    }

    public override Task<bool> ShouldRun(BusinessContext<BillingConfigurationEntity> context)
    {
        //Task Disabled to revisit truck ticket update logic
        return Task.FromResult(false);
    }

    private bool IsMatchCriteriaUpdated(BusinessContext<BillingConfigurationEntity> context)
    {
        var original = context.Original?.MatchCriteria.ToJson();
        var target = context.Target.MatchCriteria.ToJson();
        return string.CompareOrdinal(original, target) != 0;
    }
}
