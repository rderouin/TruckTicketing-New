using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;

using BlazorDownloadFile;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels.TruckTickets;

using Trident.Api.Search;
using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.Report;

public partial class LandfillDailyReport : BaseRazorComponent
{
    private bool _isPdfLoading;

    private bool _isExcelLoading;

    private IEnumerable<TruckTicketClassViewModel> ClassViewModels = TruckTicketClassViewModel.GetAll();

    private int SelectedClassViewModel = -1;

    [Parameter]
    public LandfillDailyReportRequest Request { get; set; } = new();

    [Inject]
    private IBlazorDownloadFileService FileDownloadService { get; set; }

    [Inject]
    private ITruckTicketService TruckTicketService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    private bool GenerateButtonDisable => _isPdfLoading || _isExcelLoading || !Request.FacilityIds.Any() || Request.FromDate == default || Request.ToDate == default;

    protected override async Task OnInitializedAsync()
    {
        Request.FromDate = Request.ToDate = DateTimeOffset.Now.Date;
        Request.FacilityIds = new();
        await base.OnInitializedAsync();
    }

    private void BeforeFacilityLoad(SearchCriteriaModel criteria)
    {
        criteria.OrderBy = nameof(Facility.SiteId);
        criteria.Filters[nameof(Facility.IsActive)] = true;
        criteria.Filters[nameof(Facility.Type)] = "Lf";
        criteria.PageSize = Int32.MaxValue;
    }

    private void OnFacilityChange(object args)
    {
        Request.FacilityIds = new();
        var ids = ((IEnumerable)args).Cast<Guid>();
        if (!ids.Any())
        {
            return;
        }

        Request.FacilityIds.AddRange(ids);
    }

    private async Task HandleGeneratePdf()
    {
        _isPdfLoading = true;

        Request.RequestedFileType = "pdf";
        Request.SelectedClass = SelectedClassViewModel == -1 ? null : (Class)SelectedClassViewModel;

        var response = await TruckTicketService.DownloadLandfillDailyTicket(Request);
        if (response.IsSuccessStatusCode)
        {
            var data = await response.HttpContent.ReadAsByteArrayAsync();
            if (data?.Length > 0)
            {
                await FileDownloadService.DownloadFile($"LandFillDaily-{DateTime.Today.Date:yyyy-MM-dd}.pdf", await response.HttpContent.ReadAsByteArrayAsync(),
                                                       MediaTypeNames.Application.Pdf);

                NotificationService.Notify(NotificationSeverity.Success, "Report generated successfully.");
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Warning, "No tickets could be found for the parameters specified.");
            }
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, "Something went wrong while trying to generate the report.");
        }

        _isPdfLoading = false;
    }

    private async Task HandleGenerateExcel()
    {
        _isExcelLoading = true;

        Request.RequestedFileType = "xlsx";
        Request.SelectedClass = SelectedClassViewModel == -1 ? null : (Class)SelectedClassViewModel;

        var response = await TruckTicketService.DownloadLandfillDailyTicket(Request);
        if (response.IsSuccessStatusCode)
        {
            var data = await response.HttpContent.ReadAsByteArrayAsync();
            if (data?.Length > 0)
            {
                await FileDownloadService.DownloadFile($"LandFillDaily-{DateTime.Today.Date:yyyy-MM-dd}.xlsx", await response.HttpContent.ReadAsByteArrayAsync(),
                                                       "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

                NotificationService.Notify(NotificationSeverity.Success, "Report generated successfully.");
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Warning, "No tickets could be found for the parameters specified.");
            }
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, "Something went wrong while trying to generate the report.");
        }

        _isExcelLoading = false;
    }
}
