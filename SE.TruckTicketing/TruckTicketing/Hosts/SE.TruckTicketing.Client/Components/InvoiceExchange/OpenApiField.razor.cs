using System.Collections.Generic;

using Microsoft.AspNetCore.Components;

using SE.BillingService.Contracts.Api.Models;

namespace SE.TruckTicketing.Client.Components.InvoiceExchange;

public partial class OpenApiField
{
    [Parameter]
    public InvoiceExchangeMessageFieldMappingDto Model { get; set; }

    [Parameter]
    public IList<ValueFormatDto> FormatsSource { get; set; }

    [Parameter]
    public bool ShowFormatSelection { get; set; }
}
