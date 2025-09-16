using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.BillingConfiguration.Tasks;
using SE.Shared.Domain.Entities.EDIFieldDefinition;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.EDIFieldDefinition;

public class EDIFieldDefinitionTruckTicketValidationTask : WorkflowTaskBase<BusinessContext<EDIFieldDefinitionEntity>>
{
    public const string ResultKey = nameof(EDIFieldDefinitionTruckTicketValidationTask) + nameof(ResultKey);

    private readonly IProvider<Guid, TruckTicketEntity> _truckTicketProvider;

    public EDIFieldDefinitionTruckTicketValidationTask(IProvider<Guid, TruckTicketEntity> truckTicketProvider)
    {
        _truckTicketProvider = truckTicketProvider;
    }

    public override int RunOrder => 20;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<EDIFieldDefinitionEntity> context)
    {
        if (context.Target is null)
        {
            return false;
        }

        var ediFieldDefinition = context.Target;
        var ediFieldDefinitions = context.ContextBag[nameof(EdiDefinitionDataLoaderTask.EdiDefinitionsKey)] as List<EDIFieldDefinitionEntity>;
        ediFieldDefinitions!.Add(ediFieldDefinition);

        // get truck tickets for the EDIFieldDefinitionEntity being updated by CustomerId
        var truckTickets = (await _truckTicketProvider.Get(t => t.BillingCustomerId == ediFieldDefinition.CustomerId &&
                                                                (t.Status == TruckTicketStatus.Approved ||
                                                                 t.Status == TruckTicketStatus.Hold ||
                                                                 t.Status == TruckTicketStatus.New))).ToArray(); // PK - XP for TT by customer

        foreach (var truckTicket in truckTickets)
        {
            var ediDefinitionValueMap = (truckTicket.EdiFieldValues ?? new()).ToDictionary(ediValue => ediValue.EDIFieldDefinitionId);
            truckTicket.IsEdiValid = true;

            foreach (var definition in ediFieldDefinitions)
            {
                var error = definition.Validate(ediDefinitionValueMap);
                if (error is not null)
                {
                    truckTicket.IsEdiValid = false;
                }
            }

            await _truckTicketProvider.Update(truckTicket, true);
        }

        return true;
    }

    public override Task<bool> ShouldRun(BusinessContext<EDIFieldDefinitionEntity> context)
    {
        return Task.FromResult(true);
    }
}
