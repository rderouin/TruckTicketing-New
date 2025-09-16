using Microsoft.AspNetCore.Components;

using SE.BillingService.Contracts.Api.Models;

namespace SE.TruckTicketing.Client.Components.InvoiceExchange;

public partial class FormatOverride
{
    [Parameter]
    public InvoiceExchangeMessageFieldMappingDto Model { get; set; }

    [Parameter]
    public EventCallback ModelUpdated { get; set; }

    private void NotifyParent()
    {
        ModelUpdated.InvokeAsync();
    }
}
