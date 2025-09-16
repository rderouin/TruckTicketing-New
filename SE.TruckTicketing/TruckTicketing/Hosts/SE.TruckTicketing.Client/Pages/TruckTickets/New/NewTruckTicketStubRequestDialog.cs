using System;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;

using BlazorDownloadFile;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

namespace SE.TruckTicketing.Client.Pages.TruckTickets.New;

public partial class NewTruckTicketStubRequestDialogForm : BaseTruckTicketingComponent
{
    private readonly NewTruckTicketStubRequestFormViewModel _viewModel = new();

    private dynamic _dialog;

    [Inject]
    private ITruckTicketService TruckTicketService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Inject]
    private IBlazorDownloadFileService FileDownloadService { get; set; }

    [Parameter]
    public EventCallback OnSuccess { get; set; }

    public async Task Open()
    {
        _viewModel.Response = null;
        _dialog = await DialogService.OpenAsync<NewTruckTicketStubRequestForm>("Create Pre-Printed Truck Ticket Stubs", new()
        {
            { nameof(NewTruckTicketStubRequestForm.ViewModel), _viewModel },
            { nameof(NewTruckTicketStubRequestForm.OnSubmit), new EventCallback<TruckTicketStubCreationRequest>(this, SubmitRequest) },
            { nameof(NewTruckTicketStubRequestForm.OnCancel), new EventCallback(this, CloseDialog) },
        }, new());
    }

    private async Task SubmitRequest(TruckTicketStubCreationRequest stubCreationRequest)
    {
        var response = await TruckTicketService.CreateTruckTicketStubs(stubCreationRequest);

        if (response.IsSuccessStatusCode)
        {
            if (stubCreationRequest.GeneratePdf)
            {
                await FileDownloadService.DownloadFile($"{stubCreationRequest.SiteId}-Ticket-Stubs-{DateTimeOffset.Now:yy-MM-dd-hh}.pdf", await response.HttpContent.ReadAsByteArrayAsync(),
                                                       MediaTypeNames.Application.Pdf);
            }

            if (OnSuccess.HasDelegate)
            {
                await OnSuccess.InvokeAsync();
            }

            NotificationService.Notify(NotificationSeverity.Success, "Successfully created truck ticket stubs.");
            CloseDialog();
        }
        else if (response.StatusCode != HttpStatusCode.BadRequest)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Failed to create truck ticket stubs.");
        }

        _viewModel.Response = response;
    }

    private void CloseDialog()
    {
        DialogService.Close(_dialog);
    }
}
