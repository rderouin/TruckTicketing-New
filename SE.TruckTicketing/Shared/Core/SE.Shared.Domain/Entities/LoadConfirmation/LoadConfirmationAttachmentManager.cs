using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

using SE.Shared.Domain.Infrastructure;

using Trident.Contracts;
using Trident.Contracts.Api;

namespace SE.Shared.Domain.Entities.LoadConfirmation;

public class LoadConfirmationAttachmentManager : ILoadConfirmationAttachmentManager
{
    private readonly IManager<Guid, LoadConfirmationEntity> _loadConfirmationManager;

    private readonly ITruckTicketUploadBlobStorage _truckTicketUploadBlobStorage;

    public LoadConfirmationAttachmentManager(IManager<Guid, LoadConfirmationEntity> loadConfirmationManager, ITruckTicketUploadBlobStorage truckTicketUploadBlobStorage)
    {
        _loadConfirmationManager = loadConfirmationManager;
        _truckTicketUploadBlobStorage = truckTicketUploadBlobStorage;
    }

    public (string uri, LoadConfirmationAttachmentEntity attachment) GetUploadUrl(CompositeKey<Guid> loadConfirmationKey, string filename)
    {
        var path = GetBlobPath(loadConfirmationKey.Id, filename);
        var uri = _truckTicketUploadBlobStorage.GetUploadUri(_truckTicketUploadBlobStorage.DefaultContainerName, path);
        var attachmentEntity = new LoadConfirmationAttachmentEntity
        {
            Id = Guid.NewGuid(),
            AttachedOn = DateTime.UtcNow,
            BlobPath = path,
            BlobContainer = _truckTicketUploadBlobStorage.DefaultContainerName,
            FileName = filename,
        };

        return (uri.ToString(), attachmentEntity);
    }

    public async Task<Uri> GetDownloadUrl(CompositeKey<Guid> loadConfirmationKey, Guid attachmentId)
    {
        var loadConfirmation = await _loadConfirmationManager.GetById(loadConfirmationKey); // PK - OK
        var attachment = loadConfirmation?.Attachments.SingleOrDefault(attachment => attachment.Id == attachmentId);
        if (attachment is null)
        {
            return null;
        }

        return _truckTicketUploadBlobStorage.GetDownloadUri(attachment.BlobContainer, attachment.BlobPath, $"attachment; filename=\"{attachment.FileName}\"", attachment.ContentType);
    }

    public async Task<LoadConfirmationEntity> RemoveAttachmentOnLoadConfirmation(CompositeKey<Guid> loadConfirmationKey, Guid attachmentId)
    {
        var loadConfirmation = await _loadConfirmationManager.GetById(loadConfirmationKey); // PK - OK
        var attachment = loadConfirmation?.Attachments.FirstOrDefault(a => a.Id == attachmentId);

        if (attachment == null)
        {
            return loadConfirmation;
        }

        loadConfirmation.Attachments.Remove(attachment);

        await _loadConfirmationManager.Update(loadConfirmation);

        return loadConfirmation;
    }

    private string GetBlobPath(Guid loadConfirmationId, string filename)
    {
        return $"LoadConfirmations/{loadConfirmationId}/{HttpUtility.UrlEncode(filename)}";
    }
}
