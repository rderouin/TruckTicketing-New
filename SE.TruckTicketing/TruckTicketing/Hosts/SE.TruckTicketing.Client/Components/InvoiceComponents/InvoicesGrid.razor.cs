using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Humanizer;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Radzen;
using Radzen.Blazor;

using SE.Shared.Common.Extensions;
using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Client.Components.SalesManagement;
using SE.TruckTicketing.Client.Components.UserControls;
using SE.TruckTicketing.Client.Pages.Accounts;
using SE.TruckTicketing.Client.Pages.BillingConfig;
using SE.TruckTicketing.Client.Pages.Invoices;
using SE.TruckTicketing.Client.Utilities;
using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Invoices;
using SE.TruckTicketing.Contracts.Models.LoadConfirmations;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels;

using Trident.Api.Search;
using Trident.Contracts.Api;
using Trident.Contracts.Api.Client;
using Trident.Search;
using Trident.UI.Blazor.Components.Grid;

using CompareModelOperator = Trident.Api.Search.CompareOperators;
using CompareOperators = Trident.Search.CompareOperators;

namespace SE.TruckTicketing.Client.Components.InvoiceComponents;

public partial class InvoicesGrid : BaseTruckTicketingComponent
{
    private PagableGridView<Invoice> _grid;

    private bool _isLoading;

    private double NotificationDurationInSeconds = 5000D;

    private SearchResultsModel<Invoice, SearchCriteriaModel> _results = new();

    private Dictionary<Guid, bool> _runningInvoiceActions = new();

    private IJSObjectReference JsModule;

    public SalesManagementGrid SelectedSalesManagementGrid;

    private InvoiceReversalProcess _reversal;

    [Parameter]
    public bool HideFilter { get; set; }

    public int RecordCount { get; set; }

    public int PostedCount { get; set; }

    public int NotPostedCount { get; set; }

    [Parameter]
    public EventCallback ChildStateChange { get; set; }

    public IEnumerable<Invoice> SelectedInvoices => _grid.SelectedResults;

    public IEnumerable<Invoice> DownloadableInvoices => _grid.Results.Results.Where(x => x.Status == InvoiceStatus.Posted || x.Status == InvoiceStatus.AgingUnSent);

    [Parameter]
    public EventCallback<int> OnRecordCountChange { get; set; }

    [Parameter]
    public EventCallback<int> OnPostedCountChange { get; set; }

    [Parameter]
    public EventCallback<int> OnNotPostedCountChange { get; set; }

    [Parameter]
    public EventCallback<SearchCriteriaModel> BeforeDataLoad { get; set; }

    [Parameter]
    public EventCallback ChildStateChanged { get; set; }

    [Parameter]
    public InvoicesPage InvoicesPage { get; set; }

    [Inject]
    private IInvoiceService InvoiceService { get; set; }

