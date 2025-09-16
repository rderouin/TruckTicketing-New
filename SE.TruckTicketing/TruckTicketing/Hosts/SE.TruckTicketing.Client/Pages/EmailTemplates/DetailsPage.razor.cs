using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Contracts.Models.ContentGeneration;

using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.Client.Pages.EmailTemplates;

public partial class DetailsPage : BaseTruckTicketingComponent
{
    private bool _isLoading = true;

    private EmailTemplateDetailsViewModel _viewModel;

    [Inject]
    private IServiceProxyBase<EmailTemplate, Guid> EmailTemplateService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Parameter]
    public Guid? Id { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        _isLoading = true;

        var id = Id ?? Guid.Empty;
        var template = id != Guid.Empty ? await EmailTemplateService.GetById(id) : new();
        _viewModel = new(template ?? new());

        _isLoading = false;
    }

    private async Task HandleSubmit()
    {
        var response = _viewModel.IsNew
                           ? await EmailTemplateService.Create(_viewModel.EmailTemplate)
                           : await EmailTemplateService.Update(_viewModel.EmailTemplate);

        if (response.IsSuccessStatusCode)
        {
            NotificationService.Notify(NotificationSeverity.Success,
                                       "Success",
                                       "Email template saved successfully.");
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error,
                                       "Error",
                                       "An error occured while trying to save the email template.");
        }

        _viewModel.Response = response;
    }

    private void HandleCancel()
    {
        NavigationManager.NavigateTo(NavigationHistoryManager.GetReturnUrl());
    }
}
