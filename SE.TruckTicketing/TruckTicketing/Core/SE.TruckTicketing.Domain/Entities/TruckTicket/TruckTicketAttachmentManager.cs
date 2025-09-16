using System;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.Infrastructure;
using SE.TruckTicketing.Domain.Entities.SalesLine;

using Trident.Contracts.Api;
using Trident.Data.Contracts;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket;

public class TruckTicketAttachmentManager : ITruckTicketAttachmentManager
{
    private const string TruckTicketAttachmentFolder = "attachments";

    private readonly IProvider<Guid, SalesLineEntity> _salesLineProvider;

    private readonly ISalesLinesPublisher _salesLinesPublisher;

    private readonly IProvider<Guid, TruckTicketEntity> _truckTicketProvider;

    private readonly ITruckTicketUploadBlobStorage _truckTicketUploadBlobStorage;

    public TruckTicketAttachmentManager(IProvider<Guid, TruckTicketEntity> truckTicketProvider,
                                        ITruckTicketUploadBlobStorage truckTicketUploadBlobStorage,
                                        ISalesLinesPublisher salesLinesPublisher,
                                        IProvider<Guid, SalesLineEntity> salesLineProvider)
    {
        _truckTicketUploadBlobStorage = truckTicketUploadBlobStorage;
        _salesLinesPublisher = salesLinesPublisher;
        _salesLineProvider = salesLineProvider;
        _truckTicketProvider = truckTicketProvider;
    }

    public async Task<Uri> GetDownloadUrl(CompositeKey<Guid> truckTicketKey, Guid attachmentId)
    {
        var truckTicket = await _truckTicketProvider.GetById(truckTicketKey); // PK - OK
        var attachment = truckTicket?.Attachments.FirstOrDefault(attachment => attachment.Id == attachmentId);
        if (attachment == null)
        {
            return null;
        }

        return _truckTicketUploadBlobStorage.GetDownloadUri(attachment.Container, attachment.Path, DispositionTypeNames.Inline, attachment.ContentType);
    }

    public async Task<(TruckTicketAttachmentEntity attachment, string uri)> GetUploadUrl(CompositeKey<Guid> truckTicketKey, string filename, string contentType)
    {
        var attachmentId = Guid.NewGuid();
        var path = $"{truckTicketKey.Id}/{attachmentId}";
        var uri = _truckTicketUploadBlobStorage.GetUploadUri(_truckTicketUploadBlobStorage.DefaultContainerName, path);

        var attachmentEntity = new TruckTicketAttachmentEntity
        {
            Id = attachmentId,
            Container = _truckTicketUploadBlobStorage.DefaultContainerName,
            Path = path,
            File = filename,
            ContentType = contentType,
        };

        var truckTicket = await _truckTicketProvider.GetById(truckTicketKey); // PK - OK
        truckTicket.Attachments.Add(attachmentEntity);
        await _truckTicketProvider.Update(truckTicket);

        return (attachmentEntity, uri.ToString());
    }

    public async Task<TruckTicketEntity> MarkFileUploaded(CompositeKey<Guid> truckTicketKey, Guid attachmentId)
    {
        var truckTicket = await _truckTicketProvider.GetById(truckTicketKey); // PK - OK
        var attachment = truckTicket?.Attachments.FirstOrDefault(a => a.Id == attachmentId);
        if (attachment == null)
        {
            return truckTicket;
        }

        attachment.IsUploaded = true;
        await _truckTicketProvider.Update(truckTicket);
        await NotifyIntegrations(truckTicketKey);

        return truckTicket;
    }

    public async Task<TruckTicketEntity> RemoveAttachmentOnTruckTicket(CompositeKey<Guid> truckTicketKey, Guid attachmentId)
    {
        var truckTicket = await _truckTicketProvider.GetById(truckTicketKey); // PK - OK
        var attachment = truckTicket?.Attachments.FirstOrDefault(a => a.Id == attachmentId);
        if (attachment == null)
        {
            return truckTicket;
        }

        truckTicket.Attachments.Remove(attachment);

        await _truckTicketProvider.Update(truckTicket);
        await NotifyIntegrations(truckTicketKey);

        return truckTicket;
    }

    private async Task NotifyIntegrations(CompositeKey<Guid> truckTicketKey)
    {
        // attachments should be promoted to the SL level, push SL updates to integrations
        var salesLines = await _salesLineProvider.Get(sl => sl.TruckTicketId == truckTicketKey.Id); // PK - XP for SL by TT ID
        await _salesLinesPublisher.PublishSalesLines(salesLines);
    }
}
