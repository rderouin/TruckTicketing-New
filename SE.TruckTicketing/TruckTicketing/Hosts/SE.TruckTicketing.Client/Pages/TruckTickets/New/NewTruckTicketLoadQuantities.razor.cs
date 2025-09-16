using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using Microsoft.AspNetCore.Components;

using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.Sampling;
using SE.TruckTicketing.UI.Contracts.Services;

namespace SE.TruckTicketing.Client.Pages.TruckTickets.New;

public partial class NewTruckTicketLoadQuantities : BaseTruckTicketingComponent
{
    [Inject]
    public TruckTicketExperienceViewModel ViewModel { get; set; }

    public TruckTicket Model => ViewModel.TruckTicket;

    [CascadingParameter(Name = "TruckTicketRefresh")]
    public EventCallback Refresh { get; set; }

    [Parameter]
    public Facility Facility { get; set; }

    [Parameter]
    public ServiceType ServiceType { get; set; }

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

    private string VolumeTitle => Model.CountryCode == CountryCode.US ? "Volume (BBL)" : "Volume (m³)";

    protected async Task HandleLoadVolumeChange()
    {
        ViewModel.SetLoadVolumeChange();
        
        if (ViewModel.VolumeCutValidationErrors.Count == 0 && (Model.OilVolume > 0 || Model.WaterVolume > 0 || Model.SolidVolume > 0))
        {
            await ViewModel.TriggerWorkflows();
        }
    }
    public async Task HandleWeightChange(bool checkSamplingStatus)
    {   
        Model.NetWeight = Model.GrossWeight - Model.TareWeight;

        if (checkSamplingStatus && Model.GrossWeight > 0 && Model.TareWeight > 0)
        {
            if (ShouldSkipSamplingCheck() is false)
            {
                await CheckSamplingStatus();
            }
        }

        if (double.IsNegative(Model.NetWeight))
        {
            await DialogService.OpenAsync<TruckTicketAlertComponent>("Net Weight Quantity",
                                                                     new()
                                                                     {
                                                                         { nameof(TruckTicketAlertComponent.Message), "Warning!! You have entered a Negative Net Weight Ticket." },
                                                                         { nameof(TruckTicketAlertComponent.ButtonText), "Close" },
                                                                     });
        }

        // Added condition to show warning Dialog to user if TareWeight is zero and sales lines are  generated #11958
        if (Model.NetWeight > 0 && Model.TareWeight == 0 && ViewModel.SalesLines.Any())
        {
            await DialogService.OpenAsync<TruckTicketAlertComponent>("Tare Weight Quantity",
                                                                     new()
                                                                     {
                                                                         { nameof(TruckTicketAlertComponent.Message), "Warning!! Tare Weight can not be Zero once Sales Line generated." },
                                                                         { nameof(TruckTicketAlertComponent.ButtonText), "Close" },
                                                                     });
        }
    }

    private bool ShouldSkipSamplingCheck()
    {
        if (ViewModel.TruckTicketBackup == null)
        {
            return false;
        }

        if (ViewModel.TruckTicketBackup.ServiceTypeClass != ViewModel.TruckTicket.ServiceTypeClass)
        {
            return false;
        }

        return ViewModel.TruckTicket.TimeOfLastSampleCountdownUpdate.HasValue && ViewModel.TruckTicketBackup.NetWeight > ViewModel.TruckTicket.NetWeight;
    }

    private async Task CheckSamplingStatus()
    {
        double CalcNetWeightDelta()
        {
            if (ViewModel.TruckTicketBackup != null && ViewModel.TruckTicketBackup.ServiceTypeClass == Model.ServiceTypeClass && ViewModel.TruckTicketBackup.NetWeight < Model.NetWeight)
            {
                // negative net weights were never sampled, treat them as zeroes
                var formerWeight = ViewModel.TruckTicketBackup.NetWeight > 0 ? ViewModel.TruckTicketBackup.NetWeight : 0;
                var targetWeight = Model.NetWeight > 0 ? Model.NetWeight : 0;
                var delta = targetWeight - formerWeight;
                return delta;
            }

            return Model.NetWeight;
        }

        var samplingRequest = new LandfillSamplingStatusCheckRequestDto
        {
            FacilityId = Model.FacilityId,
            NetWeight = CalcNetWeightDelta(),
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
        ViewModel.TriggerStateChanged();
    }

    private string SetBorder(string cutLabel)
    {
        return ViewModel.VolumeCutValidationErrors.ContainsKey(cutLabel) ? "outline: 1px solid red" : string.Empty;
    }

    private string GetCutLabelValue(SubstanceThresholdType thresholdType, string cutLabel)
    {
        var label = string.Empty;
        const string infinitySymbol = "∞";
        const string negativeInfinitySymbol = "-∞";
        if (thresholdType == ServiceType?.OilThresholdType && cutLabel is nameof(Model.OilVolume) or nameof(Model.OilVolumePercent))
        {
            var minValue = ServiceType?.OilMinValue.HasValue ?? false ? ServiceType?.OilMinValue.ToString() : negativeInfinitySymbol;
            var maxValue = ServiceType?.OilMaxValue.HasValue ?? false ? ServiceType?.OilMaxValue.ToString() : infinitySymbol;
            label = $"Min:{minValue} | Max:{maxValue}";
        }
        else if (thresholdType == ServiceType?.WaterThresholdType && cutLabel is nameof(Model.WaterVolume) or nameof(Model.WaterVolumePercent))
        {
            var minValue = ServiceType?.WaterMinValue.HasValue ?? false ? ServiceType?.WaterMinValue.ToString() : negativeInfinitySymbol;
            var maxValue = ServiceType?.WaterMaxValue.HasValue ?? false ? ServiceType?.WaterMaxValue.ToString() : infinitySymbol;
            label = $"Min:{minValue} | Max:{maxValue}";
        }
        else if (thresholdType == ServiceType?.SolidThresholdType && cutLabel is nameof(Model.SolidVolume) or nameof(Model.SolidVolumePercent))
        {
            var minValue = ServiceType?.SolidMinValue.HasValue ?? false ? ServiceType?.SolidMinValue.ToString() : negativeInfinitySymbol;
            var maxValue = ServiceType?.SolidMaxValue.HasValue ?? false ? ServiceType?.SolidMaxValue.ToString() : infinitySymbol;
            label = $"Min:{minValue} | Max:{maxValue}";
        }

        return label;
    }

    private class ValidationResult
    {
        public List<string> MemberNames { get; set; }
    }
}
