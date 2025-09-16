using System.Threading.Tasks;

using SE.Enterprise.Contracts.Models;

using Trident.Business;

namespace SE.Shared.Domain.Tasks;

public interface IEntityPublishMessageTask<TEntity> where TEntity : TTEntityBase
{
    Task<bool> ShouldPublishMessage(BusinessContext<TEntity> context);

    Task<string> GetSessionIdForMessage(BusinessContext<TEntity> context);

    Task<TEntity> EvaluateEntityForUpdates(BusinessContext<TEntity> context)
    {
        return Task.FromResult(context.Target);
    }

    Task EnrichEnvelopeModel(EntityEnvelopeModel<TEntity> model)
    {
        return Task.CompletedTask;
    }
}
