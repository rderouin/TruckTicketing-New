using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;

using BlazorDownloadFile;

using Microsoft.AspNetCore.Components;

using Radzen;
using Radzen.Blazor;

using SE.TruckTicketing.Client.Components.UserControls;
using SE.TruckTicketing.Client.Utilities;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.SourceLocations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;

namespace SE.TruckTicketing.Client.Components.Report;

public partial class LoadSummaryReport : BaseTruckTicketingComponent
{
    private bool _isLoading;

    private TridentApiDropDownDataGrid<MaterialApproval, Guid> _materialApprovalDataGrid;

    private SourceLocationDropDown<Guid> _sourceLocationDropDown;

    private List<Account> _truckingCompanies = new();

    private RadzenDropDownDataGrid<IEnumerable<Guid>> _truckingCompanyDropDown;

    private List<Guid> _truckingCompanyIds = new();

    public SearchResultsModel<Facility, SearchCriteriaModel> _truckingCompanyResults = new();

    private bool FacilityNotSelected = true;

    private bool IsTruckingCompanyLoaded;

    private bool IsTruckingCompanyLoading;

    private Guid materialApprovalTruckingCompanyId;

    private SearchResultsModel<Account, SearchCriteriaModel> truckingCompanyResults = new();

    private IEnumerable<Guid> TruckingCompanyIds
    {
        get => _truckingCompanyIds;
        set => _truckingCompanyIds = value?.ToList();
    }

    private bool GenerateButtonDisable => _isLoading || Request.MaterialApprovalIds == null || !Request.MaterialApprovalIds.Any();

    [Inject]
    private IServiceBase<Account, Guid> TruckingCompanyService { get; set; }

    [Parameter]
    public LoadSummaryReportRequest Request { get; set; } = new();

    [Inject]
    private IMaterialApprovalService MaterialApprovalService { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Inject]
    private IBlazorDownloadFileService FileDownloadService { get; set; }

    [Inject]
    private ITruckTicketService TruckTicketService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    protected override async Task OnInitializedAsync()
    {
        IsLoading = true;
        IsTruckingCompanyLoaded = false;
        await base.OnInitializedAsync();
        await LoadTruckingCompanies(new());
        IsLoading = false;
    }

    private void OnFacilitiesLoading(SearchCriteriaModel criteria)
    {
        criteria.OrderBy = nameof(Facility.SiteId);
        criteria.Filters[nameof(Facility.Type)] = "Lf";
        criteria.PageSize = Int32.MaxValue;
    }

    private void BeforeTruckingCompanyLoad(SearchCriteriaModel criteria)
    {
        criteria.PageSize = Int32.MaxValue;
        criteria.Filters[nameof(Account.AccountTypes).AsPrimitiveCollectionFilterKey()!] = new CompareModel
        {
            IgnoreCase = true,
            Operator = CompareOperators.contains,
            Value = AccountTypes.TruckingCompany,
        };
    }

    private void OnSourceLocationLoad(SearchCriteriaModel criteria)
    {
        criteria.Filters[nameof(SourceLocation.IsActive)] = true;

        criteria.Filters[nameof(SourceLocation.GeneratorId)] = Request.GeneratorId;
    }

    private async Task OnFacilitySelect(Facility facility)
    {
        if (facility == null)
        {
            Request.LegalEntity = string.Empty;
            Request.FacilityName = string.Empty;
            Request.SiteId = string.Empty;
            FacilityNotSelected = true;
        }
        else
        {
            Request.FacilityId = facility.Id;
            Request.LegalEntity = facility.LegalEntity;
            Request.FacilityName = facility.Name;
            Request.SiteId = facility.SiteId;
            FacilityNotSelected = false;
        }

        await ReloadMaterialApprovals();
    }

    private async Task OnSourceLocationSelect(SourceLocation sourceLocation)
    {
        if (sourceLocation == null)
        {
            Request.SourceLocationName = string.Empty;
        }
        else
        {
            Request.SourceLocationId = sourceLocation.Id;
            Request.SourceLocationName = sourceLocation.FormattedIdentifier;
        }

        await ReloadMaterialApprovals();
    }

