using System;
using System.Threading.Tasks;

using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Entities.Note;
using SE.Shared.Domain.Entities.TruckTicket;

using Trident.Business;
using Trident.Contracts;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

public class TruckTicketMaterialApprovalScaleOperatorNotesTask : WorkflowTaskBase<BusinessContext<TruckTicketEntity>>
{
    private readonly IManager<Guid, NoteEntity> _noteManager;

    public TruckTicketMaterialApprovalScaleOperatorNotesTask(IManager<Guid, NoteEntity> noteManager)
    {
        _noteManager = noteManager;
    }

    public override int RunOrder => 55;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<TruckTicketEntity> context)
    {
        var targetEntity = context.Target;

        await _noteManager.Save(new()
        {
            Id = Guid.NewGuid(),
            ThreadId = $"TruckTicket|{targetEntity.Id}",
            Comment = $"Scale operator notes for material approval number {targetEntity.MaterialApprovalNumber} has been acknowledged by {targetEntity.Acknowledgement}",
            NotEditable = true,
        }, true);

        return await Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<TruckTicketEntity> context)
    {
        var isMaterialApprovalUpdated = (context.Operation == Operation.Update &&
                                         context.Original.MaterialApprovalId != context.Target.MaterialApprovalId) ||
                                        (context.Operation == Operation.Insert && context.Target.MaterialApprovalId != Guid.Empty);

        return Task.FromResult(isMaterialApprovalUpdated && context.Target.Acknowledgement.HasText());
    }
}
