using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen.Blazor;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.ViewModels;

using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.SalesManagement;

public partial class AdHocLoadConfirmationDialog : BaseRazorComponent
{
    private string _attachmentType;

    private RadzenTemplateForm<SalesLineEmailViewModel> _form;

    private bool _generateAdHocLoadConfirmationBusy;

    private readonly SalesLineEmailViewModel _model = new();

    private bool _sendAdHocLoadConfirmation;

    [Parameter]
    public IEnumerable<SalesLine> SalesLines { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Parameter]
    public EventCallback<SalesLineEmailViewModel> OnHandleGenerateAdHocLoadConfirmation { get; set; }

    [Parameter]
    public EventCallback<SalesLineEmailViewModel> OnHandleSendAdHocLoadConfirmation { get; set; }

    private async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }

    private async Task HandleGenerateAdHocLoadConfirmation()
    {
        _generateAdHocLoadConfirmationBusy = true;
        await OnHandleGenerateAdHocLoadConfirmation.InvokeAsync(_model);
        _generateAdHocLoadConfirmationBusy = false;
    }

    private async Task HandleSendAdHocLoadConfirmation()
    {
        _sendAdHocLoadConfirmation = true;
        await OnHandleSendAdHocLoadConfirmation.InvokeAsync(_model);
        _sendAdHocLoadConfirmation = false;
    }

    private void OnChange(object selectedAttachmentType)
    {
        _attachmentType = selectedAttachmentType.ToString();
    }
}
