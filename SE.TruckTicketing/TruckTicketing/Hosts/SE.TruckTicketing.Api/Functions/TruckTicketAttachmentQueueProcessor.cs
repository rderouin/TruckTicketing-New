using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;

using Newtonsoft.Json;

using SE.Enterprise.Contracts.Constants;
using SE.Enterprise.Contracts.Models;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Domain.Entities.TruckTicket;

using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Mapper;

namespace SE.TruckTicketing.Api.Functions;

public sealed class TruckTicketAttachmentQueueProcessor
{
    private static ILog _log;

    private readonly IProvider<Guid, FacilityEntity> _facilityProvider;

    private readonly IMapperRegistry _mapper;

    private readonly ITruckTicketManager _truckTicketManager;

    public TruckTicketAttachmentQueueProcessor(ILog log,
                                               IMapperRegistry mapper,
                                               ITruckTicketManager truckTicketManager,
                                               IProvider<Guid, FacilityEntity> facilityProvider)
    {
        _log = log;
        _mapper = mapper;
        _truckTicketManager = truckTicketManager;
        _facilityProvider = facilityProvider;
    }

    [Function("TruckTicketAttachmentQueueProcessor")]
    public async Task Run([ServiceBusTrigger(ServiceBusConstants.Queues.ScannedTruckTicketProcessing,
                                             Connection = ServiceBusConstants.PrivateServiceBusNamespace, IsSessionsEnabled = true)]
                          string message,
                          FunctionContext context)
    {
        var messageId = context.BindingContext.BindingData.GetValueOrDefault(MessageConstants.EntityUpdate.MessageId);
        var messageType = context.BindingContext.BindingData.GetValueOrDefault(MessageConstants.EntityUpdate.MessageType);
        var correlationId = context.BindingContext.BindingData.GetValueOrDefault(MessageConstants.CorrelationId);
        try
        {
            var messageTypeValue = messageType as string;

            // only process ScannedAttachment messages
            if (messageTypeValue == nameof(ServiceBusConstants.EntityMessageTypes.ScannedAttachment))
            {
                if (string.IsNullOrWhiteSpace(messageTypeValue))
                {
                    _log.Warning(messageTemplate: $"Message Type is blank. ({GetLogMessageContext()})");
                    return;
                }

                var scannedAttachment = JsonConvert.DeserializeObject<EntityEnvelopeModel<TruckTicketAttachment>>(message);
                var fileName = scannedAttachment?.Payload.File;

                if (fileName != null)
                {
                    // regex retrieves all, but final hyphen and ending INT or EXT characters at the end
                    var fileNameRegex = new Regex(@"[A-z0-9]{5}[0-9]+-[A-z]{2}");
                    var fileNameMatch = fileNameRegex.Match(fileName);
                    string ticketNumber = null;

                    if (fileNameMatch.Success)
                    {
                        ticketNumber = fileNameMatch.Value;
                    }
                    else
                    {
                        _log.Warning(messageTemplate: $"Truck Ticket number not found in file name. (FileName: {fileName})");
                    }

                    var lastHyphenPosition = fileName.LastIndexOf('-');
                    var attachmentType = fileName.Substring(lastHyphenPosition + 1, 3);

                    if (!attachmentType.Equals("int", StringComparison.OrdinalIgnoreCase) && !attachmentType.Equals("ext", StringComparison.OrdinalIgnoreCase))
                    {
                        _log.Warning(messageTemplate: $"Valid Truck Ticket attachment type not found in file name. (FileName: {fileName})");
                    }
                    else
                    {
                        attachmentType = attachmentType.Equals("int", StringComparison.OrdinalIgnoreCase) ? "internal" : "external";
                    }

                    var facilityBySiteId = _facilityProvider.Search(new()
                    {
                        Filters = new() { { nameof(FacilityEntity.SiteId), ticketNumber?.Substring(0, 5) } },
                    }).Result.Results.FirstOrDefault();

                    if (facilityBySiteId.Id == default)
                    {
                        throw new($"Cannot derive Facility ID from TicketNumber: {ticketNumber} of original file name : {scannedAttachment.Payload.File} ");
                    }

                    var truckTicketEntity = _truckTicketManager.Search(new()
                    {
                        Filters = new() { { nameof(TruckTicket.TicketNumber), ticketNumber } },
                    }).Result.Results.FirstOrDefault();

                    var attachment = _mapper.Map<TruckTicketAttachmentEntity>(scannedAttachment.Payload);

                    var validAttachmentType = Enum.TryParse(attachmentType, true, out AttachmentType attachmentTypeValue);
                    if (validAttachmentType)
                    {
                        attachment.AttachmentType = attachmentTypeValue;
                    }

                    if (truckTicketEntity is not null)
                    {
                        if (truckTicketEntity.TruckTicketType is TruckTicketType.WT)
                        {
                            _log.Information(messageTemplate: message);
                            var usAttachmentType = validAttachmentType ? attachmentTypeValue : AttachmentType.Internal;
                            attachment.AttachmentType = truckTicketEntity.CountryCode is CountryCode.US ? usAttachmentType : AttachmentType.External;
                        }

                        attachment.IsUploaded = true;
                        var existingAttachment = truckTicketEntity.Attachments.FirstOrDefault(a => a.File.Equals(attachment.File, StringComparison.OrdinalIgnoreCase));
                        if (existingAttachment is null)
                        {
                            truckTicketEntity.Attachments.Add(attachment);
                        }
                        else
                        {
                            existingAttachment.Path = attachment.Path;
                        }

                        await _truckTicketManager.Save(truckTicketEntity)!;
                    }
                }
                else
                {
                    _log.Information(messageTemplate: $"File is empty in payload. ({GetLogMessageContext()})");
                }
            }
            else
            {
                _log.Information(messageTemplate: $"Message Type is not ScannedAttachment. ({GetLogMessageContext()})");
            }
        }
        catch (Exception e)
        {
            _log.Error(exception: e, messageTemplate: $"Unable to process a ScannedAttachment message. (msgId: {GetLogMessageContext()} {e.StackTrace})");
            throw;
        }

        string GetLogMessageContext()
        {
            return JsonConvert.SerializeObject(new
            {
                messageId,
                messageType,
                correlationId,
            });
        }
    }
}
