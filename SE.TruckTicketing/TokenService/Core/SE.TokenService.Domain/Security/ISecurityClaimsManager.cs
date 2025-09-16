using System.Threading.Tasks;

namespace SE.TokenService.Domain.Security;

public interface ISecurityClaimsManager
{
    Task<SecurityClaimsBag> GetUserSecurityClaimsByExternalAuthId(string externalAuthId);
}
