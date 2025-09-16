using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen.Blazor;

using SE.TruckTicketing.UI.ViewModels;

namespace SE.TruckTicketing.Client.Components.TruckTicketComponents;

public partial class EditMultipleTickets
{
    [Parameter]
    public EditMultipleTicketsViewModel Model { get; set; }

    public RadzenTemplateForm<EditMultipleTicketsViewModel> Form { get; set; }

    [Parameter]
    public EventCallback<EditMultipleTicketsViewModel> OnSubmit { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    private async Task Submit()
    {
        await OnSubmit.InvokeAsync(Model);
    }

    private async Task Cancel()
    {
        await OnCancel.InvokeAsync();
    }
}
