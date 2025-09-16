using System;
using System.Collections.Generic;

using Microsoft.AspNetCore.Components;

using SE.BillingService.Contracts.Api.Models;

namespace SE.TruckTicketing.Client.Components.InvoiceExchange;

public partial class FormatPicker
{
    [Parameter]
    public IList<ValueFormatDto> FormatsSource { get; set; }

    [Parameter]
    public InvoiceExchangeMessageFieldMappingDto Model { get; set; }

    private bool ShowConstantField => Model.DestinationFormatId == default(Guid);
}
