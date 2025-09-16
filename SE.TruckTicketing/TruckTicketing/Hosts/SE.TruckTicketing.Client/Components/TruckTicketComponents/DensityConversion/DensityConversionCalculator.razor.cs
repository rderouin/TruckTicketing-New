using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;

namespace SE.TruckTicketing.Client.Components.TruckTicketComponents.DensityConversion;

public partial class DensityConversionCalculator : BaseTruckTicketingComponent
{
    [Parameter]
    public TruckTicket TruckTicket { get; set; }

    [Parameter]
    public PreSetDensityConversionParams DefaultDensityFactorsByWeight { get; set; }

    [Parameter]
    public PreSetDensityConversionParams DefaultDensityFactorsByMidWeight { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Parameter]
    public EventCallback OnCutsUpdate { get; set; }

    [Inject]
    public IBackendService BackendService { get; set; }

    private bool IsMidWeightCalculatorUsed => TruckTicket?.ConversionParameters?.MidWeight != null;

    private WeightConversionCalculatorViewModel WeightConversionViewModel =>
        IsMidWeightCalculatorUsed ? new(null, DefaultDensityFactorsByWeight, TruckTicket) : new(TruckTicket?.ConversionParameters, DefaultDensityFactorsByWeight, TruckTicket);

    private MidWeightConversionCalculatorViewModel MidWeightConversionViewModel =>
        IsMidWeightCalculatorUsed ? new(TruckTicket?.ConversionParameters, DefaultDensityFactorsByMidWeight, TruckTicket) : new(null, DefaultDensityFactorsByMidWeight, TruckTicket);

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
    }

    private async Task HandleWeightConversionSubmit(WeightConversionCalculatorViewModel model)
    {
        model.UpdateTruckTicket(TruckTicket);
        await FireOnCutsUpdate();
    }

    private async Task HandleMidWeightConversionSubmit(MidWeightConversionCalculatorViewModel model)
    {
        model.UpdateTruckTicket(TruckTicket);
        await FireOnCutsUpdate();
    }

    private async Task FireOnCutsUpdate()
    {
        if (OnCutsUpdate.HasDelegate)
        {
            await OnCutsUpdate.InvokeAsync();
        }
    }

    private void OnConversionCalculatorHistoryLoad(SearchCriteriaModel criteria)
    {
        criteria.Filters[nameof(Change.FieldLocation)] = new CompareModel
        {
            IgnoreCase = false,
            Operator = CompareOperators.eq,
            Value = nameof(TruckTicket.ConversionParameters),
        };
    }
}
