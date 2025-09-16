using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Humanizer;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using SE.BillingService.Contracts.Api.Enums;
using SE.BillingService.Contracts.Api.Models;

namespace SE.TruckTicketing.Client.Components.InvoiceExchange;

public partial class FieldMapping
{
    static FieldMapping()
    {
        // general init
        SupportedMessageAdapters = Enum.GetValues<MessageAdapterType>()
                                       .Where(v => v != MessageAdapterType.Undefined)
                                       .ToDictionary(t => t, t => t.Humanize());

        SupportedPidxVersions = new()
        {
            [1.00m] = "v 1.0",
            [1.62m] = "v 1.62",
        };
    }

    [Parameter]
    public InvoiceExchangeType InvoiceExchangeType { get; set; }

    [Parameter]
    public InvoiceExchangeDeliveryConfigurationDto DeliveryConfiguration { get; set; }

    [Parameter]
    public List<SourceFieldDto> SourceFields { get; set; }

    [Parameter]
    public List<DestinationFieldDto> PidxFields { get; set; }

    [Parameter]
    public string PidxNamespace { get; set; }

    [Parameter]
    public List<ValueFormatDto> ValueFormats { get; set; }

    private static Dictionary<MessageAdapterType, string> SupportedMessageAdapters { get; set; }

    private static Dictionary<decimal, string> SupportedPidxVersions { get; set; }

    private string VersionedPidxNamespace => $"{PidxNamespace}-{@$"{DeliveryConfiguration.MessageAdapterVersion * 100:#_##}"}";

    private bool FullLayout => DeliveryConfiguration.MessageAdapterType != MessageAdapterType.HttpEndpoint;

    private void AddMapping()
    {
        DeliveryConfiguration.Mappings.Add(new());
    }

    private async Task DeleteMapping(InvoiceExchangeMessageFieldMappingDto mapping)
    {
        var confirmed = await DialogService.Confirm("Are you sure you want to delete this mapping?", "Confirm deletion", new()
        {
            OkButtonText = "Yes",
            CancelButtonText = "No",
        });

        if (confirmed == true)
        {
            DeliveryConfiguration.Mappings.Remove(mapping);
        }
    }

    private Task OnMessageAdapterSettingsChanged(FieldIdentifier arg)
    {
        StateHasChanged();
        return Task.CompletedTask;
    }
}
