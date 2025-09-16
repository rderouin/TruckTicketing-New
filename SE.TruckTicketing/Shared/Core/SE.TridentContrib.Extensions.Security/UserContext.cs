using System.Security.Claims;

namespace SE.TridentContrib.Extensions.Security;

public class UserContext
{
    public ClaimsPrincipal Principal { get; set; }

    public string DisplayName { get; set; }

    public string ObjectId { get; set; }

    public string EmailAddress { get; set; }
    
    public string OriginalToken { get; set; }
}
