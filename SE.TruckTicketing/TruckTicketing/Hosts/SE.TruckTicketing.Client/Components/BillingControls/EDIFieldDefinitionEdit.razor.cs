using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels.EDIFieldDefinitions;

using Trident.Contracts.Api.Client;
using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.BillingControls;

public partial class EDIFieldDefinitionEdit : BaseRazorComponent
{
    private bool _isSaving;

    private bool ShowValidationPattern { get; set; }

    private bool IsEdiFieldDropDownDisabled => ViewModel.EdiFieldDefinition.Id != default;

    [Parameter]
    public EDIFieldDefinitionDetailsViewModel ViewModel { get; set; }

    [Parameter]
    public bool IsNewRecord { get; set; }

    [Parameter]
    public EventCallback<EDIFieldDefinition> AddEDIFieldDefinition { get; set; }

    [Parameter]
    public EventCallback<EDIFieldDefinition> UpdateEDIFieldDefinition { get; set; }

    [Inject]
    private IServiceBase<EDIFieldDefinition, Guid> EDIFieldDefinitionService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    private void OnEDIFieldChanged(EDIFieldLookup ediField)
    {
        ViewModel.EdiFieldDefinition.EDIFieldName = ediField?.Name;
        InvokeAsync(StateHasChanged);
    }

    private void OnEDIValidationPatternChanged(EDIValidationPatternLookup ediValidationPatternLookup)
    {
        ViewModel.EdiFieldDefinition.ValidationPatternId = ediValidationPatternLookup.Id;
        ViewModel.EdiFieldDefinition.ValidationPattern = ediValidationPatternLookup.Pattern;
        ViewModel.EdiFieldDefinition.ValidationErrorMessage = ediValidationPatternLookup.ErrorMessage;
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        ShowValidationPattern = ViewModel.EdiFieldDefinition.ValidationRequired;
    }

    private void ValidationOnChange(bool? value)
    {
        if (value == true)
        {
            ViewModel.EdiFieldDefinition.ValidationRequired = true;
            ShowValidationPattern = true;
        }
        else
        {
            ViewModel.EdiFieldDefinition.ValidationPattern = null;
            ViewModel.EdiFieldDefinition.ValidationErrorMessage = null;
            ViewModel.EdiFieldDefinition.ValidationPatternId = Guid.Empty;
            ViewModel.EdiFieldDefinition.ValidationRequired = false;
            ShowValidationPattern = false;
        }
    }

    private async Task SaveButton_Clicked()
    {
        _isSaving = true;

        Response<EDIFieldDefinition> response;

        if (IsNewRecord)
        {
            response = await EDIFieldDefinitionService.Create(ViewModel.EdiFieldDefinition);
            if (response.IsSuccessStatusCode)
            {
                await AddEDIFieldDefinition.InvokeAsync(ViewModel.EdiFieldDefinition);
            }
        }
        else
        {
            response = await EDIFieldDefinitionService.Update(ViewModel.EdiFieldDefinition);
            if (response.IsSuccessStatusCode)
            {
                await UpdateEDIFieldDefinition.InvokeAsync(ViewModel.EdiFieldDefinition);
            }
        }

        if (response.IsSuccessStatusCode)
        {
            NotificationService.Notify(NotificationSeverity.Success, detail: ViewModel.SubmitSuccessNotificationMessage);
        }

        ViewModel.Response = response;

        _isSaving = false;
    }

    private async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }
}
