using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

using Humanizer;

using Microsoft.AspNetCore.Components;

using Newtonsoft.Json;

using Radzen;
using Radzen.Blazor;

using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Client.Components.GridFilters;
using SE.TruckTicketing.Client.Components.InvoiceComponents;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Invoices;
using SE.TruckTicketing.Contracts.Models.LoadConfirmations;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;
using Trident.Search;

using CompareOperators = Trident.Search.CompareOperators;

namespace SE.TruckTicketing.Client.Pages.Invoices;

public partial class InvoicesPage : BaseTruckTicketingComponent
{
    private RadzenDropDownDataGrid<IEnumerable<Guid>> _facilityDataGrid;

    private List<Facility> _facilityRecords = new();

    private GridFiltersContainer _gridFilterContainer;

    public SearchResultsModel<Facility, SearchCriteriaModel> facilityResults = new();

    [Parameter]
    public string FacilityIdsUrl { get; set; }

    [Parameter]
    public IEnumerable<Guid> FacilityIds { get; set; }

    [Parameter]
    public string InvoiceNumber { get; set; }

    private int RecordCount { get; set; }

    private InvoicesGrid _invoiceGrid { get; set; }

    private InvoicesGrid _postedInvoiceGrid { get; set; }

    private InvoicesGrid _notPostedInvoiceGrid { get; set; }

    private int PostedCount { get; set; }

    private int NotPostedCount { get; set; }

    private bool _isLoading { get; set; }

    private string AllCountText => $"All {RecordCount}";

    private string PostedCountText => $"Posted {PostedCount}";

    private string NotPostedCountText => $"Not Posted {NotPostedCount}";

    private int selectedTabIndex { get; set; }

    [Inject]
    private IServiceBase<Facility, Guid> FacilityService { get; set; }

    [Inject]
    private IInvoiceService InvoiceService { get; set; }

