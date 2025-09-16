using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Lookups;
using Trident.Business;
using Trident.Extensions;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

public class TruckTicketConcurrentUpdateMergerTask : WorkflowTaskBase<BusinessContext<TruckTicketEntity>>
{

    private readonly ILogger<TruckTicketConcurrentUpdateMergerTask> _logger;
    public TruckTicketConcurrentUpdateMergerTask(ILogger<TruckTicketConcurrentUpdateMergerTask> logger)
    {
        _logger = logger;
    }
    public override int RunOrder => -1;

    public override OperationStage Stage => OperationStage.BeforeUpdate;

    public override Task<bool> Run(BusinessContext<TruckTicketEntity> context)
    {
        var original = context.Original;
        var target = context.Target;

        target.Attachments = original.Attachments.MergeBy(target.Attachments, attachment => attachment.Id).ToList();

        if (target.CountryCode is Contracts.Lookups.CountryCode.US && target.TruckTicketType is TruckTicketType.WT)
        {
            Func<string, AttachmentType, bool> checkInvalidAttachment = (fileName, attachmentType) =>
            {
                if (fileName.Contains("-EXT", StringComparison.OrdinalIgnoreCase)
                                             && attachmentType == Contracts.Lookups.AttachmentType.Internal)
                {
                    return true;
                }
                if (fileName.Contains("-INT", StringComparison.OrdinalIgnoreCase)
                                             && attachmentType == Contracts.Lookups.AttachmentType.External)
                {
                    return true;
                }
                return false;
            };
            var invalidAttachments = target.Attachments.Where(f => checkInvalidAttachment(f.File, f.AttachmentType)).ToList();
            if (invalidAttachments.Any())
            {
                foreach (var attachment in invalidAttachments)
                {
                    var lastHyphenPosition = attachment.File.LastIndexOf('-');
                    var attachmentType = attachment.File.Substring(lastHyphenPosition + 1, 3);
                    if (!attachmentType.Equals("int", StringComparison.OrdinalIgnoreCase) && !attachmentType.Equals("ext", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning($"Valid Truck Ticket attachment type not found in file name. (FileName: {attachment.File})");
                    }
                    else
                    {
                        attachmentType = attachmentType.Equals("int", StringComparison.OrdinalIgnoreCase) ? "internal" : "external";
                    }
                    _logger.LogInformation($"Mismatch attachment found. FileName is {attachment.File} and type is {attachmentType}. Updating it properly.");
                    var validAttachmentType = Enum.TryParse(attachmentType, true, out AttachmentType attachmentTypeValue);
                    var usAttachmentType = validAttachmentType ? attachmentTypeValue : AttachmentType.Internal;
                    attachment.AttachmentType = usAttachmentType;
                }
            }
        }

        return Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<TruckTicketEntity> context)
    {
        var shouldRun = context.Original.VersionTag != context.Target.VersionTag &&
                        !context.Original.Attachments.Select(attachment => attachment.Id)
                                .SequenceEqual(context.Target.Attachments.Select(attachment => attachment.Id));

        return Task.FromResult(shouldRun);
    }
}
