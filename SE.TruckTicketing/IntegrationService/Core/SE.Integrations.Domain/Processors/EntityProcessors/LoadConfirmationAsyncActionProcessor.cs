using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Constants;
using SE.Enterprise.Contracts.Models;
using SE.Shared.Domain;
using SE.Shared.Domain.Entities.EntityStatus;
using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.TridentContrib.Extensions.Security;
using SE.TruckTicketing.Contracts.Models;
using SE.TruckTicketing.Contracts.Models.LoadConfirmations;
using SE.TruckTicketing.Contracts.Security;

using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Logging;

namespace SE.Integrations.Domain.Processors.EntityProcessors;

[EntityProcessorFor(ServiceBusConstants.EntityMessageTypes.ProcessLoadConfirmationRequest)]
public class LoadConfirmationAsyncActionProcessor : BaseEntityProcessor<UserRequest<LoadConfirmationSingleRequest>>
{
    private readonly ITruckTicketingAuthorizationService _authorizationService;

    private readonly IProvider<Guid, EntityStatusEntity> _entityStatusProvider;

    private readonly ILoadConfirmationApprovalWorkflow _loadConfirmationApprovalWorkflow;

    private readonly ILog _log;

    private readonly IUserContextAccessor _userContextAccessor;

    public LoadConfirmationAsyncActionProcessor(ILoadConfirmationApprovalWorkflow loadConfirmationApprovalWorkflow,
                                                IProvider<Guid, EntityStatusEntity> entityStatusProvider,
                                                ITruckTicketingAuthorizationService authorizationService,
                                                IUserContextAccessor userContextAccessor,
                                                ILog log)
    {
        _loadConfirmationApprovalWorkflow = loadConfirmationApprovalWorkflow;
        _entityStatusProvider = entityStatusProvider;
        _authorizationService = authorizationService;
        _userContextAccessor = userContextAccessor;
        _log = log;
    }

    public override async Task Process(EntityEnvelopeModel<UserRequest<LoadConfirmationSingleRequest>> model)
    {
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            model?.Payload?.Model.GuardIsNotNull(nameof(model.Payload.Model));
            model?.Payload?.UserToken.GuardIsNotNull(nameof(model.Payload.UserToken));

            // init the user's context
            _userContextAccessor.UserContext = await CreateUserContext(model!.Payload!.UserToken);

            // the original request
            var request = model.Payload.Model;

            // run the action
            switch (request.Action)
            {
                case LoadConfirmationAction.SendLoadConfirmation:
                    await _loadConfirmationApprovalWorkflow.StartFromBeginning(request.LoadConfirmationKey, request.AdditionalNotes, false);
                    break;

                default:
                    await _loadConfirmationApprovalWorkflow.DoLoadConfirmationAction(request);
                    break;
            }

            LogLoadConfirmationActionEvent("Successful");
        }
        catch (Exception x)
        {
            // when a request cannot be processed due to any error, log the error and unlock the LC
            _log.Error(exception: x);
            LogLoadConfirmationActionEvent("Failed");
        }
        finally
        {
            // reset the flag, unlock the load confirmation, either successful or not - the LC is processed
            var statuses = await _entityStatusProvider.Get(s => s.ReferenceEntityKey.Id == model.Payload.Model.LoadConfirmationKey.Id,
                                                           EntityStatusEntity.GetPartitionKey(Databases.Discriminators.LoadConfirmation));

            var first = true;
            foreach (var status in statuses)
            {
                if (first)
                {
                    first = false;
                    status.Status = null;
                    await _entityStatusProvider.Update(status);
                }
                else
                {
                    await _entityStatusProvider.Delete(status);
                }
            }
        }

        void LogLoadConfirmationActionEvent(string status)
        {
            var elapsed = DateTimeOffset.UtcNow - startTime;
            _log.Information(messageTemplate: new Dictionary<string, string>
            {
                ["EventName"] = nameof(LoadConfirmationAsyncActionProcessor),
                [nameof(LoadConfirmationAction)] = model?.Payload?.Model?.Action.ToString(),
                [nameof(elapsed.Duration)] = elapsed.ToString(),
                ["Status"] = status,
            }.ToJson());
        }
    }

    private async Task<UserContext> CreateUserContext(string token)
    {
        // get the claims principal
        var claimsPrincipal = await _authorizationService.ValidateToken(token);

        // init the current user context
        return new()
        {
            Principal = claimsPrincipal,
            DisplayName = TryGetClaim(ClaimConstants.Name),
            ObjectId = TryGetClaim(ClaimConstants.ObjectId),
            EmailAddress = TryGetClaim(ClaimConstants.Emails),
            OriginalToken = token,
        };

        string TryGetClaim(string name)
        {
            // null by default
            string value = null;

            try
            {
                // try fetching the claim
                value = _authorizationService.GetClaim(claimsPrincipal, name);
            }
            catch
            {
                // skip and return the default value
            }

            return value;
        }
    }
}
