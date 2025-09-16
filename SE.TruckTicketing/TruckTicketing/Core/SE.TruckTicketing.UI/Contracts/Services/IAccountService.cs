using System;
using System.Threading.Tasks;

using SE.TruckTicketing.Contracts.Models.Accounts;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.UI.Contracts.Services;

public interface IAccountService : IServiceBase<Account, Guid>
{
    Task<Response<InitiateAccountCreditReviewalRequest>> InitiateAccountCreditRenewal(InitiateAccountCreditReviewalRequest request);

    Task<string> GetAttachmentDownloadUri(Guid accountId, AccountAttachment accountAttachment);
}
