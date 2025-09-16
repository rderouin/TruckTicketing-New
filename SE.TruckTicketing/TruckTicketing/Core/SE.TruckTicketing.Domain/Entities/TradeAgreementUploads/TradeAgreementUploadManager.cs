using System;
using System.Web;

using SE.TridentContrib.Extensions.Security;
using SE.TruckTicketing.Domain.Infrastructure;

using Trident.Business;
using Trident.Contracts;
using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Validation;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.TradeAgreementUploads;

public class TradeAgreementUploadManager : ManagerBase<Guid, TradeAgreementUploadEntity>, ITradeAgreementUploadManager
{
    private readonly ITradeAgreementUploadBlobStorage _tradeAgreementUploadStorage;

    private readonly IUserContextAccessor _userContextAccessor;

    public TradeAgreementUploadManager(ILog logger,
                                       IProvider<Guid, TradeAgreementUploadEntity> provider,
                                       ITradeAgreementUploadBlobStorage tradeAgreementUploadStorage,
                                       IUserContextAccessor userContextAccessor,
                                       IValidationManager<TradeAgreementUploadEntity> validationManager = null,
                                       IWorkflowManager<TradeAgreementUploadEntity> workflowManager = null) : base(logger, provider, validationManager, workflowManager)
    {
        _tradeAgreementUploadStorage = tradeAgreementUploadStorage;
        _userContextAccessor = userContextAccessor;
    }

    public TradeAgreementUploadEntity GetUploadUri()
    {
        var userContext = _userContextAccessor.UserContext;
        var username = userContext.DisplayName;
        var filename = $"TA-Upload-{HttpUtility.UrlEncode(username)}-{DateTime.UtcNow:yyyy-MM-dd-hh-mm-ss}.csv";
        var blobPath = $"TA-Uploads-for-Processing/{filename}";
        var blobContainer = _tradeAgreementUploadStorage.DefaultContainerName;

        return new()
        {
            UploadFileName = filename,
            BlobPath = blobPath,
            Uri = _tradeAgreementUploadStorage.GetUploadUri(blobContainer, blobPath).ToString(),
        };
    }
}

public interface ITradeAgreementUploadManager : IManager<Guid, TradeAgreementUploadEntity>
{
    TradeAgreementUploadEntity GetUploadUri();
}
