using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen.Blazor;

using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;
using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Pages;

public partial class TruckTicketStubsCreationDialog : BaseRazorComponent
{
    private bool _isProcessing;

    [Parameter]
    public TruckTicketStubCreationRequest Request { get; set; } = new();

    [Parameter]
    public TruckTicketStubsCreationDialogViewModel ViewModel { get; set; } = new();

    [Parameter]
    public EventCallback<TruckTicketStubCreationRequest> OnSubmit { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    private async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }

    private async Task OnCreateButtonClick(RadzenSplitButtonItem item)
    {
        _isProcessing = true;
        Request.GeneratePdf = item == null;
        await OnSubmit.InvokeAsync(Request);
        _isProcessing = false;
    }

    private void OnFacilitiesLoading(SearchCriteriaModel criteria)
    {
        criteria.AddFilter(nameof(Facility.IsActive), true);
    }
}

public class TruckTicketStubsCreationDialogViewModel
{
    public Response<TruckTicketStubCreationRequest> Response { get; set; }
}
