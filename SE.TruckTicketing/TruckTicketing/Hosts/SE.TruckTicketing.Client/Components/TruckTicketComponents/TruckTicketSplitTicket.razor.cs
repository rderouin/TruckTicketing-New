using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.TruckTicketing.Client.Pages.TruckTickets.New;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Api.Search;
using Trident.Extensions;
using Trident.UI.Blazor.Components;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Components.TruckTicketComponents;

public partial class TruckTicketSplitTicket : BaseRazorComponent
{
    private PagableGridView<SplitViewModel> _splitGrid;

    private SearchResultsModel<SplitViewModel, SearchCriteriaModel> _SplitTicketList = new()
    {
        Info = new()
        {
            PageSize = 10,
        },
        Results = new List<SplitViewModel>(),
    };

    public bool IsSplitting;

    [Parameter]
    public TruckTicket TruckTicket { get; set; }

    private SplitViewModel UnAccountedVolume { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Parameter]
    public Func<IEnumerable<TruckTicket>, Task<bool>> ConfirmCustomerOnTicket { get; set; }

    [Parameter]
    public Func<IEnumerable<TruckTicket>, Task<IEnumerable<TruckTicket>>> SplitTruckTicket { get; set; }

    private double TotalVolumeSum => SplitTruckTickets.SkipLast(1).Sum(x => x.TotalVolume);

    private double TotalVolumePercentSum => SplitTruckTickets.SkipLast(1).Sum(x => x.TotalVolumePercent);

    [Inject]
    public TruckTicketExperienceViewModel ViewModel { get; set; }

    private List<SplitViewModel> SplitTruckTickets { get; set; } = new(2);

    private bool DisableSplitButtonState =>
        (UnAccountedVolume?.TotalVolumePercent ?? Double.NaN) - 0 > 0.11 || SplitTruckTickets.SkipLast(1).Select(x => x.SourceLocationId == default).FirstOrDefault();

    private TruckTicketSplitOptionList SelectedSplitOption { get; set; } = TruckTicketSplitOptionList.Percent;

    [Inject]
    public NotificationService NotificationService { get; set; }

    private async Task LoadSplitTicketGridData()
    {
        _SplitTicketList = new(SplitTruckTickets);
        _SplitTicketList.Info.TotalRecords = SplitTruckTickets.Count;
        await Task.CompletedTask;
    }

    protected override async Task OnInitializedAsync()
    {
        SetUpViewPage();
        await LoadSplitTicketGridData();
        await base.OnInitializedAsync();
    }

    private void SetUpViewPage()
    {
        //first item on split list
        var firstTruckTicketItem = GetSplitTicketLine();
        firstTruckTicketItem.SourceLocationId = TruckTicket.SourceLocationId;
        SplitTruckTickets.Add(firstTruckTicketItem);

        SplitTruckTickets.Add(GetSplitTicketLine());

        UnAccountedVolume = GetSplitTicketLine(TruckTicket);
        UnAccountedVolume.Id = default;
        SplitTruckTickets.Add(UnAccountedVolume);
    }

    private SplitViewModel GetSplitTicketLine(TruckTicket truckTicket = null)
    {
        return new()
        {
            Id = Guid.NewGuid(),
            TotalVolumePercent = truckTicket?.TotalVolumePercent ?? default,
            TotalVolume = truckTicket?.TotalVolume ?? default,
            OilVolumePercent = truckTicket?.OilVolumePercent ?? default,
            OilVolume = truckTicket?.OilVolume ?? default,
            WaterVolumePercent = truckTicket?.WaterVolumePercent ?? default,
            WaterVolume = truckTicket?.WaterVolume ?? default,
            SolidVolumePercent = truckTicket?.SolidVolumePercent ?? default,
            SolidVolume = truckTicket?.SolidVolume ?? default,
        };
    }

    private TruckTicket GetClonedTruckTicket(SplitViewModel splitViewModel = null)
    {
        var clonedTruckTicket = TruckTicket.Clone();
        clonedTruckTicket.Id = splitViewModel?.Id ?? Guid.NewGuid();

        clonedTruckTicket.TicketNumber = default;

        clonedTruckTicket.SourceLocationId = splitViewModel?.SourceLocationId ?? default;

        //clean volumes/cuts
        clonedTruckTicket.TotalVolumePercent = 100;

        clonedTruckTicket.TotalVolume = Math.Round(splitViewModel?.TotalVolume ?? default, 1);
        clonedTruckTicket.LoadVolume = Math.Round(splitViewModel?.TotalVolume ?? default, 1);
        clonedTruckTicket.OilVolumePercent = Math.Round(splitViewModel?.OilVolumePercent ?? default, 1);
        clonedTruckTicket.OilVolume = Math.Round(splitViewModel?.OilVolume ?? default, 1);
        clonedTruckTicket.WaterVolumePercent = Math.Round(splitViewModel?.WaterVolumePercent ?? default, 1);
        clonedTruckTicket.WaterVolume = Math.Round(splitViewModel?.WaterVolume ?? default, 1);
        clonedTruckTicket.SolidVolumePercent = Math.Round(splitViewModel?.SolidVolumePercent ?? default, 1);
        clonedTruckTicket.SolidVolume = Math.Round(splitViewModel?.SolidVolume ?? default, 1);

        clonedTruckTicket.Attachments = new();

        return clonedTruckTicket;
    }

