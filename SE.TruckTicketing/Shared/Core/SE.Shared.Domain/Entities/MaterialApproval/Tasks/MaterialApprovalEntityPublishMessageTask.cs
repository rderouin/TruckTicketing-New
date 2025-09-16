using System.Threading.Tasks;

using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Tasks;

using Trident.Business;

namespace SE.Shared.Domain.Entities.MaterialApproval.Tasks;

public class MaterialApprovalEntityPublishMessageTask : IEntityPublishMessageTask<MaterialApprovalEntity>
{
    public Task<bool> ShouldPublishMessage(BusinessContext<MaterialApprovalEntity> context)
    {
        var shouldPublish = context?.Target?.WLAFNumber.HasText() ?? false;
        return Task.FromResult(shouldPublish);
    }

    public Task<string> GetSessionIdForMessage(BusinessContext<MaterialApprovalEntity> context)
    {
        var referenceEntity = context?.Target ?? context?.Original ?? new();
        return Task.FromResult(referenceEntity.Id.ToString());
    }
}
