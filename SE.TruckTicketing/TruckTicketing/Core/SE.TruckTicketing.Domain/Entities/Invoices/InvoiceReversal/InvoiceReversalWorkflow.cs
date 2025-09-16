using System;
using System.Linq;
using System.Threading.Tasks;

using Humanizer;

using SE.Shared.Common.Extensions;
using SE.Shared.Domain;
using SE.Shared.Domain.Entities.Invoices;
using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.Shared.Domain.Entities.Note;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.Infrastructure;
using SE.TridentContrib.Extensions.Security;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Invoices;

using Trident.Contracts;
using Trident.Extensions;

namespace SE.TruckTicketing.Domain.Entities.Invoices.InvoiceReversal;

public class InvoiceReversalWorkflow : IInvoiceReversalWorkflow
{
    private readonly IManager<Guid, InvoiceEntity> _invoiceManager;

    private readonly ILeaseObjectBlobStorage _leaseObjectBlobStorage;

    private readonly IManager<Guid, LoadConfirmationEntity> _loadConfirmationManager;

    private readonly IManager<Guid, NoteEntity> _noteManager;

    private readonly IManager<Guid, SalesLineEntity> _salesLineManager;

    private readonly ISalesLinesPublisher _salesLinesPublisher;

    private readonly IManager<Guid, TruckTicketEntity> _truckTicketManager;

    private readonly IUserContextAccessor _userContextAccessor;

    public InvoiceReversalWorkflow(IManager<Guid, InvoiceEntity> invoiceManager,
                                   IManager<Guid, LoadConfirmationEntity> loadConfirmationManager,
                                   IManager<Guid, SalesLineEntity> salesLineManager,
                                   IManager<Guid, TruckTicketEntity> truckTicketManager,
                                   IManager<Guid, NoteEntity> noteManager,
                                   ISalesLinesPublisher salesLinesPublisher,
                                   IUserContextAccessor userContextAccessor,
                                   ILeaseObjectBlobStorage leaseObjectBlobStorage)
    {
        _invoiceManager = invoiceManager;
        _loadConfirmationManager = loadConfirmationManager;
        _salesLineManager = salesLineManager;
        _truckTicketManager = truckTicketManager;
        _noteManager = noteManager;
        _salesLinesPublisher = salesLinesPublisher;
        _userContextAccessor = userContextAccessor;
        _leaseObjectBlobStorage = leaseObjectBlobStorage;
    }

