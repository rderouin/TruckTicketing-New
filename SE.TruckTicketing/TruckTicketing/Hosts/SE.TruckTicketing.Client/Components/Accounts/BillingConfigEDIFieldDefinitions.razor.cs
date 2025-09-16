using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.ViewModels.Accounts;

using Trident.Api.Search;
using Trident.Extensions;
using Trident.UI.Blazor.Components;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Components.Accounts;

public partial class BillingConfigEDIFieldDefinitions : BaseRazorComponent
{
    private BillingConfigEDIFieldValueDetailsViewModel _detailsViewModel;

    private SearchResultsModel<EDIFieldValue, SearchCriteriaModel> _EDIFieldValuesList = new()
    {
        Info = new()
        {
            PageSize = 10,
        },
        Results = new List<EDIFieldValue>(),
    };

    protected PagableGridView<EDIFieldValue> grid;

    private bool IsGridLoading { get; set; }

    [Parameter]
    public List<EDIFieldValue> EDIFields { get; set; }

    [Parameter]
    public List<EDIFieldDefinition> EDIFieldDefinitions { get; set; }

    [Parameter]
    public Account Account { get; set; }

    private EventCallback UpdateEDIFieldDefinitionHandler =>
        new(this, (Action<EDIFieldDefinition>)(async UpdatedEDIFieldDefinitions =>
                                               {
                                                   await SetEDIFieldValueForFieldDefiniton(UpdatedEDIFieldDefinitions);
                                                   DialogService.Close();
                                               }));

    private EventCallback HandleCancel => new(this, () => DialogService.Close());

    protected override async Task OnParametersSetAsync()
    {
        await LoadEDIFieldValuesData();
        await base.OnParametersSetAsync();
    }

    private async Task LoadEDIFieldValuesData()
    {
        _EDIFieldValuesList = new(EDIFields);
        _EDIFieldValuesList.Info.TotalRecords = EDIFields.Count;
        await Task.CompletedTask;
    }

    public async Task SetEDIFieldValueForFieldDefiniton(EDIFieldDefinition ediFieldDefinition)
    {
        if (EDIFields.Any(x => x.EDIFieldDefinitionId == ediFieldDefinition.Id))
        {
            EDIFields.First(x => x.EDIFieldDefinitionId == ediFieldDefinition.Id)
                     .EDIFieldValueContent = _detailsViewModel.EDIFieldValue;

            EDIFields.First(x => x.EDIFieldDefinitionId == ediFieldDefinition.Id)
                     .EDIFieldName = ediFieldDefinition.EDIFieldName;
        }
        else
        {
            ediFieldDefinition.Id = Guid.NewGuid();
            ediFieldDefinition.DefaultValue = _detailsViewModel.EDIFieldValue;
            EDIFields.Add(new()
            {
                Id = Guid.NewGuid(),
                EDIFieldDefinitionId = ediFieldDefinition.Id,
                EDIFieldName = ediFieldDefinition.EDIFieldName,
                EDIFieldValueContent = _detailsViewModel.EDIFieldValue,
            });
        }

        _EDIFieldValuesList.Results = EDIFields;
        _EDIFieldValuesList.Info.TotalRecords = EDIFields.Count;
        await grid.ReloadGrid();
    }

    private async Task EditButton_Click(EDIFieldValue ediFieldValue)
    {
        await OpenEditDialog(ediFieldValue);
    }

    private async Task AddEdiFieldsButton_Click()
    {
        await OpenEditDialog(new(), false);
    }

    private async Task OpenEditDialog(EDIFieldValue ediFieldValue, bool editMode = true)
    {
        IsGridLoading = true;
        var model = ediFieldValue.Id == default ? null : EDIFieldDefinitions.FirstOrDefault(x => x.Id == ediFieldValue.EDIFieldDefinitionId);
        _detailsViewModel = new(model?.Clone() ?? new EDIFieldDefinition { LegalEntity = Account.LegalEntity }) { EDIFieldValue = editMode ? ediFieldValue.EDIFieldValueContent : String.Empty };
        await DialogService.OpenAsync<BillingConfigEDIFieldDefinitionEdit>("EDI Field Definition",
                                                                           new()
                                                                           {
                                                                               { nameof(BillingConfigEDIFieldDefinitionEdit.ViewModel), _detailsViewModel },
                                                                               { nameof(BillingConfigEDIFieldDefinitionEdit.ExistingEDIFieldDefinitions), EDIFieldDefinitions },
                                                                               { nameof(BillingConfigEDIFieldDefinitionEdit.EDIFieldDefinitionChanged), UpdateEDIFieldDefinitionHandler },
                                                                               { nameof(BillingConfigEDIFieldDefinitionEdit.EditMode), editMode },
                                                                               { nameof(BillingConfigEDIFieldDefinitionEdit.OnCancel), HandleCancel },
                                                                           });

        IsGridLoading = false;
    }

    private async Task DeleteButton_Click(EDIFieldValue field)
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
            EDIFields.RemoveAll(x => x.Id == field.Id);
            EDIFieldDefinitions.RemoveAll(x => x.Id == field.EDIFieldDefinitionId);
            _EDIFieldValuesList.Results = EDIFields;
            _EDIFieldValuesList.Info.TotalRecords = EDIFields.Count;
            await grid.ReloadGrid();
        }
    }
}
