using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

namespace SE.TruckTicketing.Client.Components.TruckTicketComponents.DensityConversion;

public partial class ConversionFactors<TCutParameters> : BaseTruckTicketingComponent where TCutParameters : CutParameters, new()
{
    protected double? _sharedDensity;

    [Parameter]
    public DensityConversionViewModelBase<TCutParameters> ViewModel { get; set; }

    [Parameter]
    public EventCallback<double> OnParameterChange { get; set; }

    protected async Task HandleSharedDensityChange(double? density)
    {
        _sharedDensity = null;
        if (density is null)
        {
            return;
        }

        ViewModel.Oil.DensityConversionFactor = density.Value;
        ViewModel.Water.DensityConversionFactor = density.Value;
        ViewModel.Solids.DensityConversionFactor = density.Value;

        await OnParameterChange.InvokeAsync();
    }
}
