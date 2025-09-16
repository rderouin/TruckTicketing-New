using System;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.TruckTicket;

using Trident.Business;
using Trident.Contracts;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

public class TruckTicketSalesLineAttachmentLinkerTask : WorkflowTaskBase<BusinessContext<TruckTicketEntity>>
{
    private readonly IManager<Guid, SalesLineEntity> _salesLineManager;

    public TruckTicketSalesLineAttachmentLinkerTask(IManager<Guid, SalesLineEntity> salesLineManager)
    {
        _salesLineManager = salesLineManager;
    }

    public override int RunOrder => 50;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<TruckTicketEntity> context)
    {
        var salesLines = (await _salesLineManager.Get(salesLine => salesLine.TruckTicketId == context.Target.Id)).ToList(); // PK - XP for SL by TT ID

        foreach (var salesLine in salesLines)
        {
            salesLine.Attachments = context.Target.Attachments?
                                           .OrderByDescending(attachment => attachment.AttachmentType.ToString())
                                           .Select(attachment => new SalesLineAttachmentEntity
                                            {
                                                AttachmentType = attachment.AttachmentType,
                                                Container = attachment.Container,
                                                Path = attachment.Path,
                                                File = attachment.File,
                                                Id = attachment.Id,
                                            })
                                           .ToList() ?? new();

            await _salesLineManager.Update(salesLine, true);
        }

        return true;
    }

    public override Task<bool> ShouldRun(BusinessContext<TruckTicketEntity> context)
    {
        string FingerPrint(TruckTicketAttachmentEntity attachment)
        {
            return $"{attachment.File}{attachment.Path}{attachment.Container}{attachment.AttachmentType}";
        }

        var originalAttachments = context.Original?.Attachments.Select(FingerPrint).ToArray() ?? Array.Empty<string>();
        var currentAttachments = context.Target.Attachments.Select(FingerPrint);
        var attachmentsEqual = originalAttachments.SequenceEqual(currentAttachments);
        return Task.FromResult(!attachmentsEqual);
    }
}