    [Inject]
    private ILoadConfirmationService LoadConfirmationService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Inject]
    public IServiceProxyBase<Note, Guid> NotesService { get; set; }

    private EventCallback HandleCancel => new(this, () => DialogService.Close());

    private EventCallback<InvoiceNotesViewModel> HandleMassCollectionNotes =>
        new(this, (Action<InvoiceNotesViewModel>)(async model =>
                                                  {
                                                      DialogService.Close();
                                                      await MassCollectionUpdate(model);
                                                  }));

    private bool NotDownloadable
    {
        get
        {
            // Disable Download button if no invoices are selected or there are no invoices that can be downloaded
            if (_invoiceGrid == null || !_invoiceGrid.SelectedInvoices.Any() || !_invoiceGrid.DownloadableInvoices.Any())
            {
                return true;
            }

            // get list of invoice ids that can be downloaded
            var downloadableInvoicesIds = _invoiceGrid.DownloadableInvoices.Select(i => i.Id);

            // get list of selected invoices that can be downloaded in downloadable list
            var listofSelecedDownloadableInvoices = _invoiceGrid.SelectedInvoices.Where(i => downloadableInvoicesIds.Contains(i.Id));

            // if the two list counts match then all selected invoices can be downloaded, otherwise disable button
            return _invoiceGrid.SelectedInvoices.Count() != listofSelecedDownloadableInvoices.Count();
        }
    }

    public event EventHandler FacilityChangedEvent;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        FacilityIds = FacilityIdsUrl != null ? JsonConvert.DeserializeObject<IEnumerable<Guid>>(HttpUtility.UrlDecode(FacilityIdsUrl)) : FacilityIds;
        await LoadFacilityData(new());
    }

    private void HandleFacilityChange(object args)
    {
        FacilityChangedEvent?.Invoke(this, EventArgs.Empty);
    }

    private async Task LoadFacilityData(LoadDataArgs args)
    {
        var searchCriteriaModel = args.ToSearchCriteriaModel();
        BeforeFacilityLoad(searchCriteriaModel);
        facilityResults = await FacilityService!.Search(searchCriteriaModel)!;
        _facilityRecords = facilityResults?.Results?.ToList();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _gridFilterContainer.Reload();
        }
    }

    private void HandleFacilityChange(IEnumerable<Facility> facilities)
    {
        FacilityChangedEvent?.Invoke(this, EventArgs.Empty);
    }

    private void BeforeFacilityLoad(SearchCriteriaModel criteria)
    {
        criteria.Filters[nameof(Facility.IsActive)] = true;
        criteria.PageSize = Int32.MaxValue;
    }

    private async Task ApplyFilterOnTabChange(SearchCriteriaModel model)
    {
        await _invoiceGrid.GridReloadInvoiceGrid(model);
        await _postedInvoiceGrid.GridReloadInvoiceGrid(model);
        await _notPostedInvoiceGrid.GridReloadInvoiceGrid(model);
    }

    private void HandleFacilityFilter(SearchCriteriaModel criteria)
    {
        var values = FacilityIds?.ToArray() ?? Array.Empty<Guid>();

        if (!values.Any())
        {
            criteria.Filters.Remove(nameof(SalesLine.FacilityId));
        }

        IJunction query = AxiomFilterBuilder.CreateFilter().StartGroup();

        var index = 0;

        foreach (var id in values)
        {
            if (query is GroupStart groupstart)
            {
                query = groupstart.AddAxiom(new()
                {
                    Key = (nameof(SalesLine.FacilityId) + ++index).Replace(".", string.Empty),
                    Field = nameof(SalesLine.FacilityId),
                    Operator = CompareOperators.eq,
                    Value = id,
                });
            }
            else if (query is AxiomTokenizer axiom)
            {
                query = axiom.Or().AddAxiom(new()
                {
                    Key = (nameof(SalesLine.FacilityId) + ++index).Replace(".", string.Empty),
                    Field = nameof(SalesLine.FacilityId),
                    Operator = CompareOperators.eq,
                    Value = id,
                });
            }

            criteria.Filters[nameof(SalesLine.FacilityId)] = ((AxiomTokenizer)query).EndGroup().Build();
        }
    }

    private void LoadAllData(SearchCriteriaModel criteria)
    {
        if (InvoiceNumber.HasText())
        {
            criteria.Filters[nameof(Invoice.ProformaInvoiceNumber)] = InvoiceNumber;
        }
        else
        {
            criteria.Filters.Remove(nameof(Invoice.ProformaInvoiceNumber));
        }

        HandleFacilityFilter(criteria);
    }

    private void LoadPostedData(SearchCriteriaModel criteria)
    {
        HandleFacilityFilter(criteria);
        criteria.Filters[nameof(Invoice.Status)] = InvoiceStatus.Posted.ToString();
    }

    private void LoadNotPostedData(SearchCriteriaModel criteria)
    {
        HandleFacilityFilter(criteria);
        criteria.Filters[nameof(Invoice.Status)] = InvoiceStatus.UnPosted.ToString();
    }

    private void RecordCountHandler(int count)
    {
        RecordCount = count;
    }

    private void PostedCountHandler(int count)
    {
        PostedCount = count;
    }

    private void NotPostedCountHandler(int count)
    {
        NotPostedCount = count;
    }

    private bool SelectedGridHasSelectedInvoices()
    {
        switch (selectedTabIndex)
        {
            case 0: { return _invoiceGrid?.SelectedInvoices == null ? false : _invoiceGrid.SelectedInvoices.Any(); }
            case 1: { return _postedInvoiceGrid?.SelectedInvoices == null ? false : _postedInvoiceGrid.SelectedInvoices.Any(); }
            case 2: { return _notPostedInvoiceGrid?.SelectedInvoices == null ? false : _notPostedInvoiceGrid.SelectedInvoices.Any(); }
            default: return false;
        }
    }

    private async Task HandleBulkNoteUpload()
    {
        InvoicesGrid selectedGrid = null;

        switch (selectedTabIndex)
        {
            case 0: { selectedGrid = _invoiceGrid; break; }
            case 1: { selectedGrid = _postedInvoiceGrid; break; }
            case 2: { selectedGrid = _notPostedInvoiceGrid; break; }
        }

        if (selectedGrid != null && selectedGrid.SelectedInvoices.Any())
        {
            var model = new InvoiceNotesViewModel(selectedGrid.SelectedInvoices.ToList());
            model.CollectionOwner = InvoiceCollectionOwners.Unknown;
            model.CollectionReason = InvoiceCollectionReason.None;
            model.CollectionReasonComment = null;
            await DialogService.OpenAsync<ChangeCollectionOwnerDialog>("Update Collection Owner and Notes",
                                                                       new()
                                                                       {
                                                                           { nameof(ChangeCollectionOwnerDialog.Model), model },
                                                                           { nameof(ChangeCollectionOwnerDialog.OnSubmit), HandleMassCollectionNotes },
                                                                           { nameof(ChangeCollectionOwnerDialog.OnCancel), HandleCancel },
                                                                           { nameof(ChangeCollectionOwnerDialog.IsReadOnly), false },
                                                                       });
        }
    }

    private async Task HandleUnPostedButtonClick(RadzenSplitButtonItem item)
    {
        if (_notPostedInvoiceGrid.SelectedInvoices.Any())
        {
            var unpostedInvoices = new List<string>();
            foreach (var invoice in _notPostedInvoiceGrid.SelectedInvoices)
            {
                await _notPostedInvoiceGrid.RunAction(invoice.Id, async () =>
                                                                  {
                                                                      var request = new PostInvoiceActionRequest
                                                                      {
                                                                          InvoiceKey = invoice.Key,
                                                                          InvoiceStatus = invoice.Status,
                                                                      };

                                                                      if (item == null)
                                                                      {
                                                                          if (await CanInvoiceBePosted(invoice))
                                                                          {
                                                                              request.InvoiceAction = InvoiceAction.Post;
                                                                              request.InvoiceStatus = InvoiceStatus.Posted;
                                                                          }
                                                                          else
                                                                          {
                                                                              request.InvoiceAction = InvoiceAction.Undefined;
                                                                              unpostedInvoices.Add(invoice.ProformaInvoiceNumber);
                                                                          }
                                                                      }
                                                                      else if (item.Value.Trim().Equals("PostOnly", StringComparison.OrdinalIgnoreCase) ||
                                                                               item.Value.Trim().Equals("PostNotify", StringComparison.OrdinalIgnoreCase))
                                                                      {
                                                                          request.InvoiceAction = InvoiceAction.Post;
                                                                          request.InvoiceStatus = InvoiceStatus.AgingUnSent;
                                                                      }

                                                                      if (request.InvoiceAction is not InvoiceAction.Undefined)
                                                                      {
                                                                          await _notPostedInvoiceGrid.PostInvoiceAction(request, null, null);
                                                                      }
                                                                  });
            }

            if (unpostedInvoices.Any())
            {
                var message = new NotificationMessage
                {
                    Detail = "Unposted Invoices",
                    Duration = 60000,
                    Severity = NotificationSeverity.Warning,
                    Summary = "The following invoices were not Posted and Sent since they contain load confirmations that are not in the Waiting for Invoice state: " +
                              string.Join(", ", unpostedInvoices),
                };

                NotificationService.Notify(message);
            }
        }
    }

    private async Task<bool> CanInvoiceBePosted(Invoice invoice)
    {
        var criteria = new SearchCriteriaModel
        {
            PageSize = 1,
            Filters = new()
            {
                [nameof(LoadConfirmation.InvoiceId)] = invoice.Id,
                [nameof(LoadConfirmation.Status)] = AxiomFilterBuilder.CreateFilter()
                                                                      .StartGroup()
                                                                      .AddAxiom(new()
                                                                       {
                                                                           Key = "Status1",
                                                                           Field = nameof(LoadConfirmation.Status),
                                                                           Operator = CompareOperators.ne,
                                                                           Value = LoadConfirmationStatus.Void.ToString(),
                                                                       })
                                                                      .And()
                                                                      .AddAxiom(new()
                                                                       {
                                                                           Key = "Status2",
                                                                           Field = nameof(LoadConfirmation.Status),
                                                                           Operator = CompareOperators.ne,
                                                                           Value = LoadConfirmationStatus.WaitingForInvoice.ToString(),
                                                                       })
                                                                      .EndGroup()
                                                                      .Build(),
            },
        };

        var results = await LoadConfirmationService.Search(criteria);
        return results?.Info.TotalRecords == 0;
    }

    private async Task MassCollectionUpdate(InvoiceNotesViewModel model)
    {
        var updatedInvoices = 0;
        foreach (var selectedInvoice in model.SelectedInvoices)
        {
            // update the invoice
            await InvoiceService.UpdateCollectionInfo(new()
            {
                InvoiceKey = selectedInvoice.Key,
                CollectionOwner = model.CollectionOwner,
                CollectionReason = model.CollectionReason,
                CollectionNotes = model.CollectionReasonComment,
            });

            updatedInvoices++;

            // update the comment
            var comment = $"{model.CollectionOwner.Humanize()} - {model.CollectionReason.Humanize()}";
            if (model.CollectionReasonComment.HasText())
            {
                comment += $" - {model.CollectionReasonComment}";
            }

            await NotesService.Create(new()
            {
                ThreadId = $"Invoice|{selectedInvoice.Id}",
                Comment = comment,
            });
        }

        await _invoiceGrid.RefreshGrid();

        if (updatedInvoices > 0)
        {
            NotificationService.Notify(NotificationSeverity.Success, detail: "Collection Owner change and notes updated on invoice(s) successful.");
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, detail: "Collection Owner change and notes updated on invoice(s) unsuccessful.");
        }
    }

    private async Task HandleBulkDownload()
    {
        if (_invoiceGrid.SelectedInvoices.Any())
        {
            var selectedInvoices = _invoiceGrid.SelectedInvoices.ToList();
            foreach (var invoice in selectedInvoices)
            {
                await _invoiceGrid.HandleAttachmentDownload(invoice);
            }
        }
    }

    private async Task RemoveInvoiceFilter()
    {
        if (InvoiceNumber.HasText())
        {
            InvoiceNumber = null;
        }

        await _invoiceGrid.RefreshGrid();
    }
}
