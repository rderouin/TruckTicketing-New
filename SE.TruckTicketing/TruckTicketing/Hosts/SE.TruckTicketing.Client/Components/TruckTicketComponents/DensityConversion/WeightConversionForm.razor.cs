using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using SE.TruckTicketing.Contracts.Models.Operations;

namespace SE.TruckTicketing.Client.Components.TruckTicketComponents.DensityConversion;

public partial class WeightConversionForm : BaseTruckTicketingComponent
{
    private const string FormId = nameof(WeightConversionForm);

    [Inject]
    private IJSRuntime JsRuntime { get; set; }

    [Parameter]
    public WeightConversionCalculatorViewModel ViewModel { get; set; }

    [Parameter]
    public EventCallback<WeightConversionCalculatorViewModel> OnSubmit { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    protected async Task HandleSubmit(WeightConversionCalculatorViewModel model)
    {
        if (model.IsTotalWeightInvalid || model.IsTotalCutPercentageInvalid)
        {
            return;
        }

        await OnSubmit.InvokeAsync(ViewModel);
    }

    protected void HandleCutParameterChange()
    {
        ViewModel.UpdateCutParameters();
        StateHasChanged();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JsRuntime.InvokeVoidAsync("preventDefaultEventOnEnterKeyPress", FormId);
        }
    }

    private void ResetDensityFactorToDefault()
    {
        ViewModel.Oil.DensityConversionFactor = ViewModel.DefaultDensityFactors.OilConversionFactor;
        ViewModel.Water.DensityConversionFactor = ViewModel.DefaultDensityFactors.WaterConversionFactor;
        ViewModel.Solids.DensityConversionFactor = ViewModel.DefaultDensityFactors.SolidsConversionFactor;
    }
}

public class WeightConversionCalculatorViewModel : DensityConversionViewModelBase<WeightConversionCalculatorViewModel.Parameters>
{
    public WeightConversionCalculatorViewModel(TruckTicketDensityConversionParams parameters, PreSetDensityConversionParams defaultDensityFactors, TruckTicket truckTicket) :
        base(parameters, defaultDensityFactors, truckTicket)
    {
        OnInputWeightChange();
    }

    public override bool IsTotalWeightInvalid => Math.Abs((Total.ComputedWeight ?? 0) - (NetWeight ?? 0)) > 0.011;

    public override string InvalidTotalWeightMessage => "Total weight must be equal to 'Net Weight'";

    public override void UpdateTruckTicket(TruckTicket truckTicket)
    {
        base.UpdateTruckTicket(truckTicket);

        truckTicket.ConversionParameters.MidWeight = default;
    }

    protected sealed override void OnInputWeightChange()
    {
        Oil.NetWeight = NetWeight;
        Water.NetWeight = NetWeight;
        Solids.NetWeight = NetWeight;

        UpdateCutParameters();
    }

    public class Parameters : CutParameters
    {
        public double? NetWeight { get; set; }

        public override double? AdjustedVolume => DensityConversionFactor > 0 ? Math.Round((Weight ?? 0) / DensityConversionFactor, 1) : null;

        public override void UpdateWeight()
        {
            Weight = NetWeight.HasValue && CutPercentage.HasValue && NetWeight > 0 ? Math.Round(NetWeight.Value * CutPercentage.Value / 100.0, 2) : null;
        }

        public override void UpdateCutPercentage()
        {
            CutPercentage = NetWeight.HasValue && Weight.HasValue ? Math.Round(Weight.Value / NetWeight.Value * 100.0, 1) : null;
        }
    }
}
