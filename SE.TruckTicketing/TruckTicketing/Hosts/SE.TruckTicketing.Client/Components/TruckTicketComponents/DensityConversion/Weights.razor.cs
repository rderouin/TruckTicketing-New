using JetBrains.Annotations;

using Microsoft.AspNetCore.Components;

namespace SE.TruckTicketing.Client.Components.TruckTicketComponents.DensityConversion;

public partial class Weights<TCutParameters> : BaseTruckTicketingComponent where TCutParameters : CutParameters, new()
{
    [Parameter]
    public DensityConversionViewModelBase<TCutParameters> ViewModel { get; set; }

    [Parameter]
    [CanBeNull]
    public string Title { get; set; }

    [Parameter]
    public EventCallback OnParameterChange { get; set; }
}
