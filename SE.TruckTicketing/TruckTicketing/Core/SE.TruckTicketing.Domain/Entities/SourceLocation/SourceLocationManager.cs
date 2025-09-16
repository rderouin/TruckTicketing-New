using System;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.SourceLocation;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Contracts;
using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Validation;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.SourceLocation;

public interface ISourceLocationManager : IManager<Guid, SourceLocationEntity>
{
    Task<bool> MarkSourceLocationDelete(Guid sourceLocationId);
}

public class SourceLocationManager : ManagerBase<Guid, SourceLocationEntity>, ISourceLocationManager
{
    private readonly IProvider<Guid, TruckTicketEntity> _truckTicketProvider;

    public SourceLocationManager(ILog logger,
                                 IProvider<Guid, SourceLocationEntity> provider,
                                 IProvider<Guid, TruckTicketEntity> truckTicketProvider,
                                 IValidationManager<SourceLocationEntity> validationManager = null,
                                 IWorkflowManager<SourceLocationEntity> workflowManager = null) : base(logger, provider, validationManager, workflowManager)
    {
        _truckTicketProvider = truckTicketProvider;
    }

    public async Task<bool> MarkSourceLocationDelete(Guid sourceLocationId)
    {
        ////check associated truck tickets
        var associatedTruckTicket = await _truckTicketProvider.Get(ticket => ticket.Status != TruckTicketStatus.Void && ticket.SourceLocationId == sourceLocationId); // PK - XP for TT by Source Location ID

        //if it has any ticket return false
        if (associatedTruckTicket?.Any() == true)
        {
            return false;
        }
        //if there is no ticket associated set as deleted and return true

        await Patch(sourceLocationId, new() { { nameof(SourceLocationEntity.IsDeleted), true } });

        return true;
    }
}
