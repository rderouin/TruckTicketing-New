using System;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

using SE.Shared.Domain.Entities.UserProfile;
using SE.TridentContrib.Extensions.Security;
using SE.TruckTicketing.Contracts.Api.Models;
using SE.TruckTicketing.Contracts.Models.Accounts;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.Contracts.Security;

using Trident.Azure.Functions;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions;

[UseHttpFunction(HttpFunctionApiMethod.GetById, Route = Routes.UserProfile_IdRouteTemplate,
                 ClaimsAuthorizeResource = Permissions.Resources.UserProfile,
                 ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.UserProfile_SearchRoute, 
                 ClaimsAuthorizeResource = Permissions.Resources.UserProfile,
                 ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Update,
                 Route = Routes.UserProfile_IdRouteTemplate,
                 ClaimsAuthorizeResource = Permissions.Resources.UserProfile,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
public partial class UserProfileFunctions : HttpFunctionApiBase<UserProfile, UserProfileEntity, Guid>
{
    private readonly IUserProfileManager _manager;

    private readonly IUserContextAccessor _userContextAccessor;

    public UserProfileFunctions(ILog log,
                                IMapperRegistry mapper,
                                IUserContextAccessor userContextAccessor,
                                IUserProfileManager manager)
        : base(log, mapper, manager)
    {
        _manager = manager;
        _userContextAccessor = userContextAccessor;
    }

    [Function(nameof(GetUploadSignatureUri))]
    [OpenApiOperation(nameof(GetUploadSignatureUri), nameof(UserProfileFunctions), Summary = Routes.UserProfile_Signature)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(UriDto))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    public async Task<HttpResponseData> GetUploadSignatureUri([HttpTrigger(AuthorizationLevel.Anonymous,
                                                                           nameof(HttpMethod.Post),
                                                                           Route = Routes.UserProfile_Signature)]
                                                              HttpRequestData httpRequestData)
    {
        HttpResponseData httpResponseData;
        try
        {
            var uri = await _manager.GetSignatureUploadUri(_userContextAccessor.UserContext.ObjectId);
            httpResponseData = httpRequestData.CreateResponse(HttpStatusCode.OK);
            await httpResponseData.WriteAsJsonAsync(new UriDto { Uri = uri });
        }
        catch (Exception e)
        {
            AppLogger.Error<HttpFunctionApiBase<UserProfile, UserProfileEntity, Guid>>(e, e.Message);
            httpResponseData = httpRequestData.CreateResponse(HttpStatusCode.InternalServerError);
        }

        return httpResponseData;
    }

    [Function(nameof(GetSignatureDownloadUri))]
    [OpenApiOperation(nameof(GetSignatureDownloadUri), nameof(UserProfileFunctions), Summary = Routes.UserProfile_Signature)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(UriDto))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    public async Task<HttpResponseData> GetSignatureDownloadUri([HttpTrigger(AuthorizationLevel.Anonymous,
                                                                             nameof(HttpMethod.Get),
                                                                             Route = Routes.UserProfile_Signature)]
                                                                HttpRequestData httpRequestData)
    {
        HttpResponseData httpResponseData;
        try
        {
            var uri = await _manager.GetSignatureDownloadUri(_userContextAccessor.UserContext.ObjectId);
            httpResponseData = httpRequestData.CreateResponse(HttpStatusCode.OK);
            await httpResponseData.WriteAsJsonAsync(new UriDto { Uri = uri });
        }
        catch (Exception e)
        {
            AppLogger.Error<HttpFunctionApiBase<UserProfile, UserProfileEntity, Guid>>(e, e.Message);
            httpResponseData = httpRequestData.CreateResponse(HttpStatusCode.InternalServerError);
        }

        return httpResponseData;
    }
}
