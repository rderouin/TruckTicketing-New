using System;
using System.Threading.Tasks;

using SE.TruckTicketing.Contracts.Models.Accounts;

namespace SE.TruckTicketing.UI.Contracts.Services;

public interface IUserProfileService : IServiceBase<UserProfile, Guid>
{
    Task<string> GetSignatureUploadUrl();

    Task<string> GetSignatureDownloadUrl();
}
