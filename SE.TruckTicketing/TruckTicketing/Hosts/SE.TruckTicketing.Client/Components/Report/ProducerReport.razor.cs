using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;

using BlazorDownloadFile;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Client.Utilities;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.SourceLocations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.Report;

public partial class ProducerReport : BaseRazorComponent
{
    [Inject]
    private NotificationService NotificationService { get; set; }

    private bool _isLoading;

    private bool GenerateButtonDisable => _isLoading || !Request.GeneratorIds.Any() || Request.FromDate == default || Request.ToDate == default;

    [Parameter]
    public ProducerReportRequest Request { get; set; } = new();

    [Inject]
    private IBlazorDownloadFileService FileDownloadService { get; set; }

    [Inject]
    private ITruckTicketService TruckTicketService { get; set; }

    private TridentApiDropDownDataGrid<SourceLocation, Guid> SourceLocationDataGrid { get; set; }

    private TridentApiDropDownDataGrid<ServiceType, Guid> _serviceTypeDropDown { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Request.FromDate = Request.ToDate = DateTimeOffset.Now.Date;
        await base.OnInitializedAsync();
    }

    private static void OnGeneratorLoading(SearchCriteriaModel criteria)
    {
        criteria.Filters[nameof(Account.AccountTypes).AsPrimitiveCollectionFilterKey()!] = new CompareModel
        {
            IgnoreCase = true,
            Operator = CompareOperators.contains,
            Value = AccountTypes.Generator.ToString(),
        };
    }

    private void OnSourceLocationLoading(SearchCriteriaModel criteria)
    {
        criteria.AddFilter(nameof(SourceLocation.IsActive), true);
        if (Request.GeneratorIds.Any())
        {
            criteria.Filters[nameof(SourceLocation.GeneratorId)] =
                Request.GeneratorIds.AsInclusionAxiomFilter(nameof(SourceLocation.GeneratorId), (Trident.Search.CompareOperators)CompareOperators.eq);
        }
    }

    private static void OnTruckingCompanyLoading(SearchCriteriaModel criteria)
    {
        criteria.Filters[nameof(Account.AccountTypes).AsPrimitiveCollectionFilterKey()!] = new CompareModel
        {
            IgnoreCase = true,
            Operator = CompareOperators.contains,
            Value = AccountTypes.TruckingCompany.ToString(),
        };
    }

    private void OnTruckingCompanyChange(object args)
    {
        Request.TruckingCompanyIds = new();
        var ids = args as IEnumerable<Guid> ?? Enumerable.Empty<Guid>();

        if (!ids.Any())
        {
            return;
        }

        Request.TruckingCompanyIds.AddRange(ids);
    }

    private void OnSourceLocationChange(object args)
    {
        Request.SourceLocationIds = new();
        var ids = args as IEnumerable<Guid> ?? Enumerable.Empty<Guid>();

        if (!ids.Any())
        {
            return;
        }

        Request.SourceLocationIds.AddRange(ids);
    }

    private static void OnFacilitiesLoading(SearchCriteriaModel criteria)
    {
        criteria.AddFilter(nameof(Facility.IsActive), true);
    }

    private async Task OnFacilityChange(object args)
    {
        Request.FacilityIds = new();
        var ids = args as IEnumerable<Guid> ?? Enumerable.Empty<Guid>();

        if (ids.Any())
        {
            Request.FacilityIds.AddRange(ids);
        }

        await RefreshServiceType();
    }

    private async Task RefreshServiceType()
    {
        await _serviceTypeDropDown.Refresh(Request.FacilityIds);
    }

    private async Task OnGeneratorChange(object args)
    {
        Request.GeneratorIds = new();
        var ids = args as IEnumerable<Guid> ?? Enumerable.Empty<Guid>();

        if (ids.Any())
        {
            Request.GeneratorIds.AddRange(ids);
        }

        await SourceLocationDataGrid.Reload();
    }

    private void OnServiceTypeChange(object args)
    {
        Request.ServiceTypeIds = new();
        var ids = args as IEnumerable<Guid> ?? Enumerable.Empty<Guid>();

        if (ids.Any())
        {
            Request.ServiceTypeIds.AddRange(ids);
        }
    }

    private async Task HandleGenerate()
    {
        _isLoading = true;

        var response = await TruckTicketService.DownloadProducerReport(Request);
        if (response.IsSuccessStatusCode)
        {
            var data = await response.HttpContent.ReadAsByteArrayAsync();
            if (data?.Length > 0)
            {
                await FileDownloadService.DownloadFile($"ProducerReport-{DateTime.Today.Date:yyyy-MM-dd}.pdf", await response.HttpContent.ReadAsByteArrayAsync(),
                                                       MediaTypeNames.Application.Pdf);

                NotificationService.Notify(NotificationSeverity.Success, "Producer Report downloaded successfully.");
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Warning, "No data found for selected parameters.");
            }
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, "Something went wrong while trying to generate the report.");
        }

        _isLoading = false;
    }
}
