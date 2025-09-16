using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen.Blazor;

using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;
using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.BillingControls;

public partial class EDIValueEditor : BaseRazorComponent
{
    private Guid? _customerId;

    private SearchResultsModel<EDIFieldDefinition, SearchCriteriaModel> _ediFieldDefinitions = new();

    private IEnumerable<EdiFieldDefinitionValue> _ediFieldDefinitionValues = Array.Empty<EdiFieldDefinitionValue>();

    private RadzenTemplateForm<EdiFieldDefinitionValue> _form;

    private EdiFieldDefinitionValue _model = new();

    protected bool IsLoading;

    public bool IsValid { get; private set; }

    [Parameter]
    public Guid? CustomerId { get; set; }

    [Parameter]
    public bool IsDisabled { get; set; }

    [Parameter]
    public List<EDIFieldValue> Data { get; set; } = new();

    [Inject]
    private IServiceProxyBase<EDIFieldDefinition, Guid> EdiFieldDefinitionService { get; set; }

    [Parameter]
    public EventCallback<List<EDIFieldValue>> OnChange { get; set; }

    protected async Task HandleEdiValueChange(string value, EDIFieldDefinition fieldDefinition)
    {
        var _ = _form.EditContext.Validate();
        var ediFieldValue = Data.FirstOrDefault(e => e.EDIFieldDefinitionId == fieldDefinition.Id);

        if (ediFieldValue is null)
        {
            ediFieldValue = new()
            {
                EDIFieldDefinitionId = fieldDefinition.Id,
                EDIFieldName = fieldDefinition.EDIFieldName,
            };

            Data.Add(ediFieldValue);
        }

        ediFieldValue.EDIFieldValueContent = value;
        SetInternalEdiValues();
        Validate();

        if (OnChange.HasDelegate)
        {
            await OnChange.InvokeAsync(_ediFieldDefinitionValues.Select(e => e.Value).ToList());
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_customerId != CustomerId)
        {
            _customerId = CustomerId;
            await LoadEdiFieldDefinitions();
        }

        SetInternalEdiValues();
        Validate();
    }

    private async Task LoadEdiFieldDefinitions()
    {
        IsLoading = true;

        var searchCriteria = new SearchCriteriaModel
        {
            PageSize = 100,
        };

        searchCriteria.AddFilter(nameof(EDIFieldDefinition.CustomerId), CustomerId);
        _ediFieldDefinitions = CustomerId is null
                                   ? new()
                                   : await EdiFieldDefinitionService.Search(searchCriteria) ?? _ediFieldDefinitions;

        IsLoading = false;
    }

    private EDIFieldValue EdiValueFor(Guid fieldDefinitionId)
    {
        return Data.FirstOrDefault(e => e.EDIFieldDefinitionId == fieldDefinitionId) ?? new();
    }

    private void SetInternalEdiValues()
    {
        _ediFieldDefinitionValues = IsDisabled
                                        ? Data.Select(e => new EdiFieldDefinitionValue
                                        {
                                            FieldDefinition = new()
                                            {
                                                Id = e.EDIFieldDefinitionId,
                                                EDIFieldName = e.EDIFieldName,
                                            },
                                            Value = e,
                                        }).ToArray()
                                        : _ediFieldDefinitions.Results.Select(e => new EdiFieldDefinitionValue
                                        {
                                            FieldDefinition = e,
                                            Value = EdiValueFor(e.Id),
                                        }).ToArray();
    }

    private void Validate()
    {
        var ediDefinitionValueMap = _ediFieldDefinitionValues.ToDictionary(def => def.FieldDefinition.Id, def => def.Value);
        var isValid = _ediFieldDefinitions.Results.Aggregate(true, (current, ediFieldDefinition) => current && ValidateEdi(ediFieldDefinition, ediDefinitionValueMap));
        IsValid = isValid;
    }

    private bool ValidateEdi(EDIFieldDefinition fieldDefinition, Dictionary<Guid, EDIFieldValue> ediDefinitionValueMap)
    {
        ediDefinitionValueMap.TryGetValue(fieldDefinition.Id, out var value);
        var valueString = value?.EDIFieldValueContent ?? string.Empty;
        if (fieldDefinition.IsRequired && !valueString.HasText())
        {
            return false;
        }

        try
        {
            if (fieldDefinition.ValidationRequired && !Regex.IsMatch(valueString, fieldDefinition.ValidationPattern))
            {
                return false;
            }
        }
        catch (ArgumentException)
        {
            return false;
        }

        return true;
    }

    private sealed class EdiFieldDefinitionValue
    {
        public EDIFieldDefinition FieldDefinition { get; set; }

        public EDIFieldValue Value { get; set; }
    }
}
