using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Sampling;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.Client.Pages.TruckTickets.New;

public partial class LandfillSamplingCountdown : BaseTruckTicketingComponent
{
    private Guid _facilityId;

    private bool _isLoading;

    private LandfillSamplingDto[] _landfillSamplingValues = Array.Empty<LandfillSamplingDto>();

    [Parameter]
    public Guid FacilityId { get; set; }

    [Inject]
    private TruckTicketExperienceViewModel ViewModel { get; set; }

    [Inject]
    private IServiceProxyBase<LandfillSamplingDto, Guid> LandfillSamplingService { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        if (_facilityId != FacilityId)
        {
            await LoadData(FacilityId);
        }
    }

    protected override void OnInitialized()
    {
        ViewModel.TicketSaved += ViewModelOnTicketSaved;
    }

    public override void Dispose()
    {
        ViewModel.TicketSaved -= ViewModelOnTicketSaved;
    }

    private string ValueFormatString(SamplingRuleType samplingRuleType, string value, string threshold)
    {
        double.TryParse(value, out var numericValue);
        double.TryParse(threshold, out var numericThreshold);
        DateTimeOffset.TryParse(value, out var timeValue);
        return samplingRuleType switch
               {
                   SamplingRuleType.Weight => (numericThreshold - numericValue).ToString("0.00"),
                   SamplingRuleType.Time => timeValue.ToString("g"),
                   _ => value
               };
    }

    private bool IsClass1Product(string productNumber)
    {
       return (productNumber.HasText() && productNumber.Equals("702")
                 && ViewModel.TruckTicket.ServiceTypeClass == Class.Class1 
                 && ViewModel.TruckTicket.TruckTicketType == TruckTicketType.LF);
    }

    private string ClassFor(string productNumber)
    {
        return productNumber switch
               {
                   "701" => "Class 2",
                   "702" => "Class 1",
                   _ => productNumber
               };
    }

    private async Task ViewModelOnTicketSaved()
    {
        await LoadData(_facilityId);
    }

    private async Task LoadData(Guid facilityId)
    {
        try
        {
            _isLoading = true;
            StateHasChanged();
            var criteria = new SearchCriteriaModel
            {
                Filters = new()
                {
                    [nameof(LandfillSamplingDto.FacilityId)] = facilityId,
                },
            };

            var results = await LandfillSamplingService.Search(criteria);
            _landfillSamplingValues = results?.Results.ToArray() ?? Array.Empty<LandfillSamplingDto>();
            _facilityId = facilityId;
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }
}