    private async Task OnGeneratorSelect(Account generator)
    {
        if (generator == null)
        {
            Request.GeneratorName = string.Empty;
        }
        else
        {
            Request.GeneratorId = generator.Id;
            Request.GeneratorName = generator.Name;
        }

        await _sourceLocationDropDown.Reload();
        await ReloadMaterialApprovals();
    }

    private async Task HandleGenerate()
    {
        _isLoading = true;
        var response = await TruckTicketService.DownloadLoadSummaryTicket(Request);
        if (response.IsSuccessStatusCode)
        {
            var data = await response.HttpContent.ReadAsByteArrayAsync();
            if (data?.Length > 0)
            {
                await FileDownloadService.DownloadFile("LoadSummary.pdf", data,
                                                       MediaTypeNames.Application.Pdf);

                NotificationService.Notify(NotificationSeverity.Success, "Load Summary Report Downloaded successful");
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, "Load Summary Report could not be downloaded");
            }
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, "Load Summary Report could not be downloaded");
        }

        _isLoading = false;
    }

    private void OnMaterialApprovalLoading(SearchCriteriaModel criteria)
    {
        criteria.OrderBy = nameof(MaterialApproval.MaterialApprovalNumber);
        if (Request.FacilityId != default)
        {
            criteria.AddFilter(nameof(MaterialApproval.FacilityId), Request.FacilityId);
        }

        if (Request.GeneratorId != default)
        {
            criteria.AddFilter(nameof(MaterialApproval.GeneratorId), Request.GeneratorId);
        }

        if (Request.SourceLocationId != default)
        {
            criteria.AddFilter(nameof(MaterialApproval.SourceLocationId), Request.SourceLocationId);
        }
    }

    private async Task OnMaterialApprovalSelect(MaterialApproval materialApproval)
    {
        Request.MaterialApprovalIds = new();

        if (materialApproval == null)
        {
            Request.MaterialApprovalIds = null;
        }
        else
        {
            Request.MaterialApprovalIds.Add(materialApproval.Id);
            materialApprovalTruckingCompanyId = materialApproval.TruckingCompanyId;
        }

        await LoadTruckingCompanies(new());
    }

    private void OnTruckingCompanyChange(object args)
    {
        var ids = args as IEnumerable<Guid> ?? Enumerable.Empty<Guid>();
        if (!ids.Any())
        {
            return;
        }

        Request.TruckingCompanyIds.AddRange(ids);
        Request.TruckingCompanyNames = _truckingCompanies.Where(company => Request.TruckingCompanyIds.Contains(company.Id)).Select(t => t.Name).ToList();
    }

    private async Task ReloadMaterialApprovals()
    {
        Request.MaterialApprovalIds = new();
        _materialApprovalDataGrid.MultiSelectValue = null;
        await _materialApprovalDataGrid.Reload();
        await LoadTruckingCompanies(new() { Top = 10 });
    }

    private async Task LoadTruckingCompanies(LoadDataArgs args)
    {
        IsTruckingCompanyLoading = true;
        _truckingCompanyIds = new();
        Request.TruckingCompanyIds = new();
        Request.TruckingCompanyNames = new();
        if (materialApprovalTruckingCompanyId != default)
        {
            IsTruckingCompanyLoaded = false;
            var truckingCompany = await TruckingCompanyService.GetById(materialApprovalTruckingCompanyId);
            _truckingCompanies = new() { truckingCompany };
            truckingCompanyResults = null;
            IsTruckingCompanyLoading = false;
            return;
        }

        if (!IsTruckingCompanyLoaded)
        {
            var searchCriteriaModel = args.ToSearchCriteriaModel();
            BeforeTruckingCompanyLoad(searchCriteriaModel);
            truckingCompanyResults = await TruckingCompanyService!.Search(searchCriteriaModel)!;
            _truckingCompanies = truckingCompanyResults?.Results?.ToList();
            IsTruckingCompanyLoaded = true;
        }

        IsTruckingCompanyLoading = false;
    }
}
