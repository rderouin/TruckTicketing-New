using System;
using System.Linq;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Constants;
using SE.Enterprise.Contracts.Models;
using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Entities.EntityStatus;
using SE.TridentContrib.Extensions.Security;
using SE.TruckTicketing.Contracts.Models;
using SE.TruckTicketing.Contracts.Models.LoadConfirmations;

using Trident.Business;
using Trident.Contracts;
using Trident.Contracts.Api;
using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Validation;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.LoadConfirmation;

public class LoadConfirmationManager : ManagerBase<Guid, LoadConfirmationEntity>, ILoadConfirmationManager
{
    private readonly IEntityPublisher _entityPublisher;

    private readonly IManager<Guid, EntityStatusEntity> _entityStatusManager;

    private readonly IProvider<Guid, EntityStatusEntity> _entityStatusProvider;

    private readonly IUserContextAccessor _userContextAccessor;

    public LoadConfirmationManager(ILog logger,
                                   IProvider<Guid, LoadConfirmationEntity> provider,
                                   IProvider<Guid, EntityStatusEntity> entityStatusProvider,
                                   IManager<Guid, EntityStatusEntity> entityStatusManager,
                                   IEntityPublisher entityPublisher,
                                   IUserContextAccessor userContextAccessor,
                                   IValidationManager<LoadConfirmationEntity> validationManager = null,
                                   IWorkflowManager<LoadConfirmationEntity> workflowManager = null)
        : base(logger, provider, validationManager, workflowManager)
    {
        _entityStatusProvider = entityStatusProvider;
        _entityStatusManager = entityStatusManager;
        _entityPublisher = entityPublisher;
        _userContextAccessor = userContextAccessor;
    }

    public async Task<LoadConfirmationBulkResponse> QueueLoadConfirmationAction(LoadConfirmationBulkRequest bulkRequest)
    {
        // --- STAGE 1. Mark all LCs as pending action

        // fetch existing LCs
        var loadConfirmations = (await Provider.GetByIds(bulkRequest.LoadConfirmationKeys)).ToList();

        // fetch entity statuses
        var lcIds = loadConfirmations.Select(lc => lc.Id).ToHashSet();
        var statuses = (await _entityStatusProvider.Get(s => lcIds.Contains(s.ReferenceEntityKey.Id), EntityStatusEntity.GetPartitionKey(Databases.Discriminators.LoadConfirmation))).ToList();

        // update the pending action flag
        var statusesLookup = statuses.ToLookup(s => s.ReferenceEntityKey);
        var statusesToUpdate = loadConfirmations.ToDictionary(lc => lc.Key, lc => statusesLookup[lc.Key].FirstOrDefault() ?? CreateStatusEntity(lc.Key));
        foreach (var statusToUpdate in statusesToUpdate.Values)
        {
            statusToUpdate.Status = "PendingAsyncAction";
            if (statusToUpdate.DocumentType.HasText())
            {
                await _entityStatusManager.Update(statusToUpdate, true);
            }
            else
            {
                await _entityStatusManager.Insert(statusToUpdate, true);
            }
        }

        // save to the database
        await _entityStatusManager.SaveDeferred();

        // --- STAGE 2. Queue all the entities
        var userContext = _userContextAccessor.UserContext;
        foreach (var request in bulkRequest.ToSingleRequests())
        {
            await _entityPublisher.EnqueueMessage(new EntityEnvelopeModel<UserRequest<LoadConfirmationSingleRequest>>
            {
                CorrelationId = Guid.NewGuid().ToString(),
                EnterpriseId = request.LoadConfirmationKey.Id,
                SourceId = request.LoadConfirmationKey.Id.ToString(),
                MessageDate = DateTime.UtcNow,
                MessageType = ServiceBusConstants.EntityMessageTypes.ProcessLoadConfirmationRequest,
                Source = ServiceBusConstants.Sources.TruckTicketingAsyncFlows,
                Payload = new()
                {
                    UserToken = userContext.OriginalToken,
                    Model = request,
                },
                Operation = Operation.Update.ToString(),
            });
        }

        return new()
        {
            IsSuccessful = true,
            PendingActions = loadConfirmations.Select(lc => (lc.Key, true)).ToList(),
        };

        EntityStatusEntity CreateStatusEntity(CompositeKey<Guid> loadConfirmationKey)
        {
            return new()
            {
                Id = Guid.NewGuid(),
                ReferenceEntityType = Databases.Discriminators.LoadConfirmation,
                ReferenceEntityKey = loadConfirmationKey,
            };
        }
    }
}
