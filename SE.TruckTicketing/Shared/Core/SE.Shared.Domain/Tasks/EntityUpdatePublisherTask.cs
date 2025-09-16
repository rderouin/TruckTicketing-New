using System;
using System.Linq;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Models;

using Trident.Business;
using Trident.Contracts.Configuration;
using Trident.Workflow;

namespace SE.Shared.Domain.Tasks;

public class EntityUpdatePublisherTask<TEntity> : WorkflowTaskBase<BusinessContext<TEntity>> where TEntity : TTAuditableEntityBase
{
    private readonly IAppSettings _appSettings;

    private readonly IEntityPublisher _entityPublisher;

    private readonly IEntityPublishMessageTask<TEntity> _entityPublishMessageTask;

    public EntityUpdatePublisherTask(IAppSettings appSettings,
                                     IEntityPublisher entityPublisher,
                                     IEntityPublishMessageTask<TEntity> entityPublishMessageTask = null)
    {
        _appSettings = appSettings;
        _entityPublisher = entityPublisher;
        _entityPublishMessageTask = entityPublishMessageTask;
    }

    public override int RunOrder => 10;

    public override OperationStage Stage => OperationStage.AfterInsert | OperationStage.AfterUpdate | OperationStage.AfterDelete;

    public override async Task<bool> Run(BusinessContext<TEntity> context)
    {
        string sessionId = default;
        var targetEntity = context.Target;
        Func<EntityEnvelopeModel<TEntity>, Task> envelopeEnricher = _ => Task.CompletedTask;
        if (_entityPublishMessageTask != null)
        {
            sessionId = await _entityPublishMessageTask.GetSessionIdForMessage(context);
            targetEntity = await _entityPublishMessageTask.EvaluateEntityForUpdates(context);
            envelopeEnricher = _entityPublishMessageTask.EnrichEnvelopeModel;
        }

        await _entityPublisher.EnqueueMessage(targetEntity, context.Operation.ToString(), sessionId, envelopeEnricher);
        return true;
    }

    public override async Task<bool> ShouldRun(BusinessContext<TEntity> context)
    {
        var settings = _appSettings.GetSection<EntityUpdatesPublisherSettings>(EntityUpdatesPublisherSettings.Section);
        var entitySetting = settings.PublishEntities.FirstOrDefault(setting => setting.EntityType.Equals(context.Target.EntityType));
        var shouldRun = entitySetting is not null && entitySetting.Operations.Contains(context.Operation);

        if (shouldRun && _entityPublishMessageTask != null)
        {
            shouldRun = await _entityPublishMessageTask.ShouldPublishMessage(context);
        }

        return shouldRun;
    }
}

public class EntityUpdatesPublisherSettings
{
    public const string Section = "EntityUpdatesPublisher";

    public EntityPublishSettings[] PublishEntities { get; set; } = Array.Empty<EntityPublishSettings>();

    public class EntityPublishSettings
    {
        public string EntityType { get; set; }

        public Operation[] Operations { get; set; } = { Operation.Insert, Operation.Update, Operation.Delete };
    }
}
