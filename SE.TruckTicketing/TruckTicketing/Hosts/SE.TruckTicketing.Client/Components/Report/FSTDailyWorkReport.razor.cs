using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;

using BlazorDownloadFile;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels.TruckTickets;

using Trident.Api.Search;
using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.Report;

public partial class FSTDailyWorkReport : BaseRazorComponent
{
    private bool _isPdfGenBusy;

    private bool _isExcelGenBusy;

    private List<Guid> FacilityIds = new();

    private TridentApiDropDownDataGrid<ServiceType, Guid> _serviceTypeDropDown { get; set; }
    
    private bool GenerateButtonDisable => _isPdfGenBusy || _isExcelGenBusy || FSTRequest.FacilityId == default || FSTRequest.FromDate == default || FSTRequest.ToDate == default;

    public FSTWorkTicketRequest FSTRequest { get; } = new()
    {
        SelectedTicketStatuses = new() { TruckTicketStatus.Approved }
    };

    private IEnumerable<TruckTicketStatusViewModel> TruckTicketStatusViewModels = TruckTicketStatusViewModel.GetAll();

    private IEnumerable<int> SelectedStatusViewModels = new[]{TruckTicketStatusViewModel.Approved.Id};//default selection
    
    [Inject]
    private IBlazorDownloadFileService FileDownloadService { get; set; }
    
    [Inject]
    private ITruckTicketService TruckTicketService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }
    
    private static void OnFacilitiesLoading(SearchCriteriaModel criteria)
    {
        criteria.AddFilter(nameof(Facility.IsActive), true);
        criteria.OrderBy = nameof(Facility.SiteId);
        criteria.Filters[nameof(Facility.Type)] = new CompareModel
        {
            Operator = CompareOperators.ne,
            Value = FacilityType.Lf.ToString(),
        };
    }
    
    private async Task OnFacilitySelect(Facility facility)
    {
        FSTRequest.FacilityId = facility.Id;
        FSTRequest.LegalEntityName = facility.LegalEntity;
        FSTRequest.LegalEntityId = facility.LegalEntityId;
        FSTRequest.FacilityName = facility.Name;
        FacilityIds = new() { facility.Id };

        await _serviceTypeDropDown.Refresh(FacilityIds);
    }

    private void OnTruckTicketStatusChange(object args)
    {
        FSTRequest.SelectedTicketStatuses = new();

        var selectedIds = args as IEnumerable<int> ?? Enumerable.Empty<int>();

        var selectedIdsList = selectedIds.ToList();

        if (!selectedIdsList.Any()){
            return;
        }

        foreach (var selectedId in selectedIdsList)
        {
            FSTRequest.SelectedTicketStatuses.Add((TruckTicketStatus)selectedId);
        }

        StateHasChanged();
    }

    private void OnServiceTypeChange(object args)
    {
        FSTRequest.ServiceTypeIds = new();
        var ids = args as IEnumerable<Guid> ?? Enumerable.Empty<Guid>();

        if (ids.Any())
        {
            FSTRequest.ServiceTypeIds.AddRange(ids);
        }
    }
    
    private async Task HandleGeneratePdf()
    {
       _isPdfGenBusy = true;

        FSTRequest.RequestedFileType = "pdf";

        var response = await TruckTicketService.DownloadFSTDailyWorkTicket(FSTRequest);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.HttpContent.ReadAsByteArrayAsync();
            if (content.Length > 0)
            {
                await FileDownloadService.DownloadFile("FSTDailyWorkTicket.pdf", content, MediaTypeNames.Application.Pdf);

                NotificationService.Notify(NotificationSeverity.Success, "FST Daily Report Downloaded successful");
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Info, "No Truck Tickets found for report");
            }
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, "FST Daily Report Downloaded unsuccessful");
        }

        _isPdfGenBusy = false;
    }

    private async Task HandleGenerateExcel()
    {
        _isExcelGenBusy = true;

        FSTRequest.RequestedFileType = "xlsx";

        var response = await TruckTicketService.DownloadFSTDailyWorkTicket(FSTRequest);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.HttpContent.ReadAsByteArrayAsync();
            if (content.Length > 0)
            {
                await FileDownloadService.DownloadFile("FSTDailyWorkTicket.xlsx", content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

                NotificationService.Notify(NotificationSeverity.Success, "FST Daily Report Downloaded successful");
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Info, "No Truck Tickets found for report");
            }
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, "FST Daily Report Downloaded unsuccessful");
        }

        _isExcelGenBusy = false;
    }
}
