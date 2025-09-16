using System.Threading.Tasks;

using SE.Enterprise.Contracts.Models;
using SE.Enterprise.Contracts.Models.InvoiceDelivery;
using SE.Shared.Domain.Tasks;

using Trident.Business;

namespace SE.Shared.Domain.Entities.Account.Tasks;

public class AccountContactIndexEntityPublishMessageTask : IEntityPublishMessageTask<AccountContactIndexEntity>
{
    public Task<bool> ShouldPublishMessage(BusinessContext<AccountContactIndexEntity> context)
    {
        var publishMessage = context.Target.IsActive is not false;
        return Task.FromResult(publishMessage);
    }

    public Task EnrichEnvelopeModel(EntityEnvelopeModel<AccountContactIndexEntity> model)
    {
        model.MessageType = MessageType.AccountContact.ToString();
        return Task.CompletedTask;
    }

    public Task<string> GetSessionIdForMessage(BusinessContext<AccountContactIndexEntity> context)
    {
        var defaultSessionId = context.Target.Id.ToString();
        return Task.FromResult(defaultSessionId);
    }
}
