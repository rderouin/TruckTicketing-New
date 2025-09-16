using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.SalesLine;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.SalesLine.Tasks;

public class SalesLineAttachmentTypeIndicatorTask : WorkflowTaskBase<BusinessContext<SalesLineEntity>>
{
    public override int RunOrder => 20;

    public override OperationStage Stage => OperationStage.BeforeInsert | OperationStage.BeforeUpdate;

    public override async Task<bool> Run(BusinessContext<SalesLineEntity> context)
    {
        if (context.Target is null)
        {
            return false;
        }

        var salesLine = context.Target;

        var salesLineHasInternalAttachments = salesLine.Attachments.Any(attachment => attachment.IsInternalAttachment());
        var salesLineHasExternalAttachments = salesLine.Attachments.Any(attachment => attachment.IsExternalAttachment());

        salesLine.AttachmentIndicatorType = (salesLineHasInternalAttachments, salesLineHasExternalAttachments) switch
                                            {
                                                (true, true) => AttachmentIndicatorType.InternalExternal,
                                                (true, false) => AttachmentIndicatorType.Internal,
                                                (false, true) => AttachmentIndicatorType.External,
                                                (false, false) => AttachmentIndicatorType.Neither,
                                            };

        return await Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<SalesLineEntity> context)
    {
        var salesLine = context.Target;

        return Task.FromResult((context.Operation == Operation.Insert || context.Operation == Operation.Update) &&
                               salesLine != null &&
                               salesLine.Attachments != null &&
                               salesLine.Attachments.Any());
    }
}
