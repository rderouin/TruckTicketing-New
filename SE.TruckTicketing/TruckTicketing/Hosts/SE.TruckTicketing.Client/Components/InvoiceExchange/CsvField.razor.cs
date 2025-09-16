using System.Collections.Generic;

using Microsoft.AspNetCore.Components;

using SE.BillingService.Contracts.Api.Models;

namespace SE.TruckTicketing.Client.Components.InvoiceExchange;

public partial class CsvField
{
    [Parameter]
    public InvoiceExchangeMessageFieldMappingDto Model { get; set; }

    [Parameter]
    public List<ValueFormatDto> FormatsSource { get; set; }

    [Parameter]
    public bool ShowFormatSelection { get; set; }
}
