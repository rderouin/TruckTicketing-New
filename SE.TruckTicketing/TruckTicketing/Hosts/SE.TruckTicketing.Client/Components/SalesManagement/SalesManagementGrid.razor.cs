using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.IdentityModel.Tokens;

using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Client.Components.InvoiceComponents;
using SE.TruckTicketing.Client.Components.LoadConfirmationComponents;
using SE.TruckTicketing.Client.Pages.Accounts;
using SE.TruckTicketing.Client.Pages.BillingConfig;
using SE.TruckTicketing.Client.Pages.SalesManagement;
using SE.TruckTicketing.Client.Pages.SourceLocations;
using SE.TruckTicketing.Client.Pages.TruckTickets.New;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Email;
using SE.TruckTicketing.Contracts.Models.LoadConfirmations;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels;

using Trident.Api.Search;
using Trident.Contracts.Api;
using Trident.Contracts.Api.Client;
using Trident.Extensions;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Components.SalesManagement;

public partial class SalesManagementGrid : BaseTruckTicketingComponent
{
    public PagableGridView<SalesLine> _grid;

    private bool _isLoading;

    private SearchResultsModel<SalesLine, SearchCriteriaModel> _results = new();

    [Parameter]
    public EventCallback<int> OnAllCountChange { get; set; }

    [Parameter]
    public EventCallback<int> OnRejectedCountChange { get; set; }

    [Parameter]
    public EventCallback<int> OnApprovedCountChange { get; set; }

    [Parameter]
    public EventCallback<int> OnSalesLinePriceChangeCountChange { get; set; }

    [Inject]
    private ISalesLineService SalesLineService { get; set; }

    [Inject]
    private ILoadConfirmationService LoadConfirmationService { get; set; }

    [Inject]
    public IInvoiceService InvoiceService { get; set; }

    [Inject]
    private ITruckTicketService TruckTicketService { get; set; }

    [Inject]
    private TruckTicketExperienceViewModel ViewModel { get; set; }

    [Parameter]
    public EventCallback<SearchCriteriaModel> BeforeDataLoad { get; set; }

    [Parameter]
    public SalesManagementIndexPage SalesManagementPage { get; set; }

    [Parameter]
    public EventCallback ChildStateChange { get; set; }

    [Parameter]
    public RenderFragment AdditionalColumns { get; set; }

    public IEnumerable<SalesLine> SelectedSalesLines => _grid.SelectedResults;

