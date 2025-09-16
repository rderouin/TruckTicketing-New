using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.ViewModels.Accounts;

using Trident.UI.Blazor.Components;
using Trident.UI.Blazor.Components.Modals;

namespace SE.TruckTicketing.Client.Components.Accounts;

public partial class BillingConfigEDIFieldDefinitionEdit : BaseRazorComponent
{
    protected bool IsBusy;

    [Parameter]
    public BillingConfigEDIFieldValueDetailsViewModel ViewModel { get; set; }

    [Parameter]
    public List<EDIFieldDefinition> ExistingEDIFieldDefinitions { get; set; }

    [Parameter]
    public bool EditMode { get; set; }

    private Guid EDIValidationPatternLookupValue { get; set; }

    private bool ShowValidationPattern { get; set; }

    [Parameter]
    public EventCallback EDIFieldDefinitionChanged { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

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
            ViewModel.EdiFieldDefinition.ValidationRequired = false;
            ShowValidationPattern = false;
        }
    }

    private void UpdateEdiFieldInCollection(EDIFieldDefinition model)
    {
        var toUpdate = ExistingEDIFieldDefinitions.First(x => x.Id == model.Id);
        if (toUpdate != null)
        {
            toUpdate.EDIFieldName = model.EDIFieldName;
            toUpdate.EDIFieldLookupId = model.EDIFieldLookupId;
            toUpdate.ValidationRequired = model.ValidationRequired;
            toUpdate.DefaultValue = model.DefaultValue;
            toUpdate.IsPrinted = model.IsPrinted;
            toUpdate.ValidationRequired = model.ValidationRequired;
            toUpdate.ValidationPattern = model.ValidationPattern;
            toUpdate.ValidationErrorMessage = model.ValidationErrorMessage;
        }
    }

    public void OnEDIFieldChanged(EDIFieldLookup ediField)
    {
        ViewModel.EdiFieldDefinition.EDIFieldName = ediField?.Name;
        InvokeAsync(StateHasChanged);
    }

    public void OnEDIValidationPatternChanged(EDIValidationPatternLookup ediValidationPatternLookup)
    {
        ViewModel.EdiFieldDefinition.ValidationPattern = ediValidationPatternLookup.Pattern;
        ViewModel.EdiFieldDefinition.ValidationErrorMessage = ediValidationPatternLookup.ErrorMessage;
        EDIValidationPatternLookupValue = ediValidationPatternLookup.Id;
        InvokeAsync(StateHasChanged);
    }

    private Task SaveButton_Clicked()
    {
        if (ViewModel.EdiFieldDefinition.EDIFieldLookupId == default)
        {
            DialogService.Open<ErrorAlert>("Error", new()
            {
                { nameof(ErrorAlert.Response), "Field Name is Required" },
                { nameof(Application), Application },
            });

            return Task.CompletedTask;
        }

        if (ViewModel.EdiFieldDefinition.ValidationRequired)
        {
            if (string.IsNullOrEmpty(ViewModel.EdiFieldDefinition.ValidationPattern) || string.IsNullOrEmpty(ViewModel.EdiFieldDefinition.ValidationErrorMessage))
            {
                DialogService.Open<ErrorAlert>("Error", new()
                {
                    { nameof(ErrorAlert.Response), "Validation Pattern is Required" },
                    { nameof(Application), Application },
                });

                return Task.CompletedTask;
            }
        }

        if (!EditMode && ExistingEDIFieldDefinitions.Any())
        {
            if (ExistingEDIFieldDefinitions.Select(x => x.EDIFieldLookupId).Contains(ViewModel.EdiFieldDefinition.EDIFieldLookupId))
            {
                DialogService.Open<ErrorAlert>("Error", new()
                {
                    { nameof(ErrorAlert.Response), "Edi Field already exists" },
                    { nameof(Application), Application },
                });
            }
            else
            {
                ViewModel.EdiFieldDefinition.Id = Guid.NewGuid();
                ExistingEDIFieldDefinitions.Add(ViewModel.EdiFieldDefinition);
                EDIFieldDefinitionChanged.InvokeAsync(ViewModel.EdiFieldDefinition);
            }
        }
        else
        {
            if (EditMode)
            {
                UpdateEdiFieldInCollection(ViewModel.EdiFieldDefinition);
            }
            else
            {
                ViewModel.EdiFieldDefinition.Id = Guid.NewGuid();
                ExistingEDIFieldDefinitions.Add(ViewModel.EdiFieldDefinition);
            }

            EDIFieldDefinitionChanged.InvokeAsync(ViewModel.EdiFieldDefinition);
        }

        return Task.CompletedTask;
    }

    private async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }
}
