using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

using SE.Shared.Domain.Entities.UserProfile;
using SE.TokenService.Contracts.Api.Models.Accounts;
using SE.TokenService.Contracts.Api.Routes;
using SE.TokenService.Domain.Security;

using Trident.Azure.Functions;
using Trident.Contracts;
using Trident.Extensions;
using Trident.Logging;
using Trident.Mapper;

namespace SE.TokenService.Api.Functions;

public class AccountFunctions : HttpFunctionApiBase<B2CApiConnectorRequest, UserProfileEntity, Guid>
{
    private readonly ISecurityClaimsManager _claimsManager;

    private readonly IManager<Guid, UserProfileEntity> _userProfileManager;

    public AccountFunctions(ILog log,
                            IMapperRegistry mapper,
                            IManager<Guid, UserProfileEntity> userProfileManager,
                            ISecurityClaimsManager claimsManager) : base(log, mapper, userProfileManager)
    {
        _userProfileManager = userProfileManager;
        _claimsManager = claimsManager;
    }

    [Function(nameof(PreSignUp))]
    [OpenApiOperation(nameof(PreSignUp), nameof(AccountFunctions))]
    [OpenApiRequestBody(ContentTypes.JSON, typeof(B2CApiConnectorRequest))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ContentTypes.JSON, typeof(B2CApiConnectorContinuationResponse))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ContentTypes.JSON, typeof(B2CApiConnectorBlockingResponse))]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    public async Task<HttpResponseData> PreSignUp([HttpTrigger(AuthorizationLevel.Function, nameof(HttpMethod.Post), Route = Routes.Account_PreSignUpRoute)] HttpRequestData request)
    {
        var response = request.CreateResponse(HttpStatusCode.OK);

        if ((await Create(request, nameof(PreSignUp))).StatusCode == HttpStatusCode.OK)
        {
            await response.WriteAsJsonAsync(new B2CApiConnectorContinuationResponse());
        }
        else
        {
            await response.WriteAsJsonAsync(new B2CApiConnectorBlockingResponse("Something went wrong while creating your user profile in the Truck Ticketing application."));
        }

        return response;
    }

    [Function(nameof(PreTokenIssuance))]
    [OpenApiOperation(nameof(PreTokenIssuance), nameof(AccountFunctions))]
    [OpenApiRequestBody(ContentTypes.JSON, typeof(B2CApiConnectorRequest))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ContentTypes.JSON, typeof(B2CApiConnectorContinuationResponse))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ContentTypes.JSON, typeof(B2CApiConnectorBlockingResponse))]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    public async Task<HttpResponseData> PreTokenIssuance([HttpTrigger(AuthorizationLevel.Function, nameof(HttpMethod.Post), Route = Routes.Account_PreTokenIssuanceRoute)] HttpRequestData req)
    {
        var response = req.CreateResponse();
        response.StatusCode = HttpStatusCode.OK;

        try
        {
            var apiConnectorRequest = await req.ReadFromJsonAsync<B2CApiConnectorRequest>();

            AppLogger.Information(messageTemplate: "{0}", propertyValues: new Dictionary<string, object> { { "Data", apiConnectorRequest.ToJson() } });

            var profile = Mapper.Map<UserProfileEntity>(apiConnectorRequest);

            var claims = await _claimsManager.GetUserSecurityClaimsByExternalAuthId(profile.ExternalAuthId);

            if (claims == null)
            {
                AppLogger.Warning(messageTemplate: "Could not find user profile. Attempting to create a new profile.");
                await _userProfileManager.Save(profile);
                claims = await _claimsManager.GetUserSecurityClaimsByExternalAuthId(profile.ExternalAuthId);
            }

            await response.WriteAsJsonAsync(new B2CApiConnectorClaimsContinuationResponse(claims.Roles, claims.Permissions, claims.FacilityAccess));
            return response;
        }
        catch (Exception ex)
        {
            AppLogger.Error(exception: ex, messageTemplate: "Exception occurred while trying to fetch security claims." + ex);
        }

        await response.WriteAsJsonAsync(new B2CApiConnectorBlockingResponse("Something went wrong while trying to fetch your user profile from the Truck Ticketing application."));
        return response;
    }
}
