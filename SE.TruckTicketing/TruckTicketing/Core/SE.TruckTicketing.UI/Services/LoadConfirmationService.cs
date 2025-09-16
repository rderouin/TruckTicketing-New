using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Api.Models;
using SE.TruckTicketing.Contracts.Models.LoadConfirmations;
using SE.TruckTicketing.Contracts.Models.Statuses;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Contracts.Api;
using Trident.Contracts.Api.Client;
using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Service.Resources.loadConfirmation)]
public class LoadConfirmationService : ServiceBase<LoadConfirmationService, LoadConfirmation, Guid>, ILoadConfirmationService
{
    public LoadConfirmationService(ILogger<LoadConfirmationService> logger,
                                   IHttpClientFactory httpClientFactory)
        : base(logger, httpClientFactory)
    {
    }

    public async Task<string> GetAttachmentDownloadUrl(CompositeKey<Guid> loadConfirmationKey, Guid attachmentId)
    {
        var url = Routes.LoadConfirmation.AttachmentDownload
                        .Replace(Routes.LoadConfirmation.Params.Id, loadConfirmationKey.Id.ToString())
                        .Replace(Routes.LoadConfirmation.Params.Pk, loadConfirmationKey.PartitionKey)
                        .Replace(Routes.LoadConfirmation.Params.AttachmentId, attachmentId.ToString());

        var response = await SendRequest<UriDto>(HttpMethod.Get.Method, url);
        return response.Model.Uri;
    }

    public async Task<Response<LoadConfirmationAttachment>> GetAttachmentUploadUrl(CompositeKey<Guid> loadConfirmationKey, string filename)
    {
        var url = Routes.LoadConfirmation.AttachmentUpload
                        .Replace(Routes.LoadConfirmation.Params.Id, loadConfirmationKey.Id.ToString())
                        .Replace(Routes.LoadConfirmation.Params.Pk, loadConfirmationKey.PartitionKey);

        var response = await SendRequest<LoadConfirmationAttachment>(HttpMethod.Post.Method, url, new LoadConfirmationAttachment
        {
            FileName = filename,
        });

        return response;
    }

    public async Task<string> PreviewLoadConfirmation(CompositeKey<Guid> lcKey)
    {
        var url = Routes.LoadConfirmation.Preview
                        .Replace(Routes.LoadConfirmation.Params.Id, lcKey.Id.ToString())
                        .Replace(Routes.LoadConfirmation.Params.Pk, lcKey.PartitionKey);

        var response = await SendRequest<UriDto>(HttpMethod.Get.Method, url);
        return response.Model.Uri;
    }

    public async Task<string> DownloadLoadConfirmation(CompositeKey<Guid> lcKey)
    {
        var url = Routes.LoadConfirmation.Download
                        .Replace(Routes.LoadConfirmation.Params.Id, lcKey.Id.ToString())
                        .Replace(Routes.LoadConfirmation.Params.Pk, lcKey.PartitionKey);

        var response = await SendRequest<UriDto>(HttpMethod.Get.Method, url);
        return response.Model.Uri;
    }

    public async Task<LoadConfirmationBulkResponse> DoLoadConfirmationBulkAction(LoadConfirmationBulkRequest bulkRequest)
    {
        var response = await SendRequest<LoadConfirmationBulkResponse>(HttpMethod.Post.Method, Routes.LoadConfirmation.BulkAction, bulkRequest);
        return response.Model;
    }

    public async Task<Response<LoadConfirmation>> RemoveAttachment(CompositeKey<Guid> loadConfirmationKey, Guid attachmentId)
    {
        var url = Routes.LoadConfirmation.AttachmentRemove
                        .Replace(Routes.LoadConfirmation.Params.Id, loadConfirmationKey.Id.ToString())
                        .Replace(Routes.LoadConfirmation.Params.Pk, loadConfirmationKey.PartitionKey)
                        .Replace(Routes.LoadConfirmation.Params.AttachmentId, attachmentId.ToString());

        return await SendRequest<LoadConfirmation>(HttpMethod.Patch.Method, url);
    }

    public async Task<Dictionary<CompositeKey<Guid>, EntityStatus>> GetLoadConfirmationStatuses(List<CompositeKey<Guid>> loadConfirmationKeys)
    {
        if (loadConfirmationKeys.Count == 0)
        {
            return new();
        }
        
        var url = Routes.LoadConfirmation.FetchMany;
        var response = await SendRequest<List<EntityStatus>>(HttpMethod.Post.Method, url, loadConfirmationKeys);
        var loadConfirmationStatuses = response.Model ?? new();
        return loadConfirmationStatuses.ToDictionary(s => s.ReferenceEntityKey);
    }
}
