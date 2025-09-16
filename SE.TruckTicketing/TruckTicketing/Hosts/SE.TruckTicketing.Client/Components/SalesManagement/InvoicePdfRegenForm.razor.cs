using Microsoft.AspNetCore.Components;

namespace SE.TruckTicketing.Client.Components.SalesManagement;

public partial class InvoicePdfRegenForm : BaseTruckTicketingComponent
{
    public InvoicePdfRegenFormModel Model { get; set; } = new();

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Parameter]
    public EventCallback<InvoicePdfRegenFormModel> OnSubmit { get; set; }
}

public class InvoicePdfRegenFormModel
{
    public bool IncludeInvoiceCopyWatermark { get; set; }

    public bool ShowRevisionNumber { get; set; }
}
