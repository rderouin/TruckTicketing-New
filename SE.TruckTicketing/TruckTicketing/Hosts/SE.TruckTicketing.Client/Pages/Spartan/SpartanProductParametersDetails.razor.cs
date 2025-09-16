using System;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;
using Radzen.Blazor;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Contracts.Api.Models.SpartanProductParameters;
using SE.TruckTicketing.Contracts.Models;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels.SpartanProductParameters;

using Trident.Api.Search;

namespace SE.TruckTicketing.Client.Pages.Spartan;

public partial class SpartanProductParametersDetails : BaseTruckTicketingComponent
{
    protected bool IsBusy;

    protected RadzenTemplateForm<SpartanProductParameter> ReferenceToForm;

    [Inject]
    private IServiceBase<SpartanProductParameter, Guid> SpartanProductParameterService { get; set; }

    [Parameter]
    public SpartanProductParameterDetailsViewModel ViewModel { get; set; }

    [Inject]
    private NotificationService notificationService { get; set; }

    [Parameter]
    public EventCallback<SpartanProductParameter> OnSubmit { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    private bool IsSaveEnabled { get; set; }

    private async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }
    protected void HandleLegalEntityLoading(SearchCriteriaModel criteria)
    {
        criteria.OrderBy = nameof(LegalEntity.Name);
        criteria.Filters[nameof(LegalEntity.ShowAccountsInTruckTicketing)] = true;
    }
    public void OnLegalEntityChange(LegalEntity legalEntity)
    {
        ViewModel.SpartanProductParameter.LegalEntity = legalEntity?.Name;
        IsSaveEnabled = !ReferenceToForm.EditContext.Validate();
        InvokeAsync(StateHasChanged);
    }

    public async Task OnChange()
    {
        IsSaveEnabled = !ReferenceToForm.EditContext.Validate();
        await Task.CompletedTask;
    }

    private async Task HandleSubmit()
    {
        IsBusy = true;
        var IsNew = ViewModel.SpartanProductParameter.Id == default;
        var response = ViewModel.SpartanProductParameter.Id == default
                           ? await SpartanProductParameterService.Create(ViewModel.SpartanProductParameter)
                           : await SpartanProductParameterService.Update(ViewModel.SpartanProductParameter);

        if (response.IsSuccessStatusCode)
        {
            notificationService.Notify(NotificationSeverity.Success,
                                       $"{ViewModel.SpartanProductParameter.ProductName} Spartan Product Parameter {(IsNew ? "created" : "updated")}.");

            DialogService.Close();
            await OnSubmit.InvokeAsync(ViewModel.SpartanProductParameter);
        }
        else if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            notificationService.Notify(NotificationSeverity.Error, "Failed to save spartan product parameter.");
        }

        ViewModel.Response = response;
        IsBusy = false;
    }

    private void onMinFluidChange()
    {
        ViewModel.SpartanProductParameter.MinFluidDensity = Math.Round(ViewModel.SpartanProductParameter.MinFluidDensity, 1);
    }

    private void onMaxFluidChange()
    {
        ViewModel.SpartanProductParameter.MaxFluidDensity = Math.Round(ViewModel.SpartanProductParameter.MaxFluidDensity, 1);
    }

    private void onMinPercentageChange()
    {
        ViewModel.SpartanProductParameter.MinWaterPercentage = Math.Round(ViewModel.SpartanProductParameter.MinWaterPercentage, 6);
    }

    private void onMaxPercentageChange()
    {
        ViewModel.SpartanProductParameter.MaxWaterPercentage = Math.Round(ViewModel.SpartanProductParameter.MaxWaterPercentage, 6);
    }
}