    private void OnTruckTicketSplitListStateChange(TruckTicketSplitOptionList selectedOption)
    {
        SelectedSplitOption = selectedOption;
    }

    private async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }

    private async Task HandleSubmit()
    {
        try
        {
            IsSplitting = true;
            var splitTickets = SplitTruckTickets.SkipLast(1).Select(GetClonedTruckTicket).ToList();

            var isSameCustomer = await ConfirmCustomerOnTicket.Invoke(splitTickets);
            bool? proceed = true;
            if (!isSameCustomer)
            {
                proceed = await DialogService.Confirm("This split will result in different customers being assigned to child tickets from the original. Do you want to proceed?", "MyTitle",
                                                      new()
                                                      {
                                                          OkButtonText = "Proceed",
                                                          CancelButtonText = "Cancel",
                                                      });
            }

            if (proceed.HasValue && proceed.Value)
            {
                var splitFinalTickets = await SplitTruckTicket.Invoke(splitTickets);

                await ViewModel.ReloadCurrentTruckTicket();

                var notificationMessage = @$"Ticket Split - {TruckTicket.TicketNumber} has been split and void•
                                            New Tickets have been created from split {string.Join(",", splitFinalTickets.Select(x => x.TicketNumber))} •
                                            Split initiated by: {Application?.User?.Principal?.Identity?.Name} •
                                            Split initiated at {DateTimeOffset.UtcNow}";

                ViewModel.TriggerAfterSave();

                NotificationService.Notify(NotificationSeverity.Success, "Operation Complete", notificationMessage, 30000);
            }

            await HandleCancel();
        }
        finally
        {
            IsSplitting = false;
        }
    }

    private async Task AddSplit()
    {
        SplitTruckTickets.Insert(SplitTruckTickets.Count - 1, GetSplitTicketLine());
        await _splitGrid.ReloadGrid();
    }

    private void HandleVolumeChange(SplitViewModel truckTicket)
    {
        var originalOilPercent = TruckTicket.OilVolumePercent;
        var originalWaterPercent = TruckTicket.WaterVolumePercent;
        var originalSolidPercent = TruckTicket.SolidVolumePercent;
        if (SelectedSplitOption == TruckTicketSplitOptionList.Quantity)
        {
            truckTicket.TotalVolumePercent = truckTicket.TotalVolume / TruckTicket.TotalVolume * 100.0;
        }
        else
        {
            truckTicket.TotalVolume = truckTicket.TotalVolumePercent * TruckTicket.TotalVolume / 100.0;
        }

        truckTicket.OilVolumePercent = originalOilPercent;
        truckTicket.WaterVolumePercent = originalWaterPercent;
        truckTicket.SolidVolumePercent = originalSolidPercent;

        truckTicket.OilVolume = truckTicket.TotalVolume * originalOilPercent / 100.0;

        truckTicket.WaterVolume = truckTicket.TotalVolume * originalWaterPercent / 100.0;

        truckTicket.SolidVolume = truckTicket.TotalVolume * originalSolidPercent / 100.0;

        HandleUnaccountedVolume();
    }

    private void HandleUnaccountedVolume()
    {
        UnAccountedVolume.TotalVolume = TruckTicket.TotalVolume - SplitTruckTickets.SkipLast(1).Select(x => x.TotalVolume).Sum();
        UnAccountedVolume.TotalVolumePercent = TruckTicket.TotalVolumePercent - SplitTruckTickets.SkipLast(1).Select(x => x.TotalVolumePercent).Sum();
        UnAccountedVolume.OilVolume = TruckTicket.OilVolume - SplitTruckTickets.SkipLast(1).Select(x => x.OilVolume).Sum();
        UnAccountedVolume.WaterVolume = TruckTicket.WaterVolume - SplitTruckTickets.SkipLast(1).Select(x => x.WaterVolume).Sum();
        UnAccountedVolume.SolidVolume = TruckTicket.SolidVolume - SplitTruckTickets.SkipLast(1).Select(x => x.SolidVolume).Sum();
    }

    private async Task RemoveSplitItem(SplitViewModel item)
    {
        SplitTruckTickets = SplitTruckTickets.Where(x => x.Id != item.Id).ToList();
        HandleUnaccountedVolume();
        await _splitGrid.ReloadGrid();
    }

    private enum TruckTicketSplitOptionList
    {
        [Description("Percent")]
        Percent = 1,

        [Description("Quantity")]
        Quantity = 2,
    }

    public class SplitViewModel
    {
        public Guid Id { get; set; }

        public Guid SourceLocationId { get; set; }

        public double OilVolume { get; set; }

        public double OilVolumePercent { get; set; }

        public double TotalVolume { get; set; }

        public double TotalVolumePercent { get; set; }

        public double WaterVolume { get; set; }

        public double WaterVolumePercent { get; set; }

        public double SolidVolume { get; set; }

        public double SolidVolumePercent { get; set; }
    }
}
