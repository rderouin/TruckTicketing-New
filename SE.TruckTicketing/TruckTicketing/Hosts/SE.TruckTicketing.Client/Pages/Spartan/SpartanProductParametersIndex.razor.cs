using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Newtonsoft.Json;

using Radzen;

using SE.Shared.Common.Utilities;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Client.Components.GridFilters;
using SE.TruckTicketing.Contracts.Api.Models.SpartanProductParameters;
using SE.TruckTicketing.Contracts.Constants.SpartanProductParameters;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels.SpartanProductParameters;

using Trident.Api.Search;
using Trident.Extensions;
using Trident.UI.Blazor.Components.Grid;
using Trident.UI.Blazor.Components.Grid.Filters;

using SortOrder = Trident.Contracts.Enums.SortOrder;

namespace SE.TruckTicketing.Client.Pages.Spartan;

public partial class SpartanProductParametersIndex : BaseTruckTicketingComponent
{
    private const string SuccessSummary = "Success: ";

    private const string ErrrorSummary = "Error: ";

    private readonly SearchResultsModel<SpartanProductParameter, SearchCriteriaModel> _results = new()
    {
        Info = new()
        {
            PageSize = 10,
        },
        Results = new List<SpartanProductParameter>(),
    };

    private SpartanProductParameterDetailsViewModel _detailsViewModel;

    public PagableGridView<SpartanProductParameter> _grid;

    private GridFiltersContainer _gridFilterContainer;

    [Inject]
    public NotificationService NotificationService { get; set; }

    [Inject]
    private IServiceBase<SpartanProductParameter, Guid> SpartanProductParameterService { get; set; }

    [Inject]
    private NotificationService notificationService { get; set; }

    private EventCallback HandleCancel => new(this, () => DialogService.Close());

    private EventCallback<SpartanProductParameter> HandleValidSubmit => new(this, OnSubmit);

    private int? _pageSize { get; set; } = 10;

    private Dictionary<string, FilterOptions> GridFilterOptionDict { get; set; } = new();

