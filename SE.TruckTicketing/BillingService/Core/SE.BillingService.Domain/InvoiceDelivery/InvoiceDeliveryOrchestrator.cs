using System;
using System.Linq;
using System.Threading.Tasks;

using SE.BillingService.Domain.Entities.InvoiceDelivery;
using SE.BillingService.Domain.Entities.InvoiceExchange;
using SE.BillingService.Domain.InvoiceDelivery.Context;
using SE.BillingService.Domain.InvoiceDelivery.Encoders;
using SE.BillingService.Domain.InvoiceDelivery.Enrichment;
using SE.BillingService.Domain.InvoiceDelivery.Mapper;
using SE.BillingService.Domain.InvoiceDelivery.Transport;
using SE.BillingService.Domain.InvoiceDelivery.Validation;
using SE.Shared.Domain.Entities.InvoiceDelivery;

using Trident.Extensions;
using Trident.Logging;
using Trident.Mapper;

namespace SE.BillingService.Domain.InvoiceDelivery;

public class InvoiceDeliveryOrchestrator : IInvoiceDeliveryOrchestrator
{
    private readonly IInvoiceDeliveryMessageEncoderSelector _encoderSelector;

    private readonly IInvoiceDeliveryEnricher _invoiceDeliveryEnricher;

    private readonly IInvoiceDeliveryMapper _invoiceDeliveryMapper;

    private readonly IInvoiceDeliveryRequestManager _invoiceDeliveryRequestManager;

    private readonly IInvoiceDeliveryTransportStrategy _invoiceDeliveryTransportStrategy;

    private readonly IInvoiceExchangeManager _invoiceExchangeManager;

    private readonly ILog _logger;

    private readonly IMapperRegistry _mapperRegistry;

    private readonly IInvoiceDeliveryServiceBus _serviceBus;

    private readonly IInvoiceDeliveryRequestValidator _validator;

    public InvoiceDeliveryOrchestrator(ILog logger,
                                       IInvoiceDeliveryRequestValidator validator,
                                       IInvoiceDeliveryRequestManager invoiceDeliveryRequestManager,
                                       IInvoiceDeliveryServiceBus serviceBus,
                                       IInvoiceDeliveryEnricher invoiceDeliveryEnricher,
                                       IInvoiceExchangeManager invoiceExchangeManager,
                                       IInvoiceDeliveryMapper invoiceDeliveryMapper,
                                       IInvoiceDeliveryMessageEncoderSelector encoderSelector,
                                       IInvoiceDeliveryTransportStrategy invoiceDeliveryTransportStrategy,
                                       IMapperRegistry mapperRegistry)
    {
        _logger = logger;
        _validator = validator;
        _invoiceDeliveryRequestManager = invoiceDeliveryRequestManager;
        _serviceBus = serviceBus;
        _invoiceDeliveryEnricher = invoiceDeliveryEnricher;
        _invoiceExchangeManager = invoiceExchangeManager;
        _invoiceDeliveryMapper = invoiceDeliveryMapper;
        _encoderSelector = encoderSelector;
        _invoiceDeliveryTransportStrategy = invoiceDeliveryTransportStrategy;
        _mapperRegistry = mapperRegistry;
    }

    public async Task ProcessRequest(InvoiceDeliveryContext context)
    {
        try
        {
            // track progress of this request
            var invoiceDeliveryRequestEntity = await _invoiceDeliveryRequestManager.GetById(context.RequestId);

            try
            {
                // validate the request
                var errors = await _validator.Validate(context.Request);
                if (errors?.Any() == true)
                {
                    // queue it back with errors
                    await QueueResponse(context, false, string.Join(Environment.NewLine, errors), null);
                    return;
                }

                // get the config
                context.Config = await _invoiceExchangeManager.GetFinalInvoiceExchangeConfig(context.Request.Platform, context.Request.CustomerId);
                if (context.Config == null)
                {
                    var messageContext = new
                    {
                        context.Request.Platform,
                        context.Request.CustomerId,
                    };

                    // queue it back with errors
                    var noConfigMessage = $"No configuration present for the given client. ({messageContext.ToJson()})";
                    await QueueResponse(context, false, string.Join(Environment.NewLine, noConfigMessage), null);
                    return;
                }

                // update the polling flag
                if (context.DeliveryConfig.SupportsStatusPolling == false)
                {
                    // status updates are not supported, hence no further updates; this flag will be save at the end of the task
                    invoiceDeliveryRequestEntity.HasReachedFinalStatus = true;
                }

                // enrich the request context
                await _invoiceDeliveryEnricher.Enrich(context);

                // map data
                await _invoiceDeliveryMapper.Map(context);

                // encode the message
                var encoder = _encoderSelector.Select(context.DeliveryConfig.MessageAdapterType);
                context.EncodedInvoice = await encoder.EncodeMessage(context);

                // send the encoded message
                await _invoiceDeliveryTransportStrategy.Send(context);

                // notify success
                await QueueResponse(context, true, "Successful delivery!", null);
            }
            catch (Exception e)
            {
                _logger.Error(exception: e);
                await QueueResponse(context, false, null, e);
            }

            // mark it as went through this pipeline
            invoiceDeliveryRequestEntity.IsProcessed = true;
            await _invoiceDeliveryRequestManager.Save(invoiceDeliveryRequestEntity);
        }
        catch (Exception e)
        {
            _logger.Error(exception: e);
            await QueueResponse(context, false, null, e);
            throw;
        }
    }

    private async Task QueueResponse(InvoiceDeliveryContext context, bool isSuccessful, string message, Exception x)
    {
        // response
        var response = context.Request.CreateResponse();

        // response data
        response.Payload = new()
        {
            IsSuccessful = isSuccessful,
            Message = x == null ? message : x.Message,
            AdditionalMessage = x is InvoiceDeliveryException ide ? ide.AdditionalMessage : string.Empty,

            // on the initial response, the status update props are irrelevant
            IsStatusUpdate = false,
            RemoteStatus = null,

            // let the sender know if status updates are coming
            IsFieldTicketSubmissionSupported = context.Config?.SupportsFieldTickets,
            IsFieldTicketStatusUpdatesSupported = context.DeliveryConfig?.SupportsStatusPolling,
        };

        // send it
        await _serviceBus.EnqueueResponse(response);
    }
}
