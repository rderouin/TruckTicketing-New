using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Newtonsoft.Json;

using Radzen;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.Extensions;
using Trident.UI.Blazor.Components;
using Trident.UI.Blazor.Components.Grid;

using SortOrder = Trident.Contracts.Enums.SortOrder;

namespace SE.TruckTicketing.Client.Components.TruckTicketComponents;

public partial class VoidStatusReasonsGrid : BaseRazorComponent
{
    private const string SuccessSummary = "Success: ";

    private const string ErrrorSummary = "Error: ";

    public PagableGridView<TruckTicketVoidReason> _grid;

    private SearchResultsModel<TruckTicketVoidReason, SearchCriteriaModel> _results = new()
    {
        Info = new()
        {
            PageSize = 10,
        },
        Results = new List<TruckTicketVoidReason>(),
    };

    private TruckTicketVoidReason VoidReasonModel;

    [Inject]
    private IServiceBase<TruckTicketVoidReason, Guid> TruckTicketVoidReasonsService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    private EventCallback HandleCancel => new(this, () => DialogService.Close());

    private EventCallback<TruckTicketVoidReason> HandleValidSubmit => new(this, OnSubmit);

    private int? _pageSize { get; set; } = 10;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        try
        {
            await PerformGridSearchAsync(null);
        }
        catch (Exception e)
        {
            HandleException(e, nameof(TruckTicketVoidReason), "An exception occurred getting claims in OnInitializedAsync");
        }
    }

    private void ClickMessage(NotificationSeverity severity, string summary, string detailMessage)
    {
        NotificationService.Notify(new()
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
        current.OrderBy = $"{nameof(TruckTicketVoidReason.VoidReason)}";
        current.SortOrder = SortOrder.Desc;
        await PerformGridSearchAsync(current);
    }

    private async Task OnSubmit(TruckTicketVoidReason model)
    {
        await _grid.ReloadGrid();
    }

    private async Task OpenVoidReasonEditDialog(TruckTicketVoidReason model = null)
    {
        VoidReasonModel = model?.Clone() ?? new TruckTicketVoidReason();

        await DialogService.OpenAsync<VoidStatusReasonEdit>("Void Reason",
                                                            new()
                                                            {
                                                                { nameof(VoidStatusReasonEdit.Model), VoidReasonModel },
                                                                { nameof(VoidStatusReasonEdit.OnCancel), HandleCancel },
                                                                { nameof(VoidStatusReasonEdit.OnSubmit), HandleValidSubmit },
                                                            });
    }

    private async Task DeleteButton_Click(TruckTicketVoidReason model)
    {
        const string msg = "Are you sure you want to delete this Void Reason?";
        const string title = "Delete Reason";
        var deleteConfirmed = await DialogService.Confirm(msg, title,
                                                          new()
                                                          {
                                                              OkButtonText = "Delete",
                                                              CancelButtonText = "Cancel",
                                                          });

        if (deleteConfirmed.GetValueOrDefault())
        {
            model.IsDeleted = true;
            var response = await TruckTicketVoidReasonsService.Patch(model.Id, new Dictionary<string, object> { { nameof(model.IsDeleted), true } });

            if (response.StatusCode == HttpStatusCode.OK)
            {
                _results.Info.TotalRecords--;
                await LoadData(_results.Info);
                ClickMessage(NotificationSeverity.Success, SuccessSummary, "Successfully Deleted Reason");
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
        current ??= new()
        {
            PageSize = _pageSize,
            CurrentPage = 0,
            Keywords = "",
            OrderBy = $"{nameof(TruckTicketVoidReason.VoidReason)}",
            SortOrder = SortOrder.Desc,
            Filters = new(),
        };

        _results = await TruckTicketVoidReasonsService.Search(current);
    }
}