    private IList<SalesLine> ModifiedSalesLines { get; set; } = new List<SalesLine>();

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        if (SalesManagementPage != null)
        {
            SalesManagementPage.FacilityChangedEvent += FacilityChangeHandler;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _grid.ReloadGrid();
        }
    }

    private void FacilityChangeHandler(object sender, EventArgs e)
    {
        _ = _grid.ReloadGrid();
    }

    public async Task GridReloadSalesLines(SearchCriteriaModel criteria)
    {
        await _grid.SetExternalSearchCriteriaModel(criteria);
    }

    public async Task LoadSalesLines(SearchCriteriaModel criteria)
    {
        _isLoading = true;

        if (BeforeDataLoad.HasDelegate)
        {
            await BeforeDataLoad.InvokeAsync(criteria);
        }

        criteria.Filters.Remove(PreviewSalesLinesFilter.Key);
        criteria.Filters.Remove(AwaitingRemovalAckFilter.Key);

        _results = await SalesLineService.Search(criteria) ?? _results;

        _isLoading = false;

        if (OnAllCountChange.HasDelegate)
        {
            await OnAllCountChange.InvokeAsync(_results.Info.TotalRecords);
        }
        else if (OnRejectedCountChange.HasDelegate)
        {
            await OnRejectedCountChange.InvokeAsync(_results.Info.TotalRecords);
        }
        else if (OnApprovedCountChange.HasDelegate)
        {
            await OnApprovedCountChange.InvokeAsync(_results.Info.TotalRecords);
        }
        else if (OnSalesLinePriceChangeCountChange.HasDelegate)
        {
            await OnSalesLinePriceChangeCountChange.InvokeAsync(_results.Info.TotalRecords);
        }
    }

    public async Task<bool> RefreshPrice()
    {
        var selected = SelectedSalesLines;
        var refreshedSalesLines = await SalesLineService.BulkPriceRefresh(selected);

        foreach (var refreshed in refreshedSalesLines)
        {
            var salesLine = _results.Results.FirstOrDefault(s => s.Id == refreshed.Id, null) ?? new SalesLine();
            salesLine.Rate = refreshed.Rate;
            salesLine.TotalValue = refreshed.TotalValue;
            salesLine.IsRateOverridden = refreshed.IsRateOverridden;
            ModifiedSalesLines.Add(salesLine);
        }

        StateHasChanged();
        await ChildStateChange.InvokeAsync();

        return true;
    }

    public async Task<IEnumerable<SalesLine>> SaveSelectedSalesLines(bool isPublishOnly = false)
    {
        var savedSalesLines = await SalesLineService.BulkSave(new()
        {
            SalesLines = SelectedSalesLines.ToList(),
            IsPublishOnly = isPublishOnly,
        });

        var savedSalesLineIds = savedSalesLines.Select(s => s.Id).ToHashSet();

        // remove saved sales lines from the list of modified sales lines
        if (!ModifiedSalesLines.IsNullOrEmpty())
        {
            ModifiedSalesLines = ModifiedSalesLines.Where(s => !savedSalesLineIds.Contains(s.Id)).ToList();
        }

        _grid.ClearSelectedResults();

        StateHasChanged();
        await ChildStateChange.InvokeAsync();
        return savedSalesLines;
    }

    public async Task<Response<object>> GenerateAdHocLoadConfirmation(SalesLineEmailViewModel viewModel)
    {
        var model = new LoadConfirmationAdhocModel
        {
            SalesLineKeys = SelectedSalesLines.Select(s => s.Key).ToList(),
            AttachmentType = viewModel.AttachmentIndicatorType,
        };

        return await SalesLineService.GenerateAdHocLoadConfirmation(model);
    }

    public async Task<Response<object>> SendAdHocLoadConfirmation(SalesLineEmailViewModel viewModel)
    {
        var selectedLines = SelectedSalesLines.ToList();
        var facilitySiteId = selectedLines.Where(sl => sl.FacilitySiteId.HasText()).Select(sl => sl.FacilitySiteId).ToHashSet();
        var customerId = selectedLines.Where(sl => sl.CustomerId != default).Select(sl => sl.CustomerId).ToHashSet();
        if (facilitySiteId.Count > 1 || customerId.Count > 1)
        {
            throw new InvalidOperationException("Multiple facilities and/or customers selected. Unable to determine the email template.");
        }

        var emailTemplate = new EmailTemplateDeliveryRequestModel
        {
            TemplateKey = EmailTemplateEventNames.AdHocLoadConfirmation,
            Recipients = viewModel.ToRecipients,
            BccRecipients = viewModel.BccRecipients,
            CcRecipients = viewModel.CcRecipients,
            AdHocNote = viewModel.Note,
            ContextBag = new()
            {
                [nameof(SalesLine)] = selectedLines.Select(s => s.Key).ToList().ToJson(),
                [nameof(AttachmentIndicatorType)] = viewModel.AttachmentIndicatorType.ToString(),
                ["SiteId"] = facilitySiteId.First(),
                ["BillingCustomerId"] = customerId.First(),
            },
            AdHocAttachments = new(),
        };

        return await SalesLineService.SendAdHocLoadConfirmation(emailTemplate);
    }

    public async Task<IEnumerable<SalesLine>> RemoveFromLoadConfirmationOrInvoice(IEnumerable<CompositeKey<Guid>> truckTicketKeys)
    {
        return await SalesLineService.RemoveFromLoadConfirmationOrInvoice(truckTicketKeys);
    }

    public async Task<IEnumerable<SalesLine>> SaveModifiedSalesLines(bool isPublishOnly = false)
    {
        var savedSalesLines = await SalesLineService.BulkSave(new()
        {
            SalesLines = ModifiedSalesLines.ToList(),
            IsPublishOnly = isPublishOnly,
        });

        ModifiedSalesLines = new List<SalesLine>();

        await ChildStateChange.InvokeAsync();
        return savedSalesLines;
    }

    public async Task ReloadGrid()
    {
        await _grid.ReloadGrid();
    }

    public int NumberOfSelected()
    {
        return SelectedSalesLines.Count();
    }

    private async Task OpenTruckTicketDialog(Guid truckTicketId)
    {
        var model = await TruckTicketService.GetById(truckTicketId);
        await ViewModel.Initialize(model);

        await DialogService.OpenAsync<NewTruckTicketDetailsPage>("", new(), new()
        {
            Width = "95%",
            Height = "95%",
        });
    }

    private async Task OpenSalesLineDialog(Guid salesLineId)
    {
        var model = await SalesLineService.GetById(salesLineId);

        await DialogService.OpenAsync<SalesLineDetails>($"SalesLine {model.SalesLineNumber}", new()
        {
            { nameof(SalesLineDetails.Model), model },
        }, new()
        {
            Width = "95%",
            Height = "95%",
        });
    }

    private async Task OpenInvoiceDialog(Guid invoiceId)
    {
        var invoice = await InvoiceService.GetById(invoiceId);
        await DialogService.OpenAsync<InvoiceDetails>($"Invoice {invoice.ProformaInvoiceNumber}", new()
        {
            { nameof(InvoiceDetails.Model), invoice },
        }, new()
        {
            Width = "95%",
            Height = "95%",
        });
    }

    private async Task OpenAttachmentsModal(SalesLine salesLine)
    {
        await DialogService.OpenAsync<SalesLineAttachments>($"Viewing {salesLine.SalesLineNumber} Attachments", new()
        {
            { nameof(SalesLineAttachments.SalesLine), salesLine },
            { nameof(SalesLineAttachments.UploadComplete), new EventCallback(this, async () => await _grid.ReloadGrid()) },
        }, new()
        {
            Width = "35%",
            Height = "50%",
        });
    }

    private async Task OpenLoadConfirmationDialog(Guid loadConfirmationId)
    {
        var model = await LoadConfirmationService.GetById(loadConfirmationId);
        await DialogService.OpenAsync<LoadConfirmationDetails>($"Load Confirmation {model.Number}", new()
        {
            { nameof(LoadConfirmationDetails.Model), model },
        }, new()
        {
            Width = "95%",
            Height = "95%",
        });
    }

    private async Task OpenSourceLocationDialog(Guid sourceLocationId)
    {
        await DialogService.OpenAsync<SourceLocationDetailsPage>("", new()
        {
            { nameof(SourceLocationDetailsPage.Id), sourceLocationId },
        }, new()
        {
            Width = "95%",
            Height = "95%",
        });
    }

    private async Task OpenCustomerDialog(Guid customerId)
    {
        await DialogService.OpenAsync<AccountDetailsPage>("", new()
        {
            { nameof(AccountDetailsPage.Id), customerId.ToString() },
        }, new()
        {
            Width = "95%",
            Height = "95%",
        });
    }

    private async Task OpenGeneratorDialog(Guid generatorId)
    {
        await DialogService.OpenAsync<AccountDetailsPage>("", new()
        {
            { nameof(AccountDetailsPage.Id), generatorId.ToString() },
        }, new()
        {
            Width = "95%",
            Height = "95%",
        });
    }

    private async Task OpenBillingConfigurationDialog(Guid? billingConfigurationId, Guid? customerId)
    {
        if (billingConfigurationId == null || billingConfigurationId == Guid.Empty)
        {
            return;
        }

        await DialogService.OpenAsync<BillingConfigurationEdit>("Billing Configuration", new()
        {
            { nameof(BillingConfigurationEdit.Id), billingConfigurationId.Value },
            { nameof(BillingConfigurationEdit.Operation), "edit" },
            { nameof(BillingConfigurationEdit.HideReturnToAccount), true },
            { nameof(BillingConfigurationEdit.AddEditBillingConfiguration), new EventCallback<BillingConfiguration>(this, UpdatedBillingConfiguration) },
            { nameof(BillingConfigurationEdit.CancelAddEditBillingConfiguration), new EventCallback<bool>(this, () => DialogService.Close()) },
        }, new()
        {
            Width = "80%",
            Height = "95%",
        });
    }

    private async Task UpdatedBillingConfiguration(BillingConfiguration billingConfig)
    {
        DialogService.Close();
        await _grid.ReloadGrid();
    }

    private string GetStatusLineStyle(SalesLineStatus status)
    {
        return status switch
               {
                   SalesLineStatus.Preview => "text-warning",
                   SalesLineStatus.Approved => "text-success",
                   SalesLineStatus.Exception => "text-danger",
                   SalesLineStatus.SentToFo => "text-warning",
                   SalesLineStatus.Posted => "text-warning",
                   _ => "text-secondary",
               };
    }

    private string GetAttachmentStyle(AttachmentIndicatorType indicatorType)
    {
        return indicatorType switch
               {
                   AttachmentIndicatorType.InternalExternal => "text-success",
                   AttachmentIndicatorType.Internal => "text-warning",
                   AttachmentIndicatorType.External => "text-warning",
                   AttachmentIndicatorType.Neither => "text-danger",
                   _ => "text-danger",
               };
    }
}
