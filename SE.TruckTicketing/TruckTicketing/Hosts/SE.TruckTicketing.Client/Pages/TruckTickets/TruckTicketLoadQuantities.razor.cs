using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.Sampling;
using SE.TruckTicketing.UI.Contracts.Services;

namespace SE.TruckTicketing.Client.Pages.TruckTickets;

public partial class TruckTicketLoadQuantities : BaseTruckTicketingComponent
{
    [CascadingParameter(Name = "TruckTicket")]
    public TruckTicket Model { get; set; }

    [CascadingParameter(Name = "TruckTicketRefresh")]
    public EventCallback Refresh { get; set; }

    [Parameter]
    public Facility Facility { get; set; }

    [Inject]
    private ILandfillSamplingService LandfillSamplingService { get; set; }

    protected bool ShowSamplingAlert =>
        Model.LandfillSamplingStatus.Action.Equals(LandfillSamplingStatusCheckAction.Block, StringComparison.OrdinalIgnoreCase) ||
        Model.LandfillSamplingStatus.Action.EndsWith(LandfillSamplingStatusCheckAction.Warn, StringComparison.OrdinalIgnoreCase);

    protected string AlertClass =>
        Model.LandfillSamplingStatus.Action.Equals(LandfillSamplingStatusCheckAction.Block, StringComparison.OrdinalIgnoreCase) ? "alert-danger" :
        Model.LandfillSamplingStatus.Action.Equals(LandfillSamplingStatusCheckAction.Warn, StringComparison.OrdinalIgnoreCase) ? "alert-warning" : "alert-primary";

    protected bool DisableTareWeightField =>
        Model.FacilityId == default ||
        Model.WellClassification == WellClassifications.Undefined ||
        Model.FacilityServiceSubstanceId == default ||
        Model.FacilityServiceSubstanceId == Guid.Empty;

    protected bool IsTotalVolumePercentInvalid => Math.Abs(Model.TotalVolumePercent - 100) > 0.01;

    protected bool IsTotalVolumeInvalid => Model.LoadVolume.HasValue && Math.Abs(Model.TotalVolume - Model.LoadVolume.Value) > 0.1;

    protected void HandleVolumeChange()
    {
        Model.TotalVolume = Model.OilVolume + Model.WaterVolume + Model.SolidVolume;

        Model.OilVolumePercent = Model.TotalVolume > 0 ? Model.OilVolume * 100.0 / Model.TotalVolume : 0;
        Model.WaterVolumePercent = Model.TotalVolume > 0 ? Model.WaterVolume * 100.0 / Model.TotalVolume : 0;
        Model.SolidVolumePercent = Model.TotalVolume > 0 ? Model.SolidVolume * 100.0 / Model.TotalVolume : 0;
        Model.TotalVolumePercent = Model.OilVolumePercent + Model.WaterVolumePercent + Model.SolidVolumePercent;
    }

    protected void HandleCutChange()
    {
        Model.TotalVolumePercent = Model.OilVolumePercent + Model.WaterVolumePercent + Model.SolidVolumePercent;

        Model.OilVolume = Math.Round((Model.LoadVolume ?? 0) * (Model.OilVolumePercent / 100.0), 2);
        Model.WaterVolume = Math.Round((Model.LoadVolume ?? 0) * (Model.WaterVolumePercent / 100.0), 2);
        Model.SolidVolume = Math.Round((Model.LoadVolume ?? 0) * (Model.SolidVolumePercent / 100.0), 2);
        Model.TotalVolume = Model.OilVolume + Model.WaterVolume + Model.SolidVolume;
    }

    protected async Task HandleWeightChange(bool checkSamplingStatus)
    {
        Model.NetWeight = Model.GrossWeight - Model.TareWeight;

        if (checkSamplingStatus)
        {
            await CheckSamplingStatus();
        }
    }

    private async Task CheckSamplingStatus()
    {
        var samplingRequest = new LandfillSamplingStatusCheckRequestDto
        {
            FacilityId = Model.FacilityId,
            NetWeight = Model.TareWeight,
            WellClassification = Model.WellClassification,
            FacilityServiceSubstanceId = Model.FacilityServiceSubstanceId,
        };

        Model.LandfillSamplingStatus = await LandfillSamplingService.CheckStatus(samplingRequest);

        await SetRequireSampleFlag();
    }

    private async Task SetRequireSampleFlag()
    {
        if (Model.LandfillSamplingStatus.Action.Equals(LandfillSamplingStatusCheckAction.Allow, StringComparison.OrdinalIgnoreCase) ||
            Model.LandfillSamplingStatus.Action.Equals(LandfillSamplingStatusCheckAction.Warn, StringComparison.OrdinalIgnoreCase) ||
            (Model.LandfillSamplingStatus.Action.Equals(LandfillSamplingStatusCheckAction.Block, StringComparison.OrdinalIgnoreCase) && Model.LandfillSampled))
        {
            Model.RequireSample = false;
        }
        else
        {
            Model.RequireSample = true;
        }

        await Refresh.InvokeAsync();
    }
}
