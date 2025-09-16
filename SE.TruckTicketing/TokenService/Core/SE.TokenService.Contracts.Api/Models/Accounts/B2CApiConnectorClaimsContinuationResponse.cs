using Newtonsoft.Json;

using SE.TruckTicketing.Contracts.Security;

namespace SE.TokenService.Contracts.Api.Models.Accounts;

public class B2CApiConnectorClaimsContinuationResponse : B2CApiConnectorContinuationResponse
{
    public B2CApiConnectorClaimsContinuationResponse()
    {
    }

    public B2CApiConnectorClaimsContinuationResponse(string rolesClaim, string permissionsClaim, string facilityAccessClaim)
    {
        RolesClaim = rolesClaim;
        PermissionsClaim = permissionsClaim;
        FacilityAccessClaim = facilityAccessClaim;
    }

    [JsonProperty(TruckTicketingClaimTypes.Roles)]
    public string RolesClaim { get; set; }

    [JsonProperty(TruckTicketingClaimTypes.Permissions)]
    public string PermissionsClaim { get; set; }

    [JsonProperty(TruckTicketingClaimTypes.FacilityAccess)]
    public string FacilityAccessClaim { get; set; }
}