    [Inject]
    private ICsvExportService CsvExportService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Inject]
    private ILoadConfirmationService LoadConfirmationService { get; set; }

    [Inject]
    public IServiceProxyBase<Note, Guid> NotesService { get; set; }

    [Inject]
    public ISalesLineService SalesLineService { get; set; }

    [Inject]
    private IJSRuntime JsRuntime { get; set; }

    [Inject]
    public TooltipService TooltipService { get; set; }

    [Inject]
    public IHttpClientFactory HttpClientFactory { get; set; }

    [Inject]
    private IServiceBase<Account, Guid> AccountService { get; set; }

    private EventCallback HandleCancel => new(this, () => DialogService.Close());

    private EventCallback<InvoiceNotesViewModel> HandleInvoiceCollectionUpdate =>
        new(this, (Func<InvoiceNotesViewModel, Task>)(async model =>
        {
            DialogService.Close();
            await UpdateCollectionInfo(model);
        }));

    private EventCallback<ReverseInvoiceRequest> HandleInvoiceReversal =>
        new(this, (Func<ReverseInvoiceRequest, Task>)(async request =>
        {
            DialogService.Close();
            await PostReversal(request);
        }));

    public int NumberOfSelected()
    {
        return SelectedInvoices.Count();
    }

    private async Task UpdateCollectionInfo(InvoiceNotesViewModel model)
    {
        try
        {
            if (model.SelectedInvoices.Any())
            {
                // update each invoice if bulk update
                foreach (var invoice in model.SelectedInvoices)
                {
                    // update invoice collection info
                    await InvoiceService.UpdateCollectionInfo(new()
                    {
                        InvoiceKey = invoice.Key,
                        CollectionOwner = model.CollectionOwner,
                        CollectionReason = model.CollectionReason,
                        CollectionNotes = model.CollectionReasonComment,
                    });

                    // add the comment
                    var comment = model.CollectionReason.Humanize();
                    if (model.CollectionReasonComment.HasText())
                    {
                        comment += $" - {model.CollectionReasonComment}";
                    }

                    await NotesService.Create(new()
                    {
                        ThreadId = $"Invoice|{invoice.Id}",
                        Comment = comment,
                    });
                }

                // success
                NotificationService.Notify(NotificationSeverity.Success, detail: "Invoice update successful.");
            }
        }
        catch (Exception e)
        {
            NotificationService.Notify(NotificationSeverity.Error, detail: "Invoice update unsuccessful.");
            Console.WriteLine(e);
            throw;
        }

        await _grid.ReloadGrid();
    }

    private async Task PostReversal(ReverseInvoiceRequest request)
    {
        _reversal = new() { InvoiceId = request.InvoiceKey.Id, IsReversalInProgress = true, };

        var response = await InvoiceService.ReverseInvoice(request);

        if (response.IsSuccessStatusCode)
        {
            if (!string.IsNullOrEmpty(response.Model.ErrorMessage))
            {
                NotificationService.Notify(NotificationSeverity.Error, detail: response.Model.ErrorMessage, duration: NotificationDurationInSeconds);
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Success, detail: "Invoice reversed successfully.");
            }
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, detail: "Invoice reversal failed.", duration: NotificationDurationInSeconds);
        }

        _reversal.IsReversalInProgress = false;

        await _grid.ReloadGrid();
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        if (InvoicesPage != null)
        {
            InvoicesPage.FacilityChangedEvent += FacilityChangeHandler;
        }

        JsModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/main.js");
    }

    private void FacilityChangeHandler(object sender, EventArgs e)
    {
        _ = _grid.ReloadGrid();
    }

    private bool IsRunningInvoiceAction(Guid invoiceId)
    {
        return _runningInvoiceActions.TryGetValue(invoiceId, out var isRunning) && isRunning;
    }

    public bool IsRunningAnyInvoiceAction(List<Guid> invoiceIds)
    {
        var atLeastOneIsRunning = false;
        foreach (var invoiceId in invoiceIds)
        {
            atLeastOneIsRunning = _runningInvoiceActions.TryGetValue(invoiceId, out var isRunning) && isRunning;
            if (atLeastOneIsRunning)
            {
                break;
            }
        }

        return atLeastOneIsRunning;
    }

    public async Task RunAction(Guid invoiceId, Func<Task> action)
    {
        try
        {
            _runningInvoiceActions[invoiceId] = true;
            StateHasChanged();

            await action();
        }
        finally
        {
            _runningInvoiceActions[invoiceId] = false;
            StateHasChanged();
        }
    }

    private async Task PostedButtonClick(RadzenSplitButtonItem item, Invoice invoice)
    {
        await RunAction(invoice.Id, async () =>
        {
            var request = new PostInvoiceActionRequest
            {
                InvoiceKey = invoice.Key,
                InvoiceStatus = invoice.Status,
            };

            if (item == null)
            {
                await HandleAttachmentDownload(invoice);
                return;
            }

            if (item.Value.Trim().Equals("Resend", StringComparison.OrdinalIgnoreCase))
            {
                request.InvoiceAction = InvoiceAction.Resend;
                // no status update
            }
            else if (item.Value.Trim().Equals("Email", StringComparison.OrdinalIgnoreCase))
            {
                request.InvoiceAction = InvoiceAction.Email;
                // no status update
            }
            else if (item.Value.Trim().Equals("AdvancedEmail", StringComparison.OrdinalIgnoreCase))
            {
                request.InvoiceAction = InvoiceAction.AdvancedEmail;
                // no status update
            }
            else if (item.Value.Trim().Equals("PaidUnSettled", StringComparison.OrdinalIgnoreCase))
            {
                request.InvoiceStatus = InvoiceStatus.PaidUnSettled;
                // no Action
            }
            else if (item.Value.Trim().Equals("Reverse", StringComparison.OrdinalIgnoreCase))
            {
                await OpenInvoiceReverseDialog(invoice);
                return;
            }
            else if (item.Value.Trim().Equals("Regenerate", StringComparison.OrdinalIgnoreCase))
            {
                request.InvoiceAction = InvoiceAction.Regenerate;
            }

            await PostInvoiceAction(request, invoice.CustomerId, invoice.BillingContactId);
        });
    }

    private async Task PublishSalesOrder(CompositeKey<Guid> invoiceKey)
    {
        var response = await InvoiceService.PublishSalesOrder(invoiceKey);
        if (response.IsSuccessStatusCode)
        {
            NotificationService.Notify(new()
            {
                Summary = "Sales Order Published",
                Severity = NotificationSeverity.Success,
            });
        }
        else
        {
            NotificationService.Notify(new()
            {
                Summary = "Failed to publish sales order",
                Severity = NotificationSeverity.Error,
            });
        }
    }

    private async Task UnPostedButtonClick(RadzenSplitButtonItem item, Invoice invoice)
    {
        if (item?.Value.Trim().Equals("Publish", StringComparison.OrdinalIgnoreCase) is true)
        {
            if (await CheckForNonVoidSalesLines(invoice))
            {
                await PublishSalesOrder(invoice.Key);
                return;
            }

            var message = new NotificationMessage
            {
                Detail = "Invoice has no sales lines to publish",
                Duration = 60000,
                Severity = NotificationSeverity.Warning,
                Summary = "The following invoices were not pushed to FO since they do not contain active sales lines: " +
                          string.Join(", ", invoice.ProformaInvoiceNumber),
            };

            NotificationService.Notify(message);
            return;
        }

        var unpostedInvoicesByLC = new List<string>();
        var unpostedInvoicesBySL = new List<string>();
        await RunAction(invoice.Id, async () =>
        {
            var hasSalesLines = await CheckForNonVoidSalesLines(invoice);

            var request = new PostInvoiceActionRequest
            {
                InvoiceKey = invoice.Key,
                InvoiceStatus = invoice.Status,
            };

            if (item == null)
            {
                if (hasSalesLines)
                {
                    if (await CanInvoiceBePosted(invoice))
                    {
                        request.InvoiceAction = InvoiceAction.Post;
                        request.InvoiceStatus = InvoiceStatus.Posted;
                    }
                    else
                    {
                        request.InvoiceAction = InvoiceAction.Undefined;
                        unpostedInvoicesByLC.Add(invoice.ProformaInvoiceNumber);
                    }
                }
                else
                {
                    request.InvoiceAction = InvoiceAction.Undefined;
                    unpostedInvoicesBySL.Add(invoice.ProformaInvoiceNumber);
                }
            }
            else if (item.Value.Trim().Equals("PostOnly", StringComparison.OrdinalIgnoreCase))
            {
                if (hasSalesLines)
                {
                    request.InvoiceAction = InvoiceAction.Post;
                    request.InvoiceStatus = InvoiceStatus.AgingUnSent;
                }
                else
                {
                    request.InvoiceAction = InvoiceAction.Undefined;
                    unpostedInvoicesBySL.Add(invoice.ProformaInvoiceNumber);
                }
            }
            else if (item.Value.Trim().Equals("Void", StringComparison.OrdinalIgnoreCase))
            {
                if (await CheckForActiveSalesLinesAssociation(invoice))
                {
                    NotificationService.Notify(new()
                    {
                        Duration = 60000,
                        Summary = "Unable to Void Invoice.",
                        Detail = "This Invoice contains active Sales Lines and/or Load Confirmations that must removed or voided before the Invoice itself can be voided.",
                        Severity = NotificationSeverity.Error,
                    });

                    return;
                }

                invoice.Status = InvoiceStatus.Void;
                var voidInvoiceResponse = await InvoiceService.VoidInvoice(invoice);

                if (voidInvoiceResponse?.IsSuccessStatusCode == true)
                {
                    NotificationService.Notify(NotificationSeverity.Success, detail: "Invoice voided successfully.");
                }
                else
                {
                    var validationErrors = voidInvoiceResponse != null && voidInvoiceResponse.ResponseContent.HasText()
                                               ? JsonConvert.DeserializeObject<List<ValidationResult>>(voidInvoiceResponse.ResponseContent)
                                               : null;

                    var notificationDetails = validationErrors != null && validationErrors.Any()
                                                  ? string.Join("\n", validationErrors.Select(x => x.Message).ToList())
                                                  : null;

                    NotificationService.Notify(NotificationSeverity.Error, "Invoice void action unsuccessful.", notificationDetails);
                }

                // invoice status may have been updated, so refresh grid
                await _grid.ReloadGrid();
                return;
            }

            if (request.InvoiceAction is InvoiceAction.Post && invoice.IsDeliveredToErp is not true)
            {
                await ShowMessage(new()
                {
                    Text = "This sales order has not been delivered to FO. Please resend the sales order and wait for an acknowledgment receipt before attempting to post.",
                    Title = "Cannot Post Sales Order",
                });

                return;
            }

            if (request.InvoiceAction is not InvoiceAction.Undefined)
            {
                await PostInvoiceAction(request, null, null);
            }
        });

        if (unpostedInvoicesByLC.Any() || unpostedInvoicesBySL.Any())
        {
            var detailText = "Unable to Post & Send Invoices";
            var summaryText = "";
            var itemText = "and Sent ";

            if (unpostedInvoicesByLC.Any())
            {
                summaryText += "The following invoices were not Posted and Sent since they contain load confirmations that are not in the Waiting for Invoice state: " +
                               string.Join(", ", unpostedInvoicesByLC);
            }

            if (unpostedInvoicesBySL.Any())
            {
                if (unpostedInvoicesByLC.Any())
                {
                    summaryText += "\n";
                }
                else if (item != null)
                {
                    detailText = "Unable to Post Invoice";
                    itemText = "";
                }

                summaryText += "The following invoices were not Posted " + itemText + "since they do not contain active sales lines: " +
                               string.Join(", ", unpostedInvoicesBySL);
            }

            var message = new NotificationMessage
            {
                Detail = detailText,
                Duration = 60000,
                Severity = NotificationSeverity.Warning,
                Summary = summaryText,
            };

            NotificationService.Notify(message);
        }
    }

    private async Task<bool> CheckForNonVoidSalesLines(Invoice invoice)
    {
        //Check explicitly for any active SalesLine association with this Invoice
        var associatedSalesLinesWithInvoice = await SalesLineService.Search(new()
        {
            Filters = new()
            {
                [nameof(SalesLine.InvoiceId)] = invoice.Id,
                [nameof(SalesLine.Status)] = new Compare
                {
                    Value = SalesLineStatus.Void.ToString(),
                    Operator = CompareOperators.ne,
                },
            },
            PageSize = 1,
        }) ?? new();

        return associatedSalesLinesWithInvoice.Results.Any();
    }

    private async Task<bool> CheckForActiveSalesLinesAssociation(Invoice invoice)
    {
        //Check explicitly for any active SalesLine/LoadConfirmation association with this Invoice
        var associatedSalesLinesWithInvoice = await SalesLineService.Search(new()
        {
            Filters = new()
            {
                [nameof(SalesLine.InvoiceId)] = invoice.Id,
                [nameof(SalesLine.Status)] = new CompareModel
                {
                    Operator = CompareModelOperator.ne,
                    Value = SalesLineStatus.Void.ToString(),
                },
            },
            PageSize = 1,
        }) ?? new();

        var associatedLoadConfirmationWithInvoice = await LoadConfirmationService.Search(new()
        {
            Filters = new()
            {
                [nameof(LoadConfirmation.InvoiceId)] = invoice.Id,
                [nameof(LoadConfirmation.Status)] = new CompareModel
                {
                    Operator = CompareModelOperator.ne,
                    Value = LoadConfirmationStatus.Void.ToString(),
                },
            },
            PageSize = 1,
        }) ?? new();

        //12516 - Allow automatic void of LC if associated IP is voided
        if (!associatedSalesLinesWithInvoice.Results.Any() && associatedLoadConfirmationWithInvoice.Results.Any())
        {
            var associatedLoadConfirmationNumbers = associatedLoadConfirmationWithInvoice.Results.Select(x => x.Number).ToList();
            var loadConfirmations = string.Join(", ", associatedLoadConfirmationNumbers);
            var msg = $"Are you sure you want to void associated load confirmations {loadConfirmations}";
            var title = "Void Associated Load Confirmation(s)";
            var voidLoadConfirmation = await DialogService.Confirm(msg, title,
                                                                   new()
                                                                   {
                                                                       OkButtonText = "Yes",
                                                                       CancelButtonText = "No",
                                                                   });

            return !voidLoadConfirmation.GetValueOrDefault();
        }

        return associatedLoadConfirmationWithInvoice.Results.Any() || associatedSalesLinesWithInvoice.Results.Any();
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

    private async Task AgingUnsentButtonClick(RadzenSplitButtonItem item, Invoice invoice)
    {
        await RunAction(invoice.Id, async () =>
        {
            var request = new PostInvoiceActionRequest
            {
                InvoiceKey = invoice.Key,
                InvoiceStatus = invoice.Status,
            };

            if (item == null)
            {
                await HandleAttachmentDownload(invoice);
                return;
            }

            if (item.Value.Trim().Equals("Resend", StringComparison.OrdinalIgnoreCase))
            {
                request.InvoiceAction = InvoiceAction.Resend;
                // no status update
            }
            else if (item.Value.Trim().Equals("Email", StringComparison.OrdinalIgnoreCase))
            {
                request.InvoiceAction = InvoiceAction.Email;
                // no status update
            }
            else if (item.Value.Trim().Equals("AdvancedEmail", StringComparison.OrdinalIgnoreCase))
            {
                request.InvoiceAction = InvoiceAction.AdvancedEmail;
                // no status update
            }
            else if (item.Value.Trim().Equals("PostSend", StringComparison.OrdinalIgnoreCase))
            {
                if (await CheckForNonVoidSalesLines(invoice))
                {
                    request.InvoiceAction = InvoiceAction.Post;
                    request.InvoiceStatus = InvoiceStatus.Posted;
                }
                else
                {
                    var message = new NotificationMessage
                    {
                        Detail = "Unable to Post & Send Invoice",
                        Duration = 60000,
                        Severity = NotificationSeverity.Warning,
                        Summary = "The invoice " + invoice.ProformaInvoiceNumber + " was not Posted and Sent since it does not contain active sales lines.",
                    };

                    NotificationService.Notify(message);
                    return;
                }
            }
            else if (item.Value.Trim().Equals("PaidUnSettled", StringComparison.OrdinalIgnoreCase))
            {
                request.InvoiceStatus = InvoiceStatus.PaidUnSettled;
                // no Action
            }
            else if (item.Value.Trim().Equals("Reverse", StringComparison.OrdinalIgnoreCase))
            {
                await OpenInvoiceReverseDialog(invoice);
                return;
            }
            else if (item.Value.Trim().Equals("Regenerate", StringComparison.OrdinalIgnoreCase))
            {
                request.InvoiceAction = InvoiceAction.Regenerate;
            }

            await PostInvoiceAction(request, invoice.CustomerId, invoice.BillingContactId);
        });
    }

    private async Task PostRejectedButtonClick(RadzenSplitButtonItem item, Invoice invoice)
    {
        await RunAction(invoice.Id, async () =>
        {
            var request = new PostInvoiceActionRequest
            {
                InvoiceKey = invoice.Key,
                InvoiceStatus = invoice.Status,
            };

            if (item == null)
            {
                if (await CheckForNonVoidSalesLines(invoice))
                {
                    request.InvoiceAction = InvoiceAction.Post;
                    request.InvoiceStatus = InvoiceStatus.Posted;
                }
                else
                {
                    var message = new NotificationMessage
                    {
                        Detail = "Unable to Post Invoice",
                        Duration = 60000,
                        Severity = NotificationSeverity.Warning,
                        Summary = "The invoice " + invoice.ProformaInvoiceNumber + " was not Posted since it does not contain active sales lines.",
                    };

                    NotificationService.Notify(message);
                    return;
                }
            }
            else if (item.Value == "Reverse")
            {
                await OpenInvoiceReverseDialog(invoice);
                return;
            }
            else if (item.Value == "Regenerate")
            {
                request.InvoiceAction = InvoiceAction.Regenerate;
            }

            await PostInvoiceAction(request, null, null);
        });
    }

    private async Task DefaultButtonClick(RadzenSplitButtonItem item, Invoice invoice)
    {
        await RunAction(invoice.Id, async () =>
        {
            var request = new PostInvoiceActionRequest
            {
                InvoiceKey = invoice.Key,
                InvoiceStatus = invoice.Status,
            };

            if (item == null)
            {
                await HandleAttachmentDownload(invoice);
                return;
            }

            if (item.Value.Trim().Equals("Email", StringComparison.OrdinalIgnoreCase))
            {
                request.InvoiceAction = InvoiceAction.Email;
                // no status update
            }

            if (item.Value.Trim().Equals("AdvancedEmail", StringComparison.OrdinalIgnoreCase))
            {
                request.InvoiceAction = InvoiceAction.AdvancedEmail;
                // no status update
            }

            await PostInvoiceAction(request, invoice.CustomerId, invoice.BillingContactId);
        });
    }

    public async Task PostInvoiceAction(PostInvoiceActionRequest request, Guid? customerId, Guid? billingContactId)
    {
        Response<Invoice, TTErrorCodes> response = null;
        if (request.InvoiceAction == InvoiceAction.AdvancedEmail && customerId.HasValue)
        {
            var account = await AccountService.GetById(customerId.Value);

            var contact = account.Contacts.FirstOrDefault(c => c.Id == billingContactId);

            // fall back to the default billing contact
            var allContacts = account?.Contacts.Where(c =>
                                                          c.ContactFunctions.Contains(AccountContactFunctions.BillingContact.ToString())
                                                       && c.IsActive
                                                       && !c.IsDeleted
                                                       && c.Email.HasText())
                                      .Select(a => new DisplayEmailAddress
                                      {
                                          DisplayName = a.DisplayName,
                                          Email = a.Email,
                                          IsDefault = contact == null ? a.IsPrimaryAccountContact : contact.Email == a.Email ? true : false,
                                      })
                                      .ToList();

            var requestModel = new AdvancedEmailViewModel
            {
                Contacts = allContacts,
                ContactDropdownLabel = "Billing Contact",
            };

            await DialogService.OpenAsync<AdvancedEmailComponent>("Add Additional Information",
                                                                  new()
                                                                  {
                                                                      { nameof(AdvancedEmailComponent.Model), requestModel },
                                                                      { nameof(AdvancedEmailComponent.OnSubmit), new EventCallback<AdvancedEmailViewModel>(this, () => DialogService.Close()) },
                                                                      { nameof(AdvancedEmailComponent.OnCancel), new EventCallback(this, () => DialogService.Close()) },
                                                                  }, new()
                                                                  {
                                                                      Width = "80%",
                                                                      Height = "95%",
                                                                  });

            // cancel clicked, cancel bulk op
            if (requestModel.IsOkToProceed)
            {
                var advancedEmailRequest = new InvoiceAdvancedEmailRequest
                {
                    InvoiceKey = request.InvoiceKey,
                    InvoiceStatus = request.InvoiceStatus,
                    To = requestModel.To,
                    Cc = requestModel.Cc,
                    Bcc = requestModel.Bcc,
                    AdHocNote = requestModel.AdHocNote,
                    IsCustomeEmail = true,
                };

                response = await InvoiceService.InvoiceAdvanceEmailAction(advancedEmailRequest);
            }
        }
        else
        {
            response = await InvoiceService.PostInvoiceAction(request);
        }

        if (response?.IsSuccessStatusCode == true)
        {
            NotificationService.Notify(NotificationSeverity.Success, detail: "Invoice update successful.");
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, detail: $"Invoice update unsuccessful. {response?.ValidationSummary}");
        }

        // invoice status may have been updated, so refresh grid
        await _grid.ReloadGrid();
    }

    private async Task OpenInvoiceDialog(Guid invoiceId)
    {
        var invoice = await InvoiceService.GetById(invoiceId);
        await DialogService.OpenAsync<InvoiceDetails>($"Invoice {invoice.ProformaInvoiceNumber}", new()
        {
            { nameof(InvoiceDetails.Model), invoice },
        }, new()
        {
            Width = "80%",
            Height = "95%",
        });
    }

    protected static string GetPointerEventsStyle(Invoice invoice)
    {
        if (invoice.CollectionOwner == default)
        {
            return "pointer-events: none;color: black";
        }

        return null;
    }

    private async Task OpenChangeCollectionsOwnerDialog(Invoice invoice)
    {
        if (invoice.CollectionOwner == default)
        {
            return;
        }

        var model = new InvoiceNotesViewModel(new[] { invoice });
        await DialogService.OpenAsync<ChangeCollectionOwnerDialog>("Collection Owner",
                                                                   new()
                                                                   {
                                                                       { nameof(ChangeCollectionOwnerDialog.Model), model },
                                                                       { nameof(ChangeCollectionOwnerDialog.OnSubmit), HandleInvoiceCollectionUpdate },
                                                                       { nameof(ChangeCollectionOwnerDialog.OnCancel), HandleCancel },
                                                                       { nameof(ChangeCollectionOwnerDialog.IsReadOnly), true },
                                                                   });
    }

    private async Task OpenInvoiceReverseDialog(Invoice invoice)
    {
        await DialogService.OpenAsync<InvoiceReversalDialog>("Invoice Reversal",
                                                             new()
                                                             {
                                                                 { nameof(InvoiceReversalDialog.Model), new ReverseInvoiceRequest { InvoiceKey = invoice.Key } },
                                                                 { nameof(InvoiceReversalDialog.OnSubmit), HandleInvoiceReversal },
                                                                 { nameof(InvoiceReversalDialog.OnCancel), HandleCancel },
                                                             });
    }

    private async Task Export()
    {
        var exporter = new PagableGridExporter<Invoice>(_grid, CsvExportService);
        await exporter.Export($@"Invoice{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.csv");
    }

    public async Task GridReloadInvoiceGrid(SearchCriteriaModel criteria)
    {
        await _grid.SetExternalSearchCriteriaModel(criteria);
    }

    public async Task LoadData(SearchCriteriaModel criteria)
    {
        _isLoading = true;

        if (BeforeDataLoad.HasDelegate)
        {
            await BeforeDataLoad.InvokeAsync(criteria);
        }

        _results = await InvoiceService.Search(criteria) ?? _results;
        _isLoading = false;

        await InvokeAsync(StateHasChanged);

        if (OnPostedCountChange.HasDelegate)
        {
            await OnPostedCountChange.InvokeAsync(_results.Info.TotalRecords);
        }
        else if (OnNotPostedCountChange.HasDelegate)
        {
            await OnNotPostedCountChange.InvokeAsync(_results.Info.TotalRecords);
        }
        else if (OnRecordCountChange.HasDelegate)
        {
            await OnRecordCountChange.InvokeAsync(_results.Info.TotalRecords);
        }
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

    private async Task OpenBillingConfiguration(List<InvoiceBillingConfiguration> invoiceBillingConfigurations, string billingConfigurationName)
    {
        if (invoiceBillingConfigurations == null || !invoiceBillingConfigurations.Any())
        {
            return;
        }

        var isMultipleBillingConfigurations = invoiceBillingConfigurations.Count > 1;
        if (!isMultipleBillingConfigurations)
        {
            var billingConfiguration = invoiceBillingConfigurations.FirstOrDefault(config => config.BillingConfigurationName == billingConfigurationName, new());
            if (billingConfiguration?.BillingConfigurationId == null || billingConfiguration.BillingConfigurationId == Guid.Empty)
            {
                return;
            }

            await OpenBillingConfigurationDialog(billingConfiguration.BillingConfigurationId.Value);
        }
        else
        {
            //Open control to display multiple billing configurations list
            await DialogService.OpenAsync<BillingConfigurationDataList>("Billing Configurations", new()
            {
                { nameof(BillingConfigurationDataList.ListData), invoiceBillingConfigurations },
                { nameof(BillingConfigurationDataList.Id), "BillingConfigurationDataList" },
                { nameof(BillingConfigurationDataList.OpenSelectedBillingConfiguration), new EventCallback<Guid>(this, OpenBillingConfigurationDialog) },
            });
        }
    }

    private async Task OpenBillingConfigurationDialog(Guid billingConfigurationId)
    {
        await DialogService.OpenAsync<BillingConfigurationEdit>("Billing Configuration", new()
        {
            { nameof(BillingConfigurationEdit.Id), billingConfigurationId },
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

    public async Task HandleAttachmentDownload(Invoice invoice)
    {
        var attachment = invoice.Attachments.FirstOrDefault();
        if (attachment == null)
        {
            NotificationService.Notify(NotificationSeverity.Error, detail: "The invoice hasn't been generated yet.");
            return;
        }

        var uriResponse = await InvoiceService.GetAttachmentDownloadUrl(invoice.Key, attachment.Id);
        if (!uriResponse.IsSuccessStatusCode)
        {
            return;
        }

        var uri = JToken.Parse(uriResponse.Model).ToObject<string>();
        await JsRuntime.InvokeVoidAsync("open", uri, "_blank");
    }

    public async Task RefreshGrid()
    {
        _grid.ClearSelectedResults();
        await _grid.ReloadGrid();
    }

    private void ShowTooltipForDeliveredInvoice(ElementReference elementReference, TooltipOptions options = null)
    {
        TooltipService.Open(elementReference, "Sales Order has been received in F&O.", options);
    }

    private void ShowTooltipForDeliveredWithGlInvoice(ElementReference elementReference, TooltipOptions options = null)
    {
        TooltipService.Open(elementReference, "Invoice has been created in F&O.", options);
    }

    private bool IsReversalInProgress(Guid id)
    {
        if (_reversal != null && _reversal.IsReversalInProgress && _reversal.InvoiceId.Equals(id))
        {
            return true;
        }

        return false;
    }

    private static string RedHighlight(Invoice ip)
    {
        if (ip.SalesLineCount == 0 && ip.Status != InvoiceStatus.Void)
        {
            return "redhighlight";
        }

        return "";
    }

    private class ValidationResult
    {
        public string Message { get; set; }

        public string ErrorCode { get; set; }
    }

    private class InvoiceReversalProcess
    {
        public Guid InvoiceId { get; set; }
        public bool IsReversalInProgress { get; set; }
    }
}
