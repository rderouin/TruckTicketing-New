using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using SE.TruckTicketing.Contracts.Models.LoadConfirmations;
using SE.TruckTicketing.Contracts.Models.Statuses;

using Trident.Contracts.Api;
using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.UI.Contracts.Services;

public interface ILoadConfirmationService : IServiceProxyBase<LoadConfirmation, Guid>
{
    Task<string> GetAttachmentDownloadUrl(CompositeKey<Guid> loadConfirmationKey, Guid attachmentId);

    Task<Response<LoadConfirmationAttachment>> GetAttachmentUploadUrl(CompositeKey<Guid> loadConfirmationKey, string filename);

    Task<string> PreviewLoadConfirmation(CompositeKey<Guid> lcKey);

    Task<string> DownloadLoadConfirmation(CompositeKey<Guid> lcKey);

    Task<LoadConfirmationBulkResponse> DoLoadConfirmationBulkAction(LoadConfirmationBulkRequest bulkRequest);

    Task<Response<LoadConfirmation>> RemoveAttachment(CompositeKey<Guid> loadConfirmationKey, Guid attachmentId);

    Task<Dictionary<CompositeKey<Guid>, EntityStatus>> GetLoadConfirmationStatuses(List<CompositeKey<Guid>> loadConfirmationKeys);
}
