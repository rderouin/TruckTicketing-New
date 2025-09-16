using System;

using Microsoft.AspNetCore.Components;

namespace SE.TruckTicketing.Client.Components.TruckTicketComponents.DensityConversion;

public partial class CutPercentages<TCutParameters> : BaseTruckTicketingComponent where TCutParameters : CutParameters, new()
{
    [Parameter]
    public DensityConversionViewModelBase<TCutParameters> ViewModel { get; set; }

    [Parameter]
    public EventCallback OnParameterChange { get; set; }

    protected bool TotalCutPercentageInvalid => ViewModel.Total.CutPercentage is > 0 && Math.Abs(ViewModel.Total.CutPercentage.Value - 100) > 0.11;
}
