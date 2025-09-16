using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;

using Newtonsoft.Json;

using SE.BillingService.Domain.Entities.InvoiceDelivery;
using SE.BillingService.Domain.InvoiceDelivery;
using SE.BillingService.Domain.InvoiceDelivery.Context;
using SE.Enterprise.Contracts.Constants;
using SE.Enterprise.Contracts.Models.InvoiceDelivery;

using Trident.Logging;

namespace SE.BillingService.Api.Functions.InvoiceDelivery;

public class InvoiceDeliveryFunctions
{
    private readonly IInvoiceDeliveryOrchestrator _invoiceDeliveryOrchestrator;

    private readonly IInvoiceDeliveryRequestManager _invoiceDeliveryRequestManager;

    private readonly ILog _logger;

    public InvoiceDeliveryFunctions(ILog logger,
                                    IInvoiceDeliveryOrchestrator invoiceDeliveryOrchestrator,
                                    IInvoiceDeliveryRequestManager invoiceDeliveryRequestManager)
    {
        _logger = logger;
        _invoiceDeliveryOrchestrator = invoiceDeliveryOrchestrator;
        _invoiceDeliveryRequestManager = invoiceDeliveryRequestManager;
    }

    [Function(nameof(ProcessInvoiceDeliveryRequest))]
    public async Task ProcessInvoiceDeliveryRequest([ServiceBusTrigger(ServiceBusConstants.Topics.InvoiceDelivery,
                                                                       ServiceBusConstants.Subscriptions.InvoiceDelivery,
                                                                       Connection = ServiceBusConstants.PrivateServiceBusNamespace)]
                                                    string message,
                                                    FunctionContext context)
    {
        var correlationId = context.BindingContext.BindingData.GetValueOrDefault(MessageConstants.CorrelationId);

        try
        {
            // save the original request as is
            var invoiceDeliveryRequestEntity = new InvoiceDeliveryRequestEntity { OriginalMessage = message };
            await _invoiceDeliveryRequestManager.Save(invoiceDeliveryRequestEntity);

            // create a new context
            var request = JsonConvert.DeserializeObject<DeliveryRequest>(message)!;
            using var invoiceDeliveryContext = new InvoiceDeliveryContext
            {
                RequestId = invoiceDeliveryRequestEntity.Id,
                Request = request,
            };

            // process request
            await _invoiceDeliveryOrchestrator.ProcessRequest(invoiceDeliveryContext);
        }
        catch (Exception e)
        {
            _logger.Error<InvoiceDeliveryFunctions>(e, $"Unable to process the invoice delivery request. (CorrelationId: '{correlationId}')");
            throw;
        }
    }

    [Function(nameof(PollOpenInvoice))]
    public async Task PollOpenInvoice([TimerTrigger("0 */5 * * * *", RunOnStartup = false)] TimerInfo timerInfo, FunctionContext context)
    {
        try
        {
            await _invoiceDeliveryRequestManager.ProcessRemoteStatusUpdates();
        }
        catch (Exception e)
        {
            _logger.Error<InvoiceDeliveryFunctions>(e, "Unable to poll for remote status updates.");
            throw;
        }
    }
}
