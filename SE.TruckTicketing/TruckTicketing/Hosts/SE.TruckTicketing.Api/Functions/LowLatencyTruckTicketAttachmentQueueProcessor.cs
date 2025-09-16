using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

using SE.Enterprise.Contracts.Constants;
using SE.Enterprise.Contracts.Models;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.Infrastructure;

using SE.TridentContrib.Extensions.Azure.ServiceBus.ReEnqueue;
using SE.TruckTicketing.Domain.Entities.TruckTicket;

using Trident.Contracts.Configuration;

namespace SE.TruckTicketing.Api.Functions;

public sealed class LowLatencyTruckTicketAttachmentQueueProcessor
{
    private static readonly JsonSerializerOptions EventDeserializationOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly IAppSettings _appSettings;

    private readonly ILeaseObjectBlobStorage _leaseObjectBlobStorage;

    private readonly ILogger<LowLatencyTruckTicketAttachmentQueueProcessor> _logger;

    private readonly IIntegrationsServiceBus _serviceBus;

    private readonly IServiceBusMessageEnqueuer _serviceBusMessageReEnqueuer;

    private readonly ITruckTicketManager _truckTicketManager;

    public LowLatencyTruckTicketAttachmentQueueProcessor(ITruckTicketManager truckTicketManager,
                                                         ILogger<LowLatencyTruckTicketAttachmentQueueProcessor> logger,
                                                         ILeaseObjectBlobStorage leaseObjectBlobStorage,
                                                         IIntegrationsServiceBus serviceBus,
                                                         IAppSettings appSettings,
                                                         IServiceBusMessageEnqueuer serviceBusMessageReEnqueuer)
    {
        _logger = logger;
        _truckTicketManager = truckTicketManager;
        _leaseObjectBlobStorage = leaseObjectBlobStorage;
        _serviceBus = serviceBus;
        _appSettings = appSettings;
        _serviceBusMessageReEnqueuer = serviceBusMessageReEnqueuer;
    }

    // Use ServiceBusReceivedMessage instead of string and FunctionContext after upgrade to Microsoft.Azure.Functions.Worker.Extensions.ServiceBus 5.14.1 or later is feasible
    [Function(nameof(ProcessTruckTicketScanUpload))]
    public async Task ProcessTruckTicketScanUpload([ServiceBusTrigger(ServiceBusConstants.Queues.LowLatencyScannedTruckTicketProcessing,
                                                                      Connection = ServiceBusConstants.PrivateServiceBusNamespace)]
                                                   string message,
                                                   FunctionContext functionContext)
    {
        var blobCreatedEvent = DeserializeMessageAsBlobCreatedEvent(message);
        if (blobCreatedEvent is null)
        {
            throw new AggregateException("Message Parsing Failure: Unable to parse message as a valid JSON encoded BlobCreated event.");
        }

        var scannedTicketMetadata = ParseScanMetadataFromBlobUrl(blobCreatedEvent.Data.Url);
        if (scannedTicketMetadata is null)
        {
            throw new AggregateException("Ticket Metadata Parsing Failure: Unable to parse valid ticket metadata from the BlobCreated event.");
        }

        var truckTicket = (await _truckTicketManager.Get(ticket => ticket.TicketNumber == scannedTicketMetadata.TicketNumber))?.FirstOrDefault();
        if (truckTicket is null)
        {
            // If ticket is not yet created, reenqueue the message
            // The reenqueue should happen with delay (as per ReEnqueueOptions)
            // Past MaxReEnqueueCount or MaxDelay the message should be deadlettered

            void ThrowTicketNotFoundException()
            {
                throw new AggregateException($"Missing Destination Truck Ticket. Could not find destination truck ticket '{scannedTicketMetadata.TicketNumber}' for this scan.");
            }

            var reEnqueued = await TryReEnqueue(message, functionContext, ThrowTicketNotFoundException);

            if (!reEnqueued)
            {
                _logger.LogInformation($"Missing Destination Truck Ticket '{scannedTicketMetadata.TicketNumber}'. Message not reenqueued due to either max duration or max reenqueue count.");

                ThrowTicketNotFoundException();
            }
            else
            {
                _logger.LogInformation($"Missing Destination Truck Ticket '{scannedTicketMetadata.TicketNumber}'. Message reenqueued for later processing.");

                return;
            }
        }

        // Use distributed locking to ensure we only process one attachment at a time per ticket to avoid db update race conditions.
        var distributedLockKey = nameof(ProcessTruckTicketScanUpload) + truckTicket.TicketNumber;
        await _leaseObjectBlobStorage.AcquireLeaseAndExecute(() => AttachScanToTruckTicket(truckTicket, scannedTicketMetadata, blobCreatedEvent), distributedLockKey);
    }

