using Microsoft.AspNetCore.Components;

namespace SE.TruckTicketing.Client.Components.TruckTicketComponents.DensityConversion;

public partial class AdjustedVolumes<TCutParameters> : BaseTruckTicketingComponent where TCutParameters : CutParameters, new()
{
    [Parameter]
    public DensityConversionViewModelBase<TCutParameters> ViewModel { get; set; }
}
