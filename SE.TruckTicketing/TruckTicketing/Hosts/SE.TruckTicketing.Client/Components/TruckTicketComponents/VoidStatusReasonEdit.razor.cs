using System;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.TruckTicketComponents;

public partial class VoidStatusReasonEdit : BaseRazorComponent
{
    protected bool IsBusy;

    [Inject]
    private IServiceBase<TruckTicketVoidReason, Guid> TruckTicketVoidReasonService { get; set; }

    [Parameter]
    public TruckTicketVoidReason Model { get; set; }

    [Inject]
    private NotificationService notificationService { get; set; }

    [Parameter]
    public EventCallback<TruckTicketVoidReason> OnSubmit { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    private async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }

    private async Task HandleSubmit()
    {
        IsBusy = true;
        var IsNew = Model.Id == default;
        var response = Model.Id == default
                           ? await TruckTicketVoidReasonService.Create(Model)
                           : await TruckTicketVoidReasonService.Update(Model);

        if (response.IsSuccessStatusCode)
        {
            notificationService.Notify(NotificationSeverity.Success,
                                       $"{Model.VoidReason} Void Reason {(IsNew ? "created" : "updated")}.");

            DialogService.Close();
            await OnSubmit.InvokeAsync(Model);
        }
        else if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            notificationService.Notify(NotificationSeverity.Error, "Failed to save Void Reason.");
        }

        IsBusy = false;
    }
}
