using System.Security.Claims;

using Trident.Security;

namespace SE.TridentContrib.Extensions.Security;

public interface ITruckTicketingAuthorizationService : IAuthorizationService
{
    bool HasPermission(ClaimsPrincipal user, string resource, string operation, string facilityId);

    bool HasPermissionInAnyFacility(ClaimsPrincipal user, string resource, string operation);

    bool HasFacilityAccess(ClaimsPrincipal user, string facilityId);

    string GetFacilityAccessLevel(ClaimsPrincipal user, string facilityId);

    void SetTruckTicketingIdentityClaim(ClaimsPrincipal user, Claim claim);

    ICollection<string> GetAccessibleFacilityIds(ClaimsPrincipal user);
}