    public async Task<ReverseInvoiceInfo> ReverseInvoice(ReverseInvoiceRequest reverseInvoiceRequest)
    {
        async Task<ReverseInvoiceInfo> ReverseInvoiceImpl()
        {
            /////////////////////////////////////////////////// LEVEL 1 - INVOICES ////////////////////////////////////////////////////////////

            // fetch the existing invoice and create clones
            var originalInvoice = await _invoiceManager.GetById(reverseInvoiceRequest.InvoiceKey); // PK - OK
            var invoice = new Map<InvoiceEntity>
            {
                Original = originalInvoice,
                Reversal = CloneInvoice(originalInvoice),
                Proforma = CloneInvoice(originalInvoice),
            };

            // set the invoice reversal flags
            invoice.Original.IsReversed = true; // the original is reversed
            invoice.Reversal.IsReversal = true; // the reversal is for the original
            invoice.Reversal.TransactionComplete = false;
            invoice.Reversal.RequiresPdfRegeneration = true;

            // link invoices
            invoice.Original.ReversalInvoiceId = invoice.Reversal.Id; // the original points to a reversal invoice
            invoice.Reversal.ReversedInvoiceId = invoice.Original.Id; // the reversal keep the reference to the original
            invoice.Reversal.OriginalGlInvoiceNumber = invoice.Original.GlInvoiceNumber;
            invoice.Reversal.OriginalProformaInvoiceNumber = invoice.Original.ProformaInvoiceNumber;
            invoice.Proforma.ReversedInvoiceId = invoice.Original.Id; // the proforma is also created from the original
            invoice.Proforma.OriginalGlInvoiceNumber = invoice.Original.GlInvoiceNumber;
            invoice.Proforma.OriginalProformaInvoiceNumber = invoice.Original.ProformaInvoiceNumber;

            // inverse the reversal invoice
            InverseInvoice(invoice.Reversal);

            // update the status of the new invoice
            invoice.Proforma.Status = InvoiceStatus.UnPosted;
            invoice.Proforma.TransactionComplete = false;
            invoice.Proforma.RequiresPdfRegeneration = true;
            
            // keep the justification why it is reversed
            invoice.Original.InvoiceReversalReason = reverseInvoiceRequest.InvoiceReversalReason;
            invoice.Original.InvoiceReversalDescription = reverseInvoiceRequest.InvoiceReversalDescription;

            // proforma specifics - clone original attachments
            if (reverseInvoiceRequest.IncludeOriginalDocuments)
            {
                invoice.Proforma.Attachments = invoice.Original.Attachments.Select(a => a.Clone()).ToList();
            }

            /////////////////////////////////////////////////// LEVEL 2 - LOAD CONFIRMATIONS //////////////////////////////////////////////////

            // fetch the load confirmations and clone them
            var originalLoadConfirmations = (await _loadConfirmationManager.Get(lc => lc.InvoiceId == originalInvoice.Id)).ToList(); // PK - XP for LC by Invoice ID

            var loadConfirmations = originalLoadConfirmations.Select(lc => new Map<LoadConfirmationEntity>
            {
                Original = lc, //                           // the original LC for a reference
                Reversal = null, //                         // there is no LC for the reversal invoice
                Proforma = CloneLoadConfirmation(lc), //    // proforma invoice will need an LC
            }).ToList();

            // update the load confirmations
            foreach (var lc in loadConfirmations)
            {
                // the original load confirmations are reversed
                lc.Original.IsReversed = true;

                // the new LC is linked to the original LC
                lc.Proforma.ReversedLoadConfirmationId = lc.Original.Id;

                // the new LC points to the correct invoice
                lc.Proforma.InvoiceId = invoice.Proforma.Id;

                // the new LC is open and ready for editing
                lc.Proforma.Status = LoadConfirmationStatus.Open;

                lc.Proforma.InvoiceStatus = InvoiceStatus.UnPosted;

                lc.Proforma.GlInvoiceNumber = null;

                // proforma specifics - clone original attachments
                if (reverseInvoiceRequest.IncludeOriginalDocuments)
                {
                    lc.Proforma.Attachments = lc.Original.Attachments.Select(a => a.Clone()).ToList();
                }
            }

            /////////////////////////////////////////////////// LEVEL 3 - SALES LINES /////////////////////////////////////////////////////////

            // sales lines of the existing invoice to be reversed
            var originalSalesLines = (await _salesLineManager.Get(l => l.InvoiceId == originalInvoice.Id)).ToList(); // PK - XP for SL by Invoice ID
            var salesLines = originalSalesLines.Select(sl => new Map<SalesLineEntity>
            {
                Original = sl,
                Reversal = CloneSalesLine(sl),
                Proforma = CloneSalesLine(sl),
            }).ToList();

            // update the sales lines
            var loadConfirmationLookup = loadConfirmations.ToDictionary(lc => lc.Original.Id);
            foreach (var sl in salesLines)
            {
                // setting the reversal flags
                sl.Original.IsReversed = true;
                sl.Reversal.IsReversal = true;

                // linking the sales lines
                sl.Original.ReversalSalesLineId = sl.Reversal.Id;
                sl.Reversal.ReversedSalesLineId = sl.Original.Id;
                sl.Proforma.ReversedSalesLineId = sl.Original.Id;

                // point to correct invoices
                sl.Reversal.InvoiceId = invoice.Reversal.Id;
                sl.Reversal.ProformaInvoiceNumber = invoice.Reversal.ProformaInvoiceNumber;
                sl.Proforma.InvoiceId = invoice.Proforma.Id;

                // point to correct load confirmations, applicable only for the new/proforma LC
                if (sl.Original.LoadConfirmationId.HasValue && loadConfirmationLookup.TryGetValue(sl.Original.LoadConfirmationId.Value, out var lc))
                {
                    sl.Proforma.LoadConfirmationId = lc.Proforma.Id;
                }

                // proforma specifics - clone original attachments
                if (reverseInvoiceRequest.IncludeOriginalDocuments)
                {
                    sl.Proforma.Attachments = sl.Original.Attachments.Select(a => a.Clone()).ToList();
                }

                // reverse the sales line
                InverseSalesLine(sl.Reversal);

                // the sales line is ready
                sl.Proforma.Status = SalesLineStatus.Approved;
            }

            // update the related tickets - change ticket status only when a proforma invoice is created
            var truckTicketIds = originalSalesLines.Select(s => s.TruckTicketId).ToHashSet();
            var truckTickets = (await _truckTicketManager.GetByIds(truckTicketIds)).ToList(); // PK - TODO: ENTITY or INDEX
            foreach (var truckTicket in truckTickets)
            {
                truckTicket.Status = reverseInvoiceRequest.CreateProForma ? TruckTicketStatus.Approved : TruckTicketStatus.Open;
            }

            /////////////////////////////////////////////////// LEVEL 4 - PERSISTENCE /////////////////////////////////////////////////////////

            // save the reversal entities (invoice -> load confirmation -> sales lines), LCs for reversal are n/a
            await _invoiceManager.Save(invoice.Reversal, true);
            salesLines.ForEach(salesLine => salesLine.Reversal.ProformaInvoiceNumber = invoice.Reversal.ProformaInvoiceNumber);
            foreach (var sl in salesLines.Select(sl => sl.Reversal))
            {
                await _salesLineManager.Save(sl, true);
            }

            // update the original entities (invoice -> load confirmation -> sales lines)
            await _invoiceManager.Save(invoice.Original, true);
            foreach (var lc in loadConfirmations.Select(lc => lc.Original))
            {
                await _loadConfirmationManager.Save(lc, true);
            }

            foreach (var sl in salesLines.Select(sl => sl.Original))
            {
                await _salesLineManager.Save(sl, true);
            }

            // create new entities (invoice -> load confirmation -> sales lines)
            if (reverseInvoiceRequest.CreateProForma)
            {
                await _invoiceManager.Save(invoice.Proforma, true);

                loadConfirmations.ForEach(loadConfirmation => loadConfirmation.Proforma.InvoiceNumber = invoice.Proforma.ProformaInvoiceNumber);

                foreach (var lc in loadConfirmations.Select(lc => lc.Proforma))
                {
                    await _loadConfirmationManager.Save(lc, true);
                }

                salesLines.ForEach(salesLine =>
                                   {
                                       salesLine.Proforma.ProformaInvoiceNumber = invoice.Proforma.ProformaInvoiceNumber;

                                       // point to correct load confirmation number after assignment
                                       if (salesLine.Original.LoadConfirmationId.HasValue && loadConfirmationLookup.TryGetValue(salesLine.Original.LoadConfirmationId.Value, out var lc))
                                       {
                                           salesLine.Proforma.LoadConfirmationNumber = lc.Proforma.Number;
                                       }
                                   });

                foreach (var sl in salesLines.Select(sl => sl.Proforma))
                {
                    await _salesLineManager.Save(sl, true);
                }
            }

            // push updates to integrations
            await _salesLinesPublisher.PublishSalesLines(salesLines.Select(sl => sl.Reversal));
            await _salesLinesPublisher.PublishSalesLines(salesLines.Select(sl => sl.Original));
            if (reverseInvoiceRequest.CreateProForma)
            {
                await _salesLinesPublisher.PublishSalesLines(salesLines.Select(sl => sl.Proforma));
            }

            // update the related tickets should there be any updates to them (e.g. a proforma invoice is created)
            if (truckTickets.Any())
            {
                foreach (var tt in truckTickets)
                {
                    await _truckTicketManager.Save(tt, true);
                }
            }

            /////////////////////////////////////////////////// LEVEL 5 - ADD NOTES ////////////////////////////////////////////////////

            // add notes to all invoices
            var user = _userContextAccessor.UserContext.DisplayName;
            var commonNoteText = $"Reversed invoice {invoice.Original.ProformaInvoiceNumber}, Credit Invoice {invoice.Reversal.ProformaInvoiceNumber}";
            if (reverseInvoiceRequest.CreateProForma)
            {
                commonNoteText += $", New Invoice (S) {invoice.Proforma.ProformaInvoiceNumber}";
            }

            commonNoteText += $"{Environment.NewLine}Reason: {reverseInvoiceRequest.InvoiceReversalReason.Humanize()}";
            if (reverseInvoiceRequest.InvoiceReversalDescription.HasText())
            {
                commonNoteText += $"{Environment.NewLine}Comment: {reverseInvoiceRequest.InvoiceReversalDescription}";
            }

            // notes for each invoice entity is different
            var notesForOriginal = $"This invoice has been reversed by {user}, the credit invoice No. {invoice.Reversal.ProformaInvoiceNumber}";
            var notesForReversal = $"This invoice is the credit invoice for the reversed invoice by {user}, invoice No. {invoice.Original.ProformaInvoiceNumber}";
            var notesForProforma =
                $"This invoice was autogenerated by a reversal of the invoice No. {invoice.Original.ProformaInvoiceNumber} by {user}. Credit invoice No. {invoice.Reversal.ProformaInvoiceNumber}";

            // save all notes
            await AddNote(invoice.Original, $"{commonNoteText}{Environment.NewLine}{notesForOriginal}");
            await AddNote(invoice.Reversal, $"{commonNoteText}{Environment.NewLine}{notesForReversal}");
            if (reverseInvoiceRequest.CreateProForma)
            {
                await AddNote(invoice.Proforma, $"{commonNoteText}{Environment.NewLine}{notesForProforma}");
            }

            // COMMIT TO DB
            await _invoiceManager.SaveDeferred();

            // done
            return new()
            {
                OriginalInvoice = invoice.Original,
                ReversalInvoice = invoice.Reversal,
                ProformaInvoice = invoice.Proforma,
            };
        }

        var (success, value) = await _leaseObjectBlobStorage.TryAcquireLeaseAndExecute(ReverseInvoiceImpl, $"reverse-invoice-{reverseInvoiceRequest.InvoiceKey.Id}");
        if (success)
        {
            return value;
        }

        value.ErrorMessage = "The invoice is already being reversed. Please wait.";

        return value;
    }

