using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen.Blazor;

using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Invoices;

using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.InvoiceComponents;

public partial class InvoiceReversalDialog : BaseRazorComponent
{
    private RadzenTemplateForm<ReverseInvoiceRequest> _form;

    private bool InvoiceReversalDescriptionIsRequired => Model.InvoiceReversalReason == InvoiceReversalReason.Other;

    private bool CreateNew { get; set; }

    [Parameter]
    public ReverseInvoiceRequest Model { get; set; }

    [Parameter]
    public EventCallback<ReverseInvoiceRequest> OnSubmit { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    private async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }

    private async Task HandleSubmit()
    {
        Model.CreateProForma = CreateNew;
        await OnSubmit.InvokeAsync(Model);
    }
}
