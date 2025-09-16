using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.SourceLocations;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels.Accounts;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;
using Trident.Extensions;
using Trident.UI.Blazor.Components;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Components.Accounts;

public partial class GeneratorSourceLocationStep : BaseRazorComponent
{
    private readonly SearchResultsModel<SourceLocation, SearchCriteriaModel> _sourceLocations = new()
    {
        Info = new() { PageSize = 10 },
        Results = new List<SourceLocation>(),
    };

    private GeneratorSourceLocationDetailsViewModel _detailsViewModel;

    private PagableGridView<SourceLocation> _grid;

    private bool _isCreateNewSourceLocationButtonDiabled;

    [Parameter]
    public List<AccountContact> AccountContacts { get; set; }

    [Parameter]
    public Account Account { get; set; }

    [Parameter]
    public List<SourceLocation> SourceLocations { get; set; }

    [Inject]
    private IServiceProxyBase<SourceLocation, Guid> SourceLocationService { get; set; }

    [Inject]
    private INewAccountService NewAccountService { get; set; }

    [Parameter]
    public EventCallback<SourceLocation> AddSourceLocation { get; set; }

    [Parameter]
    public EventCallback<SourceLocation> DeleteSourceLocation { get; set; }

    [Parameter]
    public EventCallback SourceLocationWorkflowValidationResponse { get; set; }

    [Parameter]
    public EventCallback SourceLocationValidationSuccessful { get; set; }

    protected override async void OnParametersSet()
    {
        await LoadSourceLocation();
        base.OnParametersSet();
    }

    private async Task LoadSourceLocation()
    {
        _sourceLocations.Results = new List<SourceLocation>(SourceLocations);
        if (SourceLocations.Count > 0)
        {
            foreach (var sourceLocation in SourceLocations)
            {
                if (!sourceLocation.IsValidated)
                {
                    var response = await NewAccountService.SourceLocationWorkflowValidation(sourceLocation);
                    if (!response.IsSuccessStatusCode)
                    {
                        sourceLocation.IsValidated = true;
                        _isCreateNewSourceLocationButtonDiabled = true;
                        await SourceLocationWorkflowValidationResponse.InvokeAsync(response);
                        break;
                    }

                    await SourceLocationValidationSuccessful.InvokeAsync(true);
                    sourceLocation.IsValidated = true;
                    _isCreateNewSourceLocationButtonDiabled = false;
                }
            }
        }
        else
        {
            _isCreateNewSourceLocationButtonDiabled = false;
        }

        _sourceLocations.Info.TotalRecords = SourceLocations.Count();
        await Task.CompletedTask;
    }

    private async Task OpenSourceLocationDetailsDialog(SourceLocation model = null)
    {
        _detailsViewModel = new(model?.Clone() ?? new SourceLocation
        {
            IsActive = true,
            GeneratorId = Account.Id,
            GeneratorName = Account.Name,
            GeneratorProductionAccountContactId = Account.Contacts.Where(x => x.IsPrimaryAccountContact &&
                                                                              x.ContactFunctions.Contains(AccountContactFunctions.ProductionAccountant.ToString()))
                                                         .FirstOrDefault(new AccountContact()).Id,
        });

        await DialogService.OpenAsync<GeneratorSourceLocationDetails>(_detailsViewModel.Title,
                                                                      new()
                                                                      {
                                                                          { nameof(GeneratorSourceLocationDetails.ViewModel), _detailsViewModel },
                                                                          { nameof(GeneratorSourceLocationDetails.OnCancel), new EventCallback(this, () => DialogService.Close()) },
                                                                          { nameof(GeneratorSourceLocationDetails.OnSubmit), new EventCallback<SourceLocation>(this, OnSubmit) },
                                                                          { nameof(GeneratorSourceLocationDetails.Account), Account },
                                                                      },
                                                                      new()
                                                                      {
                                                                          Width = "60%",
                                                                      });
    }

    private async Task OnSubmit(SourceLocation model)
    {
        await AddSourceLocation.InvokeAsync(model);
        DialogService.Close();
    }

    private async Task DeleteButton_Click(SourceLocation model)
    {
        const string msg = "Are you sure you want to delete this item?";
        const string title = "Delete Source Location";
        var deleteConfirmed = await DialogService.Confirm(msg, title,
                                                          new()
                                                          {
                                                              OkButtonText = "Yes",
                                                              CancelButtonText = "No",
                                                          });

        if (deleteConfirmed.GetValueOrDefault())
        {
            await DeleteSourceLocation.InvokeAsync(model);
        }
    }
}
