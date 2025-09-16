using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using SE.BillingService.Contracts.Api.Enums;
using SE.BillingService.Contracts.Api.Models;

namespace SE.TruckTicketing.Client.Components.InvoiceExchange;

public partial class AdapterSettings
{
    private EditContext _editContext;

    [Parameter]
    public MessageAdapterType Type { get; set; }

    [Parameter]
    public InvoiceExchangeMessageAdapterSettingsDto MessageAdapterSettings { get; set; } = new();

    [Parameter]
    public EventCallback<FieldIdentifier> OnFieldChanged { get; set; }

    protected override Task OnParametersSetAsync()
    {
        _editContext = new(MessageAdapterSettings);
        _editContext.OnFieldChanged += EditContextOnOnFieldChanged;
        return base.OnParametersSetAsync();
    }

    private void EditContextOnOnFieldChanged(object sender, FieldChangedEventArgs e)
    {
        OnFieldChanged.InvokeAsync(e.FieldIdentifier);
    }
}
