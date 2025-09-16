using System;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;

using BlazorDownloadFile;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Client.Pages.TruckTickets.New;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

namespace SE.TruckTicketing.Client.Pages.TruckTickets;

public class NewTruckTicketStubRequestDialog : BaseTruckTicketingComponent
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
        _dialog = await DialogService.OpenAsync<TruckTicketStubsCreationDialog>("Create Pre-Printed Truck Tickets", new()
        {
            { nameof(TruckTicketStubsCreationDialog.ViewModel), _viewModel },
            { nameof(TruckTicketStubsCreationDialog.OnSubmit), new EventCallback<TruckTicketStubCreationRequest>(this, SubmitRequest) },
            { nameof(TruckTicketStubsCreationDialog.OnCancel), new EventCallback(this, CloseDialog) },
        });
    }

    private async Task SubmitRequest(TruckTicketStubCreationRequest stubCreationRequest)
    {
        var response = await TruckTicketService.CreateTruckTicketStubs(stubCreationRequest);

        if (response.IsSuccessStatusCode)
        {
            if (stubCreationRequest.GeneratePdf)
            {
                await FileDownloadService.DownloadFile($"ticket-stubs-{DateTimeOffset.Now:yy-MM-dd-hh}.pdf", await response.HttpContent.ReadAsByteArrayAsync(), MediaTypeNames.Application.Pdf);
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
