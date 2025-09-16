using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Microsoft.Azure.Functions.Worker;

using Newtonsoft.Json;

using SE.Enterprise.Contracts.Constants;
using SE.Enterprise.Contracts.Models;
using SE.Shared.Domain.Infrastructure;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Azure.Functions;
using Trident.Contracts.Configuration;
using Trident.Logging;

namespace SE.Integrations.Api.Functions.TruckTicketAttachments;

public sealed class TruckTicketAttachmentBlobFunctions : IFunctionController
{
    private static ILog _log;

    private readonly IAppSettings _appSettings;

    private readonly IIntegrationsServiceBus _integrationsServiceBus;

    public TruckTicketAttachmentBlobFunctions(ILog log, IAppSettings appSettings, IIntegrationsServiceBus integrationsServiceBus)
    {
        _log = log;
        _appSettings = appSettings;
        _integrationsServiceBus = integrationsServiceBus;
    }

    [Function("TruckTicketAttachmentBlobFunctions")]
    public void Run([BlobTrigger(BlobStorageConstants.Paths.TTScannedAttachmentsInbound, Connection = BlobStorageConstants.DocumentStorageAccount)] string myBlob,
                    string name,
                    Uri uri,
                    FunctionContext context)
    {
        var messageType = nameof(ServiceBusConstants.EntityMessageTypes.ScannedAttachment);
        var correlationId = Guid.NewGuid();
        var envelopeModel = new EntityEnvelopeModel<TruckTicketAttachment>();
        envelopeModel.EnterpriseId = Guid.NewGuid();
        envelopeModel.Source = "Attachments";
        envelopeModel.CorrelationId = correlationId.ToString();
        envelopeModel.MessageType = messageType;
        envelopeModel.Operation = "Update";
        try
        {
            if (string.IsNullOrEmpty(name))
            {
                _log.Error(messageTemplate: $"File name is blank. ({GetLogMessageContext()})");
                throw new();
            }

            var blobUri = uri.ToString();
            var containerName = _appSettings.GetKeyOrDefault(BlobStorageConstants.Containers.TTScannedAttachmentsInbound.Trim('%'));
            envelopeModel.Payload = new()
            {
                Container = containerName,
                File = name,
                Id = Guid.NewGuid(),
                Path = blobUri[blobUri.IndexOf(containerName)..].Remove(0, containerName.Length + 1),
            };

            //ticketNumber
            var fileFilter = new Regex(@"[A-z0-9]{5}[0-9]+-[A-z]{2}");
            var matchedTicketNumber = fileFilter.Match(name);
            var ticketNumber = matchedTicketNumber.Success ? matchedTicketNumber.Value : name;

            //topicName
            var queueOrTopicName = _appSettings.GetKeyOrDefault(ServiceBusConstants.Queues.ScannedTruckTicketProcessing.Trim('%'));
            var metadata = new Dictionary<string, string>
            {
                [nameof(envelopeModel.Source)] = envelopeModel.Source,
                [nameof(envelopeModel.MessageType)] = envelopeModel.MessageType,
            };

            _integrationsServiceBus.Enqueue(queueOrTopicName, envelopeModel, metadata, ticketNumber);
        }
        catch (Exception e)
        {
            _log.Error(exception: e, messageTemplate: $"Unable to process a message. (msgId: {messageType})");
            throw;
        }

        string GetLogMessageContext()
        {
            return JsonConvert.SerializeObject(envelopeModel);
        }
    }
}
