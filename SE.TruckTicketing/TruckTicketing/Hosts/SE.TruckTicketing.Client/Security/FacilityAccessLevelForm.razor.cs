using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Contracts.Models.Accounts;
using SE.TruckTicketing.Contracts.Models.Operations;

namespace SE.TruckTicketing.Client.Security;

public partial class FacilityAccessLevelForm : BaseTruckTicketingComponent
{
    [Parameter]
    public UserProfileFacilityAccess Model { get; set; }

    [Parameter]
    public UserProfile UserProfile { get; set; }

    [Parameter]
    public EventCallback<UserProfileFacilityAccess> OnSubmit { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    private Task HandleFacilityChange(Facility facility)
    {
        Model.FacilityId = facility.Id.ToString();
        Model.FacilityDisplayName = facility.Display;
        return Task.CompletedTask;
    }
}