    private async Task AddNote(InvoiceEntity invoice, string message)
    {
        await _noteManager.Insert(new()
        {
            Id = Guid.NewGuid(),
            Comment = message,
            ThreadId = $"{Databases.Discriminators.Invoice}|{invoice.Id}",
            NotEditable = true,
        }, true);
    }

    private static SalesLineEntity CloneSalesLine(SalesLineEntity original)
    {
        var salesLine = original.Clone();
        salesLine.Id = Guid.NewGuid();
        salesLine.SalesLineNumber = null;
        salesLine.InvoiceId = null;
        salesLine.ProformaInvoiceNumber = null;
        salesLine.LoadConfirmationId = null;
        salesLine.LoadConfirmationNumber = null;
        salesLine.Attachments = null;
        return salesLine;
    }

    private static LoadConfirmationEntity CloneLoadConfirmation(LoadConfirmationEntity original)
    {
        var loadConfirmation = original.Clone();
        loadConfirmation.Id = Guid.NewGuid();
        loadConfirmation.Number = original.Frequency == LoadConfirmationFrequency.TicketByTicket.ToString() ? original.Number : null;
        loadConfirmation.InvoiceId = Guid.Empty;
        loadConfirmation.InvoiceNumber = null;
        loadConfirmation.Attachments = null;

        // 'PrimitiveCollection' postfix
        loadConfirmation.Generators = new();
        original.Generators?.ForEach(e => loadConfirmation.Generators.Add(e.Clone()));
        if (loadConfirmation.Generators?.Any() == false)
        {
            loadConfirmation.Generators = null;
        }

        // owned entities fix
        loadConfirmation.Generators?.ForEach(e => e.Id = Guid.NewGuid());
        loadConfirmation.Signatories?.ForEach(e => e.Id = Guid.NewGuid());

        return loadConfirmation;
    }

    private static InvoiceEntity CloneInvoice(InvoiceEntity original)
    {
        var invoice = original.Clone();
        invoice.Id = Guid.NewGuid();
        invoice.GlInvoiceNumber = null;
        invoice.ProformaInvoiceNumber = null;
        invoice.Attachments = null;

        // 'PrimitiveCollection' postfix
        invoice.Generators = new();
        original.Generators?.List.ForEach(e => invoice.Generators.List.Add(e));

        // 'PrimitiveCollection' postfix
        invoice.Signatories = new();
        original.Signatories?.List.ForEach(e => invoice.Signatories.List.Add(e));

        return invoice;
    }

    private static void InverseSalesLine(SalesLineEntity salesLine)
    {
        salesLine.Quantity *= -1.0;
        salesLine.TotalValue *= -1.0;
    }

    private static void InverseInvoice(InvoiceEntity invoice)
    {
        invoice.InvoiceAmount *= -1.0;
    }

    private class Map<T>
    {
        public T Original { get; init; }

        public T Reversal { get; init; }

        public T Proforma { get; init; }
    }
}
