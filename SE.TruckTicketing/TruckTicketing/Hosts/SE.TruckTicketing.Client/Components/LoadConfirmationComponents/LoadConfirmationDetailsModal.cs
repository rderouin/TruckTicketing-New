using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Models.LoadConfirmations;

namespace SE.TruckTicketing.Client.Components.LoadConfirmationComponents;

public partial class LoadConfirmationDetailsModal : BaseTruckTicketingComponent
{
    [Parameter]
    public LoadConfirmation Model { get; set; }

    public async Task OpenModal()
    {
        await DialogService.OpenAsync<LoadConfirmationDetails>($"Load Confirmation {Model.Number}",
                                                               new() { { nameof(LoadConfirmationDetails.Model), Model } },
                                                               new()
                                                               {
                                                                   Height = "80%",
                                                                   Width = "80%",
                                                               });
    }
}
