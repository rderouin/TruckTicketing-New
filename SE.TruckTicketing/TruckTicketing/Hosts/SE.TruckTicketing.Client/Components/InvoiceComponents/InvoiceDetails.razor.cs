using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Humanizer;

using Microsoft.AspNetCore.Components;
using Radzen;
using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Acknowledgement;
using SE.TruckTicketing.Contracts.Models.Invoices;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.Shared.Common.Extensions;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;
using SE.TruckTicketing.Client.Components.UserControls;

namespace SE.TruckTicketing.Client.Components.InvoiceComponents;

public partial class InvoiceDetails : BaseTruckTicketingComponent
{
    private Account _customer = new();

    private Notes _notes;

    [Parameter]
    public Invoice Model { get; set; }

    [Inject]
    public IServiceBase<Note, Guid> NotesService { get; set; }

    [Inject]
    private IServiceProxyBase<Account, Guid> AccountService { get; set; }

    [Inject]
    private IServiceProxyBase<Acknowledgement, Guid> AcknowledgementService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Inject]
    private IInvoiceService InvoiceService { get; set; }

    private string ThreadId => $"Invoice|{Model.Id}";

    public bool IsPostedInvoice => Model.Status == InvoiceStatus.Posted;

    protected override async Task OnInitializedAsync()
    {
        if (Model.Status == InvoiceStatus.Posted)
        {
            await DisplayGeneralCreditStatusMessage();
        }

        await base.OnInitializedAsync();
    }

    private async Task DisplayGeneralCreditStatusMessage()
    {
        if (Model.CustomerId == Guid.Empty)
        {
            return;
        }

        _customer = await AccountService.GetById(Model.CustomerId);

        if (_customer == null)
        {
            return;
        }

        List<Acknowledgement> _acknowledgements = new();
        var customerCreditStatus = _customer.CreditStatus switch
                                   {
                                       CreditStatus.RequiresRenewal => CreditStatus.RequiresRenewal.Humanize(),
                                       CreditStatus.Denied => CreditStatus.Denied.Humanize(),
                                       _ => string.Empty,
                                   };

        var customerWatchList = _customer.WatchListStatus switch
                                {
                                    WatchListStatus.Yellow => WatchListStatus.Yellow.Humanize(),
                                    WatchListStatus.Red => WatchListStatus.Red.Humanize(),
                                    _ => string.Empty,
                                };

        var creditStatusMessage =
            $"Customer credit status is {customerCreditStatus}.  Please alert GM, Manager, Lead Advisor, Peter Maros, Kari Nyland and SES Marketing Accounting.  Make plans to collect payment through credit card or prepayment for future loads.";

        var watchListMessage =
            $"Customer credit watchlist is {customerWatchList}.  Please alert GM, Manager, Lead Advisor, Peter Maros, Kari Nyland and SES Marketing Accounting.  Make plans to collect payment through credit card or prepayment for future loads.";

        if (_customer.EnableCreditMessagingGeneral != null)
        {
            if (_customer.EnableCreditMessagingGeneral.Value)
            {
                var creditMessagingStatusTitle = $"Customer credit status: {customerCreditStatus}";
                if (!string.IsNullOrEmpty(customerCreditStatus))
                {
                    var acknowledgementConfirmed = await DialogService.Confirm(creditStatusMessage, creditMessagingStatusTitle,
                                                                               new()
                                                                               {
                                                                                   OkButtonText = "Acknowledge",
                                                                                   CancelButtonText = "Cancel",
                                                                               });

                    if (acknowledgementConfirmed.GetValueOrDefault())
                    {
                        _acknowledgements.Add(new()
                        {
                            ReferenceEntityId = Model.Id,
                            Status = customerCreditStatus,
                        });
                    }
                }

                if (_customer.NetOff != null)
                {
                    if (_customer.NetOff.Value)
                    {
                        var watchListTitle = "Customer has Net Off";
                        var acknowledgementConfirmed = await DialogService.Confirm(watchListMessage, watchListTitle,
                                                                                   new()
                                                                                   {
                                                                                       OkButtonText = "Acknowledge",
                                                                                       CancelButtonText = "Cancel",
                                                                                   });

                        if (acknowledgementConfirmed.GetValueOrDefault())
                        {
                            _acknowledgements.Add(new()
                            {
                                ReferenceEntityId = Model.Id,
                                Status = "Net Off",
                            });
                        }
                    }
                }

                foreach (var ack in _acknowledgements)
                {
                    await AcknowledgementService.Create(ack);
                }
            }
        }

        if (_customer.EnableCreditMessagingRedFlag != null)
        {
            if (_customer.EnableCreditMessagingRedFlag.Value && _customer.WatchListStatus == WatchListStatus.Red)
            {
                var watchListTitle = $"Customer credit watchlist: {customerWatchList}";
                var acknowledgementConfirmed = await DialogService.Confirm(watchListMessage, watchListTitle,
                                                                           new()
                                                                           {
                                                                               OkButtonText = "Acknowledge",
                                                                               CancelButtonText = "Cancel",
                                                                           });

                if (acknowledgementConfirmed.GetValueOrDefault())
                {
                    _acknowledgements.Add(new()
                    {
                        ReferenceEntityId = Model.Id,
                        Status = customerWatchList,
                    });
                }
            }
        }
    }

    private async Task<bool> HandleNoteUpdate(Note note)
    {
        var response = note.Id == default ? await NotesService.Create(note) : await NotesService.Update(note);
        return response.IsSuccessStatusCode;
    }

    private async Task<SearchResultsModel<Note, SearchCriteriaModel>> OnDataLoad(SearchCriteriaModel criteria)
    {
        return await NotesService.Search(criteria);
    }

    public async Task OpenAsModal()
    {
        await DialogService.OpenAsync<InvoiceDetails>($"Invoice {Model.GlInvoiceNumber}",
                                                      new() { { nameof(Model), Model } },
                                                      new()
                                                      {
                                                          Height = "80%",
                                                          Width = "80%",
                                                      });
    }

    private EventCallback HandleCancel => new(this, () => DialogService.Close());

    private EventCallback<InvoiceNotesViewModel> HandleInvoiceCollectionUpdate =>
        new(this, (Func<InvoiceNotesViewModel, Task>)(async model =>
        {
            DialogService.Close();
            await UpdateCollectionInfo(model);
        }));

    private async Task UpdateCollectionInfo(InvoiceNotesViewModel model)
    {
        try
        {            
            var invoice = model.SelectedInvoices.First(); // there will only be a single invoice
            await InvoiceService.UpdateCollectionInfo(new()
            {
                InvoiceKey = invoice.Key,
                CollectionOwner = model.CollectionOwner,
                CollectionReason = model.CollectionReason,
                CollectionNotes = model.CollectionReasonComment,
            });

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

            await _notes.Reload();

            NotificationService.Notify(NotificationSeverity.Success, detail: "Invoice update successful.");
        }
        catch (Exception e)
        {
            NotificationService.Notify(NotificationSeverity.Error, detail: "Invoice update unsuccessful.");
            Console.WriteLine(e);
            throw;
        }
    }

    private async Task OpenChangeCollectionsOwnerDialog()
    {
        // clear collection owner fields so dialog doesnt set using existing values
        Model.CollectionOwner = InvoiceCollectionOwners.Unknown;
        Model.CollectionReason = InvoiceCollectionReason.None;
        Model.CollectionNotes = null;

        var model = new InvoiceNotesViewModel(new[] { Model });

        await DialogService.OpenAsync<ChangeCollectionOwnerDialog>("Change Collection Owner",
                                                                   new()
                                                                   {
                                                                       { nameof(ChangeCollectionOwnerDialog.Model), model },
                                                                       { nameof(ChangeCollectionOwnerDialog.OnSubmit), HandleInvoiceCollectionUpdate },
                                                                       { nameof(ChangeCollectionOwnerDialog.OnCancel), HandleCancel },
                                                                       { nameof(ChangeCollectionOwnerDialog.IsReadOnly), false },
                                                                   });
    }
}
