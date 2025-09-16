using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using Radzen.Blazor;

using SE.TruckTicketing.Contracts.Models.Operations;

using static SE.TruckTicketing.Client.Components.TruckTicketComponents.DensityConversion.MidWeightConversionCalculatorViewModel;

namespace SE.TruckTicketing.Client.Components.TruckTicketComponents.DensityConversion;

public partial class MidWeightConversionForm : BaseTruckTicketingComponent
{
    private const string FormId = nameof(MidWeightConversionForm);

    protected RadzenTemplateForm<MidWeightConversionCalculatorViewModel> Form;

    [Inject]
    private IJSRuntime JsRuntime { get; set; }

    [Parameter]
    public MidWeightConversionCalculatorViewModel ViewModel { get; set; }

    [Parameter]
    public EventCallback<MidWeightConversionCalculatorViewModel> OnSubmit { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    protected async Task HandleSubmit(MidWeightConversionCalculatorViewModel model)
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

public class MidWeightConversionCalculatorViewModel : DensityConversionViewModelBase<Parameters>
{
    private double? _midWeight;

    public MidWeightConversionCalculatorViewModel(TruckTicketDensityConversionParams parameters, PreSetDensityConversionParams defaultDensityFactors, TruckTicket truckTicket) :
        base(parameters, defaultDensityFactors, truckTicket)
    {
        if (parameters is null)
        {
            return;
        }

        _midWeight = parameters.MidWeight;
        OnInputWeightChange();
    }

    public double? MidWeight
    {
        get => _midWeight;
        set
        {
            _midWeight = value;
            OnInputWeightChange();
        }
    }

    public override bool IsTotalWeightInvalid => Math.Abs((Total.ComputedWeight ?? 0) - (FluidsWeight ?? 0)) > 0.011;

    public override string InvalidTotalWeightMessage => "Total weight must be equal to 'Fluids Weight'";

    public double? DrySolidsWeight => MidWeight.HasValue && TareWeight.HasValue ? Math.Round(MidWeight.Value - TareWeight.Value, 2) : null;

    public double? FluidsWeight => GrossWeight.HasValue && MidWeight.HasValue ? Math.Round(GrossWeight.Value - MidWeight.Value, 2) : null;

    protected sealed override void OnInputWeightChange()
    {
        Oil.FluidsWeight = FluidsWeight;
        Water.FluidsWeight = FluidsWeight;
        Solids.FluidsWeight = FluidsWeight;
        Solids.DrySolidsWeight = DrySolidsWeight;

        UpdateCutParameters();
    }

    public override void UpdateTruckTicket(TruckTicket truckTicket)
    {
        base.UpdateTruckTicket(truckTicket);

        truckTicket.ConversionParameters.MidWeight = _midWeight;
    }

    public class Parameters : CutParameters
    {
        public double? FluidsWeight { get; set; }

        public double? DrySolidsWeight { get; set; }

        public override double? AdjustedVolume => DensityConversionFactor > 0 ? Math.Round(((Weight ?? 0) + (DrySolidsWeight ?? 0)) / DensityConversionFactor, 1) : null;

        public override void UpdateWeight()
        {
            Weight = FluidsWeight.HasValue && CutPercentage.HasValue && FluidsWeight > 0 ? Math.Round(FluidsWeight.Value * CutPercentage.Value / 100.0, 2) : null;
        }

        public override void UpdateCutPercentage()
        {
            CutPercentage = FluidsWeight.HasValue && Weight.HasValue ? Math.Round(Weight.Value / FluidsWeight.Value * 100.0, 1) : null;
        }
    }
}
