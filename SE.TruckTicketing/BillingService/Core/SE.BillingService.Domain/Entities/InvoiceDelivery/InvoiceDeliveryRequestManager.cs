using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Newtonsoft.Json;

using SE.BillingService.Domain.Integrations.OpenInvoice;
using SE.Enterprise.Contracts.Models.InvoiceDelivery;
using SE.Shared.Domain.Entities.InvoiceDelivery;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Validation;
using Trident.Workflow;

namespace SE.BillingService.Domain.Entities.InvoiceDelivery;

public class InvoiceDeliveryRequestManager : ManagerBase<Guid, InvoiceDeliveryRequestEntity>, IInvoiceDeliveryRequestManager
{
    private readonly IInvoiceDeliveryServiceBus _invoiceDeliveryServiceBus;

    private readonly IOpenInvoiceService _openInvoiceService;

    public InvoiceDeliveryRequestManager(ILog logger,
                                         IOpenInvoiceService openInvoiceService,
                                         IInvoiceDeliveryServiceBus invoiceDeliveryServiceBus,
                                         IProvider<Guid, InvoiceDeliveryRequestEntity> provider,
                                         IValidationManager<InvoiceDeliveryRequestEntity> validationManager = null,
                                         IWorkflowManager<InvoiceDeliveryRequestEntity> workflowManager = null)
        : base(logger, provider, validationManager, workflowManager)
    {
        _openInvoiceService = openInvoiceService;
        _invoiceDeliveryServiceBus = invoiceDeliveryServiceBus;
    }

    public async Task ProcessRemoteStatusUpdates()
    {
        // find all tickets to follow up on
        var requestsEntitiesToFollowup = await Provider.Get(e => e.IsProcessed && e.HasReachedFinalStatus == false);

        // multiple original requests may contain the same ticket ID
        var ticketsLookup = requestsEntitiesToFollowup.Select(e => new
        {
            Entity = e,
            Request = e.GetInvoiceDeliveryRequest(),
        }).ToLookup(e => e.Request.InvoiceId, e => e);

        // unique/distinct fetch
        var tickets = ticketsLookup.Select(l => l.Key).ToHashSet();

        // fetch from remote by 25
        var tasks = tickets.Chunk(25).Select(c => _openInvoiceService.QueryReceiptsAsync(c)).ToArray();

        // process each response
        foreach (var task in tasks)
        {
            try
            {
                // response from a remote
                var response = await task;

                // process all from a batch
                var entitiesToSave = new List<InvoiceDeliveryRequestEntity>();
                foreach (var receipt in response.Receipts ?? Enumerable.Empty<OpenInvoiceReceiptsResult.Receipt>())
                {
                    // not in final = no action... we'll wait a little
                    if (receipt.IsInFinalStatus() == false)
                    {
                        continue;
                    }

                    // 1..M entities to update
                    var pairs = ticketsLookup[receipt.ReceiptNumber].ToList();
                    foreach (var pair in pairs)
                    {
                        // false in DB, true in the remote... update the db
                        pair.Entity.HasReachedFinalStatus = true;
                        entitiesToSave.Add(pair.Entity);

                        // notify sender
                        await NotifySender(pair.Entity, receipt, _invoiceDeliveryServiceBus);
                    }
                }

                // batch update per batch
                if (entitiesToSave.Any())
                {
                    await BulkSave(entitiesToSave);
                }
            }
            catch (Exception e)
            {
                Logger.Error(exception: e, messageTemplate: "Unable to process the request batch.");
            }
        }

        static async Task NotifySender(InvoiceDeliveryRequestEntity entity,
                                       OpenInvoiceReceiptsResult.Receipt receipt,
                                       IInvoiceDeliveryServiceBus invoiceDeliveryServiceBus)
        {
            // create a response for the request
            var request = JsonConvert.DeserializeObject<DeliveryRequest>(entity.OriginalMessage);

            // response to the request
            var response = request.CreateResponse();

            // response model
            response.Payload = new()
            {
                IsSuccessful = true,
                Message = null,

                // provide the status update
                IsStatusUpdate = true,
                RemoteStatus = GetRemoteStatus(receipt.GetEnumStatus()),

                // these properties are irrelevant on status updates
                IsFieldTicketSubmissionSupported = null,
                IsFieldTicketStatusUpdatesSupported = null,
            };

            // send it
            await invoiceDeliveryServiceBus.EnqueueResponse(response);

            static RemoteStatus GetRemoteStatus(OpenInvoiceReceiptStatus receiptStatus)
            {
                return receiptStatus switch
                       {
                           OpenInvoiceReceiptStatus.Approved => RemoteStatus.Approved,
                           OpenInvoiceReceiptStatus.Disputed => RemoteStatus.Denied,
                           OpenInvoiceReceiptStatus.Cancelled => RemoteStatus.Denied,
                           OpenInvoiceReceiptStatus.Submitted => RemoteStatus.Other,
                           OpenInvoiceReceiptStatus.Saved => RemoteStatus.Other,
                           _ => default,
                       };
            }
        }
    }
}
