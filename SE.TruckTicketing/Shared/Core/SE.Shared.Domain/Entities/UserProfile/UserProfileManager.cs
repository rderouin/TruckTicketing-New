using System;
using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;

using SE.Shared.Domain.Infrastructure;

using Trident.Business;
using Trident.Contracts;
using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Validation;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.UserProfile;

public class UserProfileManager : ManagerBase<Guid, UserProfileEntity>, IUserProfileManager
{
    private readonly ISignatureUploadBlobStorage _signatureUploadBlobStorage;

    public UserProfileManager(ILog logger,
                              IProvider<Guid, UserProfileEntity> provider,
                              ISignatureUploadBlobStorage signatureUploadBlobStorage,
                              IValidationManager<UserProfileEntity> validationManager = null,
                              IWorkflowManager<UserProfileEntity> workflowManager = null)
        : base(logger, provider, validationManager, workflowManager)
    {
        _signatureUploadBlobStorage = signatureUploadBlobStorage;
    }

    public Task<string> GetSignatureUploadUri(string userId)
    {
        var uri = _signatureUploadBlobStorage.GetUploadUri(_signatureUploadBlobStorage.DefaultContainerName, userId);
        return Task.FromResult(uri.ToString());
    }

    public async Task<string> GetSignatureDownloadUri(string userId)
    {
        if (await _signatureUploadBlobStorage.Exists(_signatureUploadBlobStorage.DefaultContainerName, userId))
        {
            var uri = _signatureUploadBlobStorage.GetDownloadUri(_signatureUploadBlobStorage.DefaultContainerName, userId, DispositionTypeNames.Inline, null);
            return uri.ToString();
        }

        return null;
    }

    public async Task<Stream> DownloadSignature(string userId)
    {
        if (await _signatureUploadBlobStorage.Exists(_signatureUploadBlobStorage.DefaultContainerName, userId))
        {
            var stream = await _signatureUploadBlobStorage.Download(_signatureUploadBlobStorage.DefaultContainerName, userId);
            return stream;
        }

        return null;
    }
}

public interface IUserProfileManager : IManager<Guid, UserProfileEntity>
{
    Task<string> GetSignatureUploadUri(string userId);

    Task<string> GetSignatureDownloadUri(string userId);

    Task<Stream> DownloadSignature(string userId);
}
