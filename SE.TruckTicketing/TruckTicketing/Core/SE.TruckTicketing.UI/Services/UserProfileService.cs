using System;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Api.Models;
using SE.TruckTicketing.Contracts.Models.Accounts;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Service.Resources.userProfiles)]
public class UserProfileService : ServiceBase<UserProfileService, UserProfile, Guid>, IUserProfileService
{
    public UserProfileService(ILogger<UserProfileService> logger,
                              IHttpClientFactory httpClientFactory)
        : base(logger, httpClientFactory)
    {
    }

    public async Task<string> GetSignatureUploadUrl()
    {
        var response = await SendRequest<UriDto>(HttpMethod.Post.Method, Routes.UserProfile_Signature, new());
        return response.Model.Uri;
    }

    public async Task<string> GetSignatureDownloadUrl()
    {
        var response = await SendRequest<UriDto>(HttpMethod.Get.Method, Routes.UserProfile_Signature, new());
        return response.Model.Uri;
    }
}
