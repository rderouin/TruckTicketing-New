using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.SalesManagement;

public partial class SalesLinePriceChangeDialog : BaseRazorComponent
{
    private bool CommentsRequired => ChangeReason == SalesLinePriceChangeReason.Other;

    private string RequiredClass => CommentsRequired ? "required" : string.Empty;

    [Parameter]
    public IEnumerable<SalesLine> SalesLines { get; set; }

    [Parameter]
    public Double OriginalRate { get; set; }

    [Parameter]
    public Double? NewRate { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; } = EventCallback.Empty;

    [Parameter]
    public EventCallback OnSubmit { get; set; } = EventCallback.Empty;

    [Parameter]
    public EventCallback ParentStateHasChanged { get; set; } = EventCallback.Empty;

    [Parameter]
    public bool IsMultiSelect { get; set; }

    private string ChangeComments { get; set; }

    private SalesLinePriceChangeReason ChangeReason { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
    }

    private async Task HandleSubmit()
    {
        foreach (var salesLine in SalesLines)
        {
            //10828 - Allow update Rate for Cut Lines

            if (NewRate is null or < 0)
            {
                continue;
            }

            salesLine.OriginalRate = salesLine.Rate;
            salesLine.ChangeReason = ChangeReason;
            salesLine.ChangeComments = ChangeComments;
            salesLine.IsRateOverridden = true;
            salesLine.Rate = Math.Round((double)NewRate, 2);
            salesLine.TotalValue = Math.Round((double)(NewRate * salesLine.Quantity), 2);
            salesLine.PriceChangeDate = DateTimeOffset.Now;
            salesLine.PriceChangeUserName = Application?.User?.Principal?.Identity?.Name;
        }

        await ParentStateHasChanged.InvokeAsync();
        DialogService.Close();
    }

    private async Task HandleCancelPriceChange()
    {
        if (!IsMultiSelect && OriginalRate != 0)
        {
            foreach (var salesLine in SalesLines)
            {
                salesLine.Rate = OriginalRate;
            }
        }

        await OnCancel.InvokeAsync();
        DialogService.Close();
        await ParentStateHasChanged.InvokeAsync();
    }

    // TODO: coming in future PR with Frontend TruckTicket Refactor
    // private async Task<bool> IsFieldTicketAndSentToOpenInvoice()
    // {
    //     if (SalesLine.LoadConfirmationId == null || SalesLine.LoadConfirmationId == Guid.Empty)
    //     {
    //         return false;
    //     }
    // }
}
