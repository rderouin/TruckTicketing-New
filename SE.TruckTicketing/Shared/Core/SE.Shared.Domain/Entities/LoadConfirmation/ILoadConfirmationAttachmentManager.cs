using System;
using System.Threading.Tasks;

using Trident.Contracts.Api;

namespace SE.Shared.Domain.Entities.LoadConfirmation;

public interface ILoadConfirmationAttachmentManager
{
    (string uri, LoadConfirmationAttachmentEntity attachment) GetUploadUrl(CompositeKey<Guid> loadConfirmationKey, string filename);

    Task<Uri> GetDownloadUrl(CompositeKey<Guid> loadConfirmationKey, Guid attachmentId);

    Task<LoadConfirmationEntity> RemoveAttachmentOnLoadConfirmation(CompositeKey<Guid> loadConfirmationKey, Guid attachmentId);
}
