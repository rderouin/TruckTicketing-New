using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.Extensions;
using Trident.UI.Blazor.Components;
using Trident.UI.Blazor.Components.Grid;

using SortOrder = Trident.Contracts.Enums.SortOrder;

namespace SE.TruckTicketing.Client.Components.TruckTicketComponents;

public partial class HoldStatusReasonsGrid : BaseRazorComponent
{
    private readonly SearchResultsModel<TruckTicketHoldReason, SearchCriteriaModel> _results = new()
    {
        Info = new()
        {
            PageSize = 10,
        },
        Results = new List<TruckTicketHoldReason>(),
    };

    public PagableGridView<TruckTicketHoldReason> _grid;

    private TruckTicketHoldReason HoldReasonModel;

    [Inject]
    private IServiceBase<TruckTicketHoldReason, Guid> TruckTicketHoldReasonsService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    private EventCallback HandleCancel => new(this, () => DialogService.Close());

    private EventCallback<TruckTicketHoldReason> HandleValidSubmit => new(this, OnSubmit);

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
            HandleException(e, nameof(TruckTicketHoldReason), "An exception occurred in OnInitializedAsync");
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
        current.OrderBy = $"{nameof(TruckTicketHoldReason.HoldReason)}";
        current.SortOrder = SortOrder.Desc;
        await PerformGridSearchAsync(current);
    }

    private async Task OnSubmit(TruckTicketHoldReason model)
    {
        await _grid.ReloadGrid();
    }

    private async Task OpenHoldReasonEditDialog(TruckTicketHoldReason model = null)
    {
        HoldReasonModel = model?.Clone() ?? new TruckTicketHoldReason();

        await DialogService.OpenAsync<HoldStatusReasonEdit>("Hold Reason",
                                                            new()
                                                            {
                                                                { nameof(HoldStatusReasonEdit.Model), HoldReasonModel },
                                                                { nameof(HoldStatusReasonEdit.OnCancel), HandleCancel },
                                                                { nameof(VoidStatusReasonEdit.OnSubmit), HandleValidSubmit },
                                                            });
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
                OrderBy = $"{nameof(TruckTicketHoldReason.HoldReason)}",
                SortOrder = SortOrder.Desc,
                Filters = new(),
            };
        }

        var results = await TruckTicketHoldReasonsService.Search(current);
        _results.Results = results == null || !results.Results.Any() ? new List<TruckTicketHoldReason>() : results.Results;
        _results.Info = results == null || results.Info == null ? new() : results.Info;
    }
}