    private BlobCreatedEvent DeserializeMessageAsBlobCreatedEvent(string message)
    {
        try
        {
            var blobCreatedEvent = JsonSerializer.Deserialize<BlobCreatedEvent>(message, EventDeserializationOptions);
            _logger.LogInformation("Successfully parsed {BlobCreatedEvent}", blobCreatedEvent);
            return blobCreatedEvent;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to parse message body as JSON.");
            return null;
        }
    }

    private TicketScanMetadata ParseScanMetadataFromBlobUrl(string blobUrl)
    {
        try
        {
            var metadata = TicketScanMetadata.FromBlobUrl(blobUrl);
            _logger.LogInformation("Successfully parsed {ScannedTicketMetadata}", metadata);
            return metadata;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to extract valid truck ticket metadata from {blobUrl}", blobUrl);
            return null;
        }
    }

    private Task<TruckTicketEntity> AttachScanToTruckTicket(TruckTicketEntity truckTicket, TicketScanMetadata ticketScanMetadata, BlobCreatedEvent blobCreatedEvent)
    {
        var existingAttachment = truckTicket.Attachments.Find(attachment => attachment.File.Equals(ticketScanMetadata.File, StringComparison.OrdinalIgnoreCase));
        if (existingAttachment is null)
        {
            truckTicket.Attachments.Add(new()
            {
                Id = Guid.NewGuid(),
                IsUploaded = true,
                File = ticketScanMetadata.File,
                Path = ticketScanMetadata.Path,
                Container = ticketScanMetadata.Container,
                ContentType = blobCreatedEvent.Data.ContentType,
                AttachmentType = ticketScanMetadata.GetComputedAttachmentType(truckTicket.CountryCode),
            });
        }
        else
        {
            existingAttachment.Path = ticketScanMetadata.Path;
            existingAttachment.AttachmentType = ticketScanMetadata.GetComputedAttachmentType(truckTicket.CountryCode);
        }

        return _truckTicketManager.Save(truckTicket);
    }

    private async Task<bool> TryReEnqueue(string message, FunctionContext functionContext, Action throwTicketNotFoundException)
    {
        var topicName = _appSettings.GetKeyOrDefault(ServiceBusConstants.Queues.LowLatencyScannedTruckTicketProcessing.Trim('%'), "tt-blob-inbound-attachments");
        var bindingData = functionContext?.BindingContext?.BindingData;

        if (bindingData?.ContainsKey("ApplicationProperties") != true)
        {
            _logger.LogWarning("Service bus message is missing ApplicationProperties binding data");

            throwTicketNotFoundException();
        }

        var applicationPropertiesJson = bindingData.GetValueOrDefault("ApplicationProperties", "{}").ToString();

        _logger.LogInformation($"TryReEnqueue: applicationPropertiesJson: {applicationPropertiesJson}");

        var options = _appSettings.GetSection<ProcessTruckTicketScanUploadOptions>(nameof(ProcessTruckTicketScanUpload)) ?? new ProcessTruckTicketScanUploadOptions
        {
            ReEnqueueOptions = new()
            {
                MaxReEnqueueCount = 5,
                BackoffType = BackoffTypeEnum.Exponential,
                Delay = TimeSpan.FromMinutes(45),
            },
        };

        var reEnqueued = await _serviceBusMessageReEnqueuer.ReEnqueue(_serviceBus,
                                                               topicName,
                                                               message,
                                                               applicationPropertiesJson,
                                                               options.ReEnqueueOptions);


        return reEnqueued;
    }
}

public class ProcessTruckTicketScanUploadOptions
{
    public ProcessTruckTicketScanUploadOptions()
    {
        ReEnqueueOptions = new();
    }

    public ReEnqueueOptions ReEnqueueOptions { get; set; }
}
