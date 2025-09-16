using System;
using System.Threading.Tasks;

using SE.TruckTicketing.Contracts.Models.Accounts;

using Trident.Contracts;

namespace SE.Shared.Domain.Entities.NewAccount;

public interface INewAccountManager : IManager
{
    Task CreateNewAccount(NewAccountModel newAccount);

    Task InitiateNewAccountCreditReviewal(InitiateAccountCreditReviewalRequest request);

    Task<string> GetAttachmentDownloadUri(Guid accountId, Guid attachmentId);
}
