using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Contracts.Models.Operations;

namespace SE.TruckTicketing.Client.Components.UserControls;

public partial class MaterialApprovalOperatorNotesAcknowledgmentDialog : BaseTruckTicketingComponent
{
    private readonly HashSet<Guid> _acknowledgedMaterialApprovalIds = new();

    private MaterialApproval _materialApproval;

    private Guid? _truckTicketId;

    [Parameter]
    public TruckTicket TruckTicket { get; set; }

    [Parameter]
    public EventCallback OnAcknowledgementDecline { get; set; }

    [Parameter]
    public EventCallback<MaterialApproval> OnMaterialApprovalAccept { get; set; }

    private EventCallback HandleAcknowledgementDecline => new(this, DeclineAcknowledgement);

    private EventCallback<string> HandleAcknowledgement => new(this, AcknowledgeScaleOperatorNotes);

    protected override void OnParametersSet()
    {
        if (_truckTicketId == TruckTicket?.Id && _truckTicketId != Guid.Empty)
        {
            return;
        }

        _truckTicketId = TruckTicket?.Id;
        _acknowledgedMaterialApprovalIds.Clear();
    }

    public async Task OpenDialog(MaterialApproval materialApproval)
    {
        if (materialApproval is null ||
            !materialApproval.ScaleOperatorNotes.HasText() ||
            TruckTicket?.Acknowledgement?.Contains(materialApproval.MaterialApprovalNumber) is true)
        {
            await OnMaterialApprovalAccept.InvokeAsync(materialApproval);
            return;
        }

        _materialApproval = materialApproval;

        await DialogService.OpenAsync<MaterialApprovalOperatorNotesAcknowledgmentForm>("Scale Operator Notes Acknowledgement", new()
        {
            { nameof(MaterialApprovalOperatorNotesAcknowledgmentForm.MaterialApproval), materialApproval },
            { nameof(MaterialApprovalOperatorNotesAcknowledgmentForm.OnAcknowledge), HandleAcknowledgement },
            { nameof(MaterialApprovalOperatorNotesAcknowledgmentForm.OnAcknowledgementDecline), HandleAcknowledgementDecline },
        }, new()
        {
            Width = "35%",
            CloseDialogOnOverlayClick = false,
            ShowClose = false,
        });
    }

    private async Task AcknowledgeScaleOperatorNotes(string initials)
    {
        TruckTicket.Acknowledgement = $"{_materialApproval.MaterialApprovalNumber}||{initials}";
        _acknowledgedMaterialApprovalIds.Add(_materialApproval.Id);
        DialogService.Close();
        await OnMaterialApprovalAccept.InvokeAsync(_materialApproval);
        _materialApproval = default;
    }

    private async Task DeclineAcknowledgement()
    {
        DialogService.Close();

        await OnAcknowledgementDecline.InvokeAsync();
    }
}
