using System.Collections.Generic;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Linq;

using BlazorDownloadFile;

using Microsoft.AspNetCore.Components;

using Radzen;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Client.Components.GridFilters;
using SE.TruckTicketing.Client.Components.UserControls;
using SE.TruckTicketing.Client.Utilities;
using SE.TruckTicketing.Contracts.Models.Email;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Security;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.Extensions;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Pages.MaterialApproval;

public partial class MaterialApprovalIndex : BaseTruckTicketingComponent
{
    private const string EditBasePath = "/material-approval/edit";

    private const string CloneBasePath = "/material-approval/clone";

    private PagableGridView<Contracts.Models.Operations.MaterialApproval> _grid;

    private GridFiltersContainer _gridFilterContainer;

    private bool _isLoading;

    private SearchResultsModel<Contracts.Models.Operations.MaterialApproval, SearchCriteriaModel> _results = new();

    [Inject]
    private ICsvExportService CsvExportService { get; set; }

    [Inject]
    private IBlazorDownloadFileService FileDownloadService { get; set; }

    [Inject]
    private IMaterialApprovalService MaterialApprovalService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Inject]
    private IFacilityServiceService FacilityServices { get; set; }

    [Inject]
    private IServiceTypeService ServiceTypeService { get; set; }

    [Inject]
    public TooltipService TooltipService { get; set; }

    private bool HasMaterialApprovalWritePermission => HasWritePermission(Permissions.Resources.MaterialApproval);

    private string AddMaterialApprovalLink_Css => GetLink_CssClass(HasMaterialApprovalWritePermission);

    private EmailTemplateSenderDialog _dialog = new();

    private Contracts.Models.Operations.MaterialApproval EmailModel { get; set; }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _gridFilterContainer.Reload();
        }
    }

    private async Task Export()
    {
        var exporter = new PagableGridExporter<Contracts.Models.Operations.MaterialApproval>(_grid, CsvExportService);
        await exporter.Export("material-approval.csv");
    }

    private async Task LoadData(SearchCriteriaModel current)
    {
        _isLoading = true;
        _results = await MaterialApprovalService.Search(current) ?? _results;
        _isLoading = false;

        StateHasChanged();
    }

    private async Task EditButton_Click(Contracts.Models.Operations.MaterialApproval model)
    {
        NavigationManager.NavigateTo($"{EditBasePath}/{model.Id}");
        await Task.CompletedTask;
    }

    private async Task CloneButton_Click(Contracts.Models.Operations.MaterialApproval model)
    {
        NavigationManager.NavigateTo($"{CloneBasePath}/{model.Id}");
        await Task.CompletedTask;
    }

    private async Task PrintButton_Click(Contracts.Models.Operations.MaterialApproval model)
    {
        _isLoading = true;
        var response = await MaterialApprovalService.DownloadMaterialApprovalPdf(model.Id);
        if (response.IsSuccessStatusCode)
        {
            await FileDownloadService.DownloadFile($"{model.MaterialApprovalNumber}.pdf", await response.HttpContent.ReadAsByteArrayAsync(),
                                                   MediaTypeNames.Application.Pdf);
        }
        else if (response.StatusCode != HttpStatusCode.BadRequest)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Failed to download scale ticket with material approval info.");
        }

        _isLoading = false;
    }

    private async Task EmailButton_Click(Contracts.Models.Operations.MaterialApproval model)
    {
        EmailModel = model;
        await _dialog.Open();
    }

    private async Task OnRequest(EmailTemplateDeliveryRequestModel model)
    {
        model.ContextBag = new() { { nameof(Contracts.Models.Operations.MaterialApproval), EmailModel.ToJson() } };

        var facilityService = await FacilityServices.GetById(EmailModel.FacilityServiceId);
        if (facilityService != null)
        {
            var serviceType = await ServiceTypeService.GetById(facilityService.ServiceTypeId);

            model.ContextBag["Class"] = serviceType.Class.ToJson();
        }

        var response = await MaterialApprovalService.DownloadMaterialApprovalPdf(EmailModel.Id);
        if (response.IsSuccessStatusCode)
        {
            var pdfBytes = await response.HttpContent.ReadAsByteArrayAsync();
            model.ContextBag["PDF"] = pdfBytes.ToJson();
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, "Failed to download scale ticket with material approval info.");
        }

        await Task.CompletedTask;
    }
    
    public string RetrieveMASignatoryNames(Contracts.Models.Operations.MaterialApproval t)
    {
        t.SignatoryNames = string.Join(", ", t.ApplicantSignatories.Select(sig => sig.SignatoryName));
        return t.SignatoryNames;
    }

    void ShowTooltip(ElementReference element, string text)
    {
        TooltipService.Open(element, text);
    }
}

