using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Constants;
using SE.Enterprise.Contracts.Models;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.Processors;

using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;

namespace SE.TruckTicketing.Domain.Entities.SalesLine;

[EntityProcessorFor(ServiceBusConstants.EntityMessageTypes.SalesLineAttachment)]
public class SalesLineAttachmentProcessor : BaseEntityProcessor<AttachmentModel>
{
    private readonly ILog _logger;

    private readonly IManager<Guid, SalesLineEntity> _salesLinesManager;

    private readonly IManager<Guid, TruckTicketEntity> _truckTicketManager;

    private readonly IMapperRegistry _mapperRegistry;

    public SalesLineAttachmentProcessor(ILog logger, 
                                        IManager<Guid, SalesLineEntity> salesLinesManager,
                                        IManager<Guid, TruckTicketEntity> truckTicketManager,                                         
                                        IMapperRegistry mapperRegistry)
    {
        _logger = logger;
        _salesLinesManager = salesLinesManager;
        _truckTicketManager = truckTicketManager;        
        _mapperRegistry = mapperRegistry;
    }

    public override async Task Process(EntityEnvelopeModel<AttachmentModel> model)
    {
        var payload = model.Payload;

        try
        {
            var salesLine = await GetSalesLine(payload);

            if (salesLine == null)
            {
                _logger.Warning(messageTemplate: $"SalesLineId not found: {payload.SalesLineId})");
                return;
            }

            var truckTicket = await GetTruckTicket(salesLine.TruckTicketId);

            if (truckTicket == null)
            {
                _logger.Warning(messageTemplate: $"TruckTicketId not found: {salesLine.TruckTicketId}");
                return;
            }

            var blobs = model.Blobs;
            await UpdateTruckTicket(truckTicket, payload, blobs);
        }
        catch(Exception exception)
        {
            _logger.Error(exception: exception);            
            throw;
        }        
    }

    private async Task<SalesLineEntity> GetSalesLine(AttachmentModel payload)
    {
        var salesLines = await _salesLinesManager.Get(salesLine => salesLine.Id == payload.SalesLineId); // PK - TODO: INT
        return salesLines.FirstOrDefault();      
    }

    private async Task<TruckTicketEntity> GetTruckTicket(Guid truckTicketId)
    {
        return await _truckTicketManager.GetById(truckTicketId); // PK - TODO: ENTITY or INDEX
    }

    private async Task UpdateTruckTicket(TruckTicketEntity truckTicket, AttachmentModel payload, List<BlobAttachment> attachments)
    {
        foreach (var attachment in attachments)
        {
            // lookup attachment by filename
            var truckTicketAttachment = truckTicket.Attachments.FirstOrDefault(x => x.File.Trim().Equals(attachment.Filename.Trim(), StringComparison.OrdinalIgnoreCase));

            if (truckTicketAttachment != null)
            {
                // update existing attachment
                truckTicketAttachment.Path = attachment.BlobPath;
                truckTicketAttachment.Container = attachment.ContainerName;
                truckTicketAttachment.ContentType = attachment.ContentType;
                truckTicketAttachment.File = attachment.Filename;
            }
            else
            {
                // attachment does not exist, so adding to list
                truckTicket.Attachments.Add(new()
                {
                    Id = payload.Id,
                    Path = attachment.BlobPath,
                    Container = attachment.ContainerName,
                    ContentType = attachment.ContentType,
                    File = attachment.Filename,
                    IsUploaded = true,
                });
            }
        }

        await _truckTicketManager.Update(truckTicket);
    }
}
