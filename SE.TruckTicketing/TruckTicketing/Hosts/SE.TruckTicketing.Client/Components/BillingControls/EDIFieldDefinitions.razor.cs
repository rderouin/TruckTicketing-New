using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.ViewModels.EDIFieldDefinitions;
using Trident.Api.Search;
using Trident.Extensions;
using Trident.UI.Blazor.Components;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Components.BillingControls;

public partial class EDIFieldDefinitions : BaseRazorComponent, IDisposable
{
    private EDIFieldDefinitionDetailsViewModel _detailsViewModel;

    private SearchResultsModel<EDIFieldDefinition, SearchCriteriaModel> _EDIFieldDefinitionsList = new()
    {
        Info = new()
        {
            PageSize = 10,
        },
        Results = new List<EDIFieldDefinition>(),
    };

    protected PagableGridView<EDIFieldDefinition> grid;

    private bool IsGridLoading { get; set; }

    [Parameter]
    public List<EDIFieldDefinition> EDIFields { get; set; }

    [Parameter]
    public EventCallback<List<EDIFieldDefinition>> SubscriptionStateChanged { get; set; }

    private EventCallback UpdateEDIFieldsHandler =>
        new(this, (Action<List<EDIFieldDefinition>>)(async EDIFields =>
                                                     {
                                                         await SetEDIFieldsChanged(EDIFields);
                                                     }));

    private EventCallback HandleCancel => new(this, () => DialogService.Close());

    public override void Dispose()
    {
        DialogService.OnClose -= Close;
    }

    public async Task SetEDIFieldsChanged(List<EDIFieldDefinition> list)
    {
        EDIFields = list;
        _EDIFieldDefinitionsList.Results = EDIFields;
        _EDIFieldDefinitionsList.Info.TotalRecords = EDIFields.Count;
        await SubscriptionStateChanged.InvokeAsync(EDIFields);
        await grid.ReloadGrid();
    }

    private async Task EditButton_Click(EDIFieldDefinition model)
    {
        await OpenEditDialog(model);
    }

    private void LoadEDIFieldDefinitionsData(SearchCriteriaModel current)
    {
        _EDIFieldDefinitionsList = new()
        {
            Info = new() { PageSize = 20 },
            Results = EDIFields,
        };
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        _EDIFieldDefinitionsList.Results = EDIFields;
    }

    private async Task AddEdiFieldsButton_Click()
    {
        await OpenEditDialog(new() { Id = Guid.NewGuid() }, false);
    }

    private async Task OpenEditDialog(EDIFieldDefinition model, bool editMode = true)
    {
        IsGridLoading = true;
        _detailsViewModel = new(model?.Clone() ?? new EDIFieldDefinition());
        await DialogService.OpenAsync<EDIFieldDefinitionEdit>("EDI Field Definition",
                                                              new()
                                                              {
                                                                  { "ViewModel", _detailsViewModel },
                                                                  { "SelectedEDIFields", EDIFields },
                                                                  { "SubscriptionStateChanged", UpdateEDIFieldsHandler },
                                                                  { "EditMode", editMode },
                                                                  { nameof(EDIFieldDefinitionEdit.OnCancel), HandleCancel },
                                                              });

        IsGridLoading = false;
    }

    private void LoadData()
    {
        _EDIFieldDefinitionsList.Results = EDIFields;
        StateHasChanged();
    }

    private void Close(dynamic result)
    {
        if (result is not EDIFieldDefinition fieldDefinition)
        {
            return;
        }

        var resultList = _EDIFieldDefinitionsList.Results.ToList();
        var index = resultList.FindIndex(x => x.Id == fieldDefinition.Id);
        if (index >= 0)
        {
            resultList[index] = fieldDefinition;
        }
        else
        {
            resultList.Add(fieldDefinition);
            _EDIFieldDefinitionsList.Info.TotalRecords++;
        }

        _EDIFieldDefinitionsList.Results = resultList;
        EDIFields = _EDIFieldDefinitionsList.Results.ToList();
    }

    private async Task DeleteButton_Click(EDIFieldDefinition field)
    {
        const string msg = "Are you sure you want to delete this item?";
        const string title = "Delete EDI Field";
        var deleteConfirmed = await DialogService.Confirm(msg, title,
                                                          new()
                                                          {
                                                              OkButtonText = "Yes",
                                                              CancelButtonText = "No",
                                                          });

        if (deleteConfirmed.GetValueOrDefault())
        {
            var resultList = _EDIFieldDefinitionsList.Results.ToList();
            var index = resultList.FindIndex(x => x.Id == field.Id);
            resultList.RemoveAll(x => x.Id == field.Id);
            _EDIFieldDefinitionsList.Results = resultList;
            _EDIFieldDefinitionsList.Info.TotalRecords--;
            EDIFields = _EDIFieldDefinitionsList.Results.ToList();
            await SubscriptionStateChanged.InvokeAsync(EDIFields);
            await grid.ReloadGrid();
        }
    }
}
