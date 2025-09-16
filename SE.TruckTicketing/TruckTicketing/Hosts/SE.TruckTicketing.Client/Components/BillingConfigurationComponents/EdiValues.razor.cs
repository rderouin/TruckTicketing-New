using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;
using Radzen.Blazor;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.ViewModels;

using Trident.Mapper;
using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.BillingConfigurationComponents;

public partial class EDIValues : BaseRazorComponent
{
    private List<EDIValueViewModel> _ediViewModelData = new();

    private List<EDIValueViewModel> _ediViewModelUpdatedData = new();

    private bool isValid;

    protected RadzenTemplateForm<EDIValueViewModel> ReferenceToForm;

    private EDIValueViewModel model { get; set; } = new();

    [Parameter]
    public IEnumerable<EDIFieldDefinition> EDIFields { get; set; }

    [Parameter]
    public IEnumerable<EDIFieldValue> EDIFieldValues { get; set; }

    [Parameter]
    public EventCallback<List<EDIValueViewModel>> OnValueChange { get; set; }

    [Inject]
    private IMapperRegistry Mapper { get; set; }

    [Parameter]
    public EventCallback<List<EDIValueViewModel>> OnInvalidData { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        try
        {
            _ediViewModelData = EDIFields?.Select(x => new EDIValueViewModel
            {
                EDIFieldDefinitionId = x.Id,
                CustomerId = x.CustomerId,
                EDIFieldLookupId = x.EDIFieldLookupId,
                EDIFieldName = x.EDIFieldName,
                DefaultValue = x.DefaultValue,
                ValidationRequired = x.ValidationRequired,
                IsPrinted = x.IsPrinted,
                IsRequired = x.IsRequired,
                IsNew = x.IsNew,
                ValidationPattern = x.ValidationPattern,
                ValidationErrorMessage = x.ValidationErrorMessage,
            }).ToList();
        }
        catch (Exception e)
        {
            HandleException(e, nameof(EDIFieldValue), "An exception occurred getting claims in OnInitializedAsync");
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (EDIFieldValues.Any())
        {
            _ediViewModelUpdatedData = Mapper.Map<List<EDIValueViewModel>>(EDIFieldValues);
            foreach (var updatedData in _ediViewModelUpdatedData)
            {
                _ediViewModelData.Where(x => x.EDIFieldDefinitionId == updatedData.EDIFieldDefinitionId).Select(c =>
                                                                                                                {
                                                                                                                    c.Id = updatedData.Id;
                                                                                                                    c.EDIFieldValueContent = updatedData.IsNew
                                                                                                                        ? updatedData.DefaultValue
                                                                                                                        : updatedData.EDIFieldValueContent;

                                                                                                                    return c;
                                                                                                                }).ToList();
            }
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        ReferenceToForm.EditContext.Validate();
        await Task.CompletedTask;
    }

    public async Task OnChange(string value, string name)
    {
        isValid = ReferenceToForm.EditContext.Validate();
        await OnValueChange.InvokeAsync(_ediViewModelData);

        if (!isValid)
        {
            await OnInvalidData.InvokeAsync();
        }
    }

    public void OnSubmit(EDIValueViewModel model)
    {
    }

    public void OnInvalidSubmit(FormInvalidSubmitEventArgs arg)
    {
    }
}
