using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.Client.Pages.TruckTickets.New;

public partial class NewTruckTicketStubRequestForm : BaseTruckTicketingComponent
{
    protected bool IsProcessing;

    [Parameter]
    public TruckTicketStubCreationRequest Request { get; set; } = new();

    [Parameter]
    public NewTruckTicketStubRequestFormViewModel ViewModel { get; set; } = new();

    [Parameter]
    public EventCallback<TruckTicketStubCreationRequest> OnSubmit { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    protected async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }

    protected async Task OnCreateButtonClick()
    {
        IsProcessing = true;
        Request.GeneratePdf = true;
        await OnSubmit.InvokeAsync(Request);
        IsProcessing = false;
    }

    protected void SetFacilitySideId(Facility facility)
    {
        Request.SiteId = facility?.SiteId;
    }

    protected void OnFacilitiesLoading(SearchCriteriaModel criteria)
    {
        criteria.AddFilter(nameof(Facility.IsActive), true);
    }
}

public class NewTruckTicketStubRequestFormViewModel
{
    public Response<TruckTicketStubCreationRequest> Response { get; set; }
}
