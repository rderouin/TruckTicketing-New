using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Models.Operations;

namespace SE.TruckTicketing.Client.Components.UserControls;

public partial class MaterialApprovalOperatorNotesAcknowledgmentForm : BaseTruckTicketingComponent
{
    private MaterialApprovalOperatorNotesAcknowledgmentFormModel _model;

    [Parameter]
    public EventCallback<string> OnAcknowledge { get; set; }

    [Parameter]
    public EventCallback OnAcknowledgementDecline { get; set; }

    [Parameter]
    public MaterialApproval MaterialApproval { get; set; }

    protected override void OnInitialized()
    {
        _model = new();
    }

    private async Task HandleSubmit()
    {
        await OnAcknowledge.InvokeAsync(_model.Initials);
    }
}

public class MaterialApprovalOperatorNotesAcknowledgmentFormModel
{
    public string Initials { get; set; }
}
