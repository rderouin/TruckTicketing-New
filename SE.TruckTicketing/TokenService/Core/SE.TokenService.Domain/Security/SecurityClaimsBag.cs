namespace SE.TokenService.Domain.Security;

public class SecurityClaimsBag
{
    public string Roles { get; set; }

    public string Permissions { get; set; }

    public string FacilityAccess { get; set; }
}
