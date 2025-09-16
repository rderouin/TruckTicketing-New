using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Models.Invoices;

namespace SE.TruckTicketing.Client.Components.UserControls;

public partial class BillingConfigurationDataList
{
    [Parameter]
    public List<InvoiceBillingConfiguration> ListData { get; set; }

    [Parameter]
    public string Id { get; set; }

    [Parameter]
    public EventCallback<Guid> OpenSelectedBillingConfiguration { get; set; }

    private async Task InvokeOpenBillingConfiguration(Guid? id)
    {
        if (id == null || id == Guid.Empty)
        {
            return;
        }

        await OpenSelectedBillingConfiguration.InvokeAsync(id.Value);
    }
}
