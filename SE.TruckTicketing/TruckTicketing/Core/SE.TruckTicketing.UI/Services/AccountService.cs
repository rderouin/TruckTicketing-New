using System;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Api.Models;
using SE.TruckTicketing.Contracts.Models.Accounts;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Contracts.Api.Client;
using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Service.Resources.accounts)]
public class AccountService : ServiceBase<AccountService, Account, Guid>, IAccountService
{
    public AccountService(ILogger<AccountService> logger, IHttpClientFactory httpClientFactory) : base(logger, httpClientFactory)
    {
    }

    public async Task<Response<InitiateAccountCreditReviewalRequest>> InitiateAccountCreditRenewal(InitiateAccountCreditReviewalRequest initiateAccountCrditReviewalRequest)
    {
        var response = await SendRequest<InitiateAccountCreditReviewalRequest>(CreateMethod, Routes.Accounts_InitiateCreditReviewal, initiateAccountCrditReviewalRequest);
        return response;
    }

    public async Task<string> GetAttachmentDownloadUri(Guid accountId, AccountAttachment accountAttachment)
    {
        var url = Routes.NewAccount.AttachmentDownload.Replace(Routes.NewAccount.Parameters.Id, accountId.ToString())
                        .Replace(Routes.NewAccount.Parameters.Attachmentid, accountAttachment.Id.ToString());

        var response = await SendRequest<UriDto>(HttpMethod.Get.Method, url);
        return response.Model.Uri;
    }
}

[Service(Service.SETruckTicketingApi, Routes.AccountContactReferenceIndex.Base)]
public class AccountContactReferenceIndexService : ServiceBase<AccountContactReferenceIndexService, AccountContactReferenceIndex, Guid>, IServiceBase<AccountContactReferenceIndex, Guid>
{
    public AccountContactReferenceIndexService(ILogger<AccountContactReferenceIndexService> logger, IHttpClientFactory httpClientFactory) : base(logger, httpClientFactory)
    {
    }
}

[Service(Service.SETruckTicketingApi, Routes.AccountContactIndex.Base)]
public class AccountContactIndexService : ServiceBase<AccountContactIndexService, AccountContactIndex, Guid>, IServiceBase<AccountContactIndex, Guid>
{
    public AccountContactIndexService(ILogger<AccountContactIndexService> logger, IHttpClientFactory httpClientFactory) : base(logger, httpClientFactory)
    {
    }
}