    private async Task UpdateIsActiveState(SpartanProductParameter spartanProductParameter, bool isActive)
    {
        var response = await SpartanProductParameterService.Patch(spartanProductParameter.Id, new Dictionary<string, object> { { nameof(spartanProductParameter.IsActive), isActive } });

        if (response.IsSuccessStatusCode)
        {
            NotificationService.Notify(NotificationSeverity.Success, detail: "Status change successful.");
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, detail: "Status change unsuccessful.");
            spartanProductParameter.IsActive = !isActive;
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _gridFilterContainer.Reload();
        }
    }

    private void ClickMessage(NotificationSeverity severity, string summary, string detailMessage)
    {
        notificationService.Notify(new()
        {
            Severity = severity,
            Summary = summary,
            Detail = detailMessage,
            Duration = 4000,
        });
    }

    private async Task LoadData(SearchCriteriaModel current)
    {
        _pageSize = current.PageSize;
        current.OrderBy = $"{nameof(SpartanProductParameter.ProductName)}";
        current.SortOrder = SortOrder.Desc;
        await PerformGridSearchAsync(current);
    }

    private async Task OnSubmit(SpartanProductParameter model)
    {
        await _grid.ReloadGrid();
    }

    private async Task OpenSpartanProductParameterDetailsDialog(SpartanProductParameter model = null)
    {
        _detailsViewModel = new(model?.Clone() ?? new SpartanProductParameter());

        await DialogService.OpenAsync<SpartanProductParametersDetails>(_detailsViewModel.Title,
                                                                       new()
                                                                       {
                                                                           { nameof(SpartanProductParametersDetails.ViewModel), _detailsViewModel },
                                                                           { nameof(SpartanProductParametersDetails.OnCancel), HandleCancel },
                                                                           { nameof(SpartanProductParametersDetails.OnSubmit), HandleValidSubmit },
                                                                       });
    }

    private async Task DeleteButton_Click(SpartanProductParameter model)
    {
        const string msg = "Are you sure you want to delete this parameter?";
        const string title = "Delete Parameter";
        var deleteConfirmed = await DialogService.Confirm(msg, title,
                                                          new()
                                                          {
                                                              OkButtonText = "Delete",
                                                              CancelButtonText = "Cancel",
                                                          });

        if (deleteConfirmed.GetValueOrDefault())
        {
            var response = await SpartanProductParameterService.Patch(model.Id, new Dictionary<string, object> { { nameof(model.IsDeleted), true } });
            if (response.StatusCode == HttpStatusCode.OK)
            {
                _results.Info.TotalRecords--;
                await LoadData(_results.Info);
                ClickMessage(NotificationSeverity.Success, SuccessSummary, "Successfully Deleted Parameter");
            }
            else
            {
                //Pass Empty to ErrorAlert Component when no ValidationErrro received from response
                var json = response.ValidationErrors == null ? string.Empty : JsonConvert.SerializeObject(response.ValidationErrors);
                ClickMessage(NotificationSeverity.Error, ErrrorSummary, json);
            }
        }
    }

    private async Task PerformGridSearchAsync(SearchCriteriaModel current)
    {
        if (current == null)
        {
            current = new()
            {
                PageSize = _pageSize,
                CurrentPage = 0,
                Keywords = "",
                OrderBy = $"{nameof(SpartanProductParameter.ProductName)}",
                SortOrder = SortOrder.Desc,
                Filters = new(),
            };
        }

        ApplyDefaultFilters(ref current);
        var results = await SpartanProductParameterService.Search(current);
        _results.Results = results == null || !results.Results.Any() ? new List<SpartanProductParameter>() : results.Results;
        _results.Info = results == null || results.Info == null ? new() : results.Info;
    }

    private void ApplyDefaultFilters(ref SearchCriteriaModel current)
    {
        current.Filters.TryAdd(nameof(SpartanProductParameter.IsDeleted), false);
        current.Filters.TryAdd("EntityType", "SpartanProductParameter");
    }

    private void ConfigureFilters()
    {
        GridFilterOptionDict = new()
        {
            //NumericFilter
            {
                nameof(SpartanProductParameter.MinFluidDensity), new DoubleFilterOptions
                {
                    FilterGroupNumber = 1,
                    Label = "Fluid Density (Min)",
                    FilterPath = $"{nameof(SpartanProductParameter.MinFluidDensity)}",
                    BoundType = ValueBoundOptions.Both,
                    LowerBoundOperator = CompareOperators.gte,
                    UpperBoundOperator = CompareOperators.lte,
                }
            },
            {
                nameof(SpartanProductParameter.MaxFluidDensity), new DoubleFilterOptions
                {
                    FilterGroupNumber = 1,
                    Label = "Fluid Density (Max)",
                    FilterPath = $"{nameof(SpartanProductParameter.MaxFluidDensity)}",
                    BoundType = ValueBoundOptions.Both,
                    LowerBoundOperator = CompareOperators.gte,
                    UpperBoundOperator = CompareOperators.lte,
                }
            },
            {
                nameof(SpartanProductParameter.MinWaterPercentage), new DoubleFilterOptions
                {
                    FilterGroupNumber = 1,
                    Label = "Water % (Min)",
                    FilterPath = $"{nameof(SpartanProductParameter.MinWaterPercentage)}",
                    BoundType = ValueBoundOptions.Both,
                    LowerBoundOperator = CompareOperators.gte,
                    UpperBoundOperator = CompareOperators.lte,
                }
            },
            {
                nameof(SpartanProductParameter.MaxWaterPercentage), new DoubleFilterOptions
                {
                    FilterGroupNumber = 1,
                    Label = "Water % (Max)",
                    FilterPath = $"{nameof(SpartanProductParameter.MaxWaterPercentage)}",
                    BoundType = ValueBoundOptions.Both,
                    LowerBoundOperator = CompareOperators.gte,
                    UpperBoundOperator = CompareOperators.lte,
                }
            },
            //DropDownDataGridFilter
            {
                nameof(SpartanProductParameter.LocationOperatingStatus), new DropDownFilterOptions
                {
                    FilterGroupNumber = 1,
                    Label = "Location Operating Status",
                    FilterPath = nameof(SpartanProductParameter.LocationOperatingStatus),
                    Operator = CompareOperators.contains,
                    ListItems = DataDictionary.For<LocationOperatingStatus>().Select(x =>
                                                                                         new ListOption<string>
                                                                                         {
                                                                                             Display = x.Value,
                                                                                             Value = x.Value,
                                                                                         }).ToList(),
                }
            },
            //CheckboxFilter
            {
                nameof(SpartanProductParameter.ShowDensity), new DropDownFilterOptions
                {
                    FilterGroupNumber = 2,
                    Label = "Show Density",
                    FilterPath = nameof(SpartanProductParameter.ShowDensity),
                    Operator = CompareOperators.contains,
                    ListItems = new()
                    {
                        new()
                        {
                            Value = "true",
                            Display = "Yes",
                        },
                        new()
                        {
                            Value = "false",
                            Display = "No",
                        },
                    },
                }
            },
        };
    }
}
