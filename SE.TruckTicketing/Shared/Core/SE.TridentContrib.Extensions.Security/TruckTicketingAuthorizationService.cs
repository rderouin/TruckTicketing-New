using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

using Newtonsoft.Json;

using SE.Shared.Common.Threading;
using SE.TruckTicketing.Contracts.Security;

using Trident.Contracts.Configuration;
using Trident.Extensions;
using Trident.Security;

namespace SE.TridentContrib.Extensions.Security;

public class TruckTicketingAuthorizationService : AuthorizationService, ITruckTicketingAuthorizationService
{
    private const string TruckTicketingIdentityLabel = "TruckTicketingIdentity";

    private static readonly Dictionary<string, string> EmptyLookup = new();

    private readonly IAppSettings _appSettings;

    private readonly ILogger<AuthorizationService> _logger;

    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    private readonly AsyncLazy<TokenValidationParameters> _tokenValidationParameters;

    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _userFacilityAccessLevels = new();

    public TruckTicketingAuthorizationService(IAppSettings appSettings, ILogger<AuthorizationService> logger) : base(appSettings, logger)
    {
        _appSettings = appSettings;
        _logger = logger;
        _tokenValidationParameters = new(RetrieveTokenValidationParameters);
    }

    public string GetFacilityAccessLevel(ClaimsPrincipal user, string facilityId)
    {
        var lookup = GetFacilityAccessLookup(user);
        if (lookup.TryGetValue(FacilityAccessConstants.AllFacilityAccessFacilityId, out var level))
        {
            return level;
        }

        if (lookup.TryGetValue(facilityId, out level))
        {
            return level;
        }

        return null;
    }

    public bool HasFacilityAccess(ClaimsPrincipal user, string facilityId)
    {
        var lookup = GetFacilityAccessLookup(user);
        return lookup.ContainsKey(FacilityAccessConstants.AllFacilityAccessFacilityId) ||
               lookup.ContainsKey(facilityId);
    }

    public new bool HasPermission(ClaimsPrincipal user, string resource, string operation)
    {
        return HasPermission(user, resource, operation, FacilityAccessConstants.AllFacilityAccessFacilityId);
    }

    public bool HasPermission(ClaimsPrincipal user, string resource, string operation, string facilityId)
    {
        var accessLevel = GetFacilityAccessLevel(user, facilityId) ?? FacilityAccessLevels.InheritedRoles;
        if (accessLevel == FacilityAccessLevels.InheritedRoles)
        {
            SetTruckTicketingIdentityClaim(user, new(ClaimConstants.Permissions, GetClaim(user, TruckTicketingClaimTypes.Permissions)));
        }

        return accessLevel switch
               {
                   FacilityAccessLevels.Admin => true,
                   FacilityAccessLevels.ReadOnly => operation == Permissions.Operations.Read,
                   FacilityAccessLevels.InheritedRoles => base.HasPermission(user, resource, operation),
                   _ => false,
               };
    }

    public bool HasPermissionInAnyFacility(ClaimsPrincipal user, string resource, string operation)
    {
        return GetFacilityAccessLookup(user).Keys.Any(facilityId => HasPermission(user, resource, operation, facilityId));
    }

    public void SetTruckTicketingIdentityClaim(ClaimsPrincipal user, Claim claim)
    {
        var identity = user.Identities.FirstOrDefault(i => i.Label == TruckTicketingIdentityLabel);
        if (identity is null)
        {
            identity = new(nameof(TruckTicketingAuthorizationService)) { Label = TruckTicketingIdentityLabel };
            user.AddIdentity(identity);
        }

        var existingClaim = user.FindFirst(claim.Type);
        identity.TryRemoveClaim(existingClaim);
        identity.AddClaim(claim);
    }

    public ICollection<string> GetAccessibleFacilityIds(ClaimsPrincipal user)
    {
        return GetFacilityAccessLookup(user).Keys.ToArray();
    }

    public new async Task<ClaimsPrincipal> ValidateToken(string token)
    {
        try
        {
            return _tokenHandler.ValidateToken(token, await _tokenValidationParameters.Value, out var validatedToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while validating the user's authorization token.");
            throw;
        }
    }

    private IReadOnlyDictionary<string, string> GetFacilityAccessLookup(ClaimsPrincipal user)
    {
        if (user != null && user.Identity!.IsAuthenticated)
        {
            var lookupJson = user.FindFirst(TruckTicketingClaimTypes.FacilityAccess)?.Value.FromBase64() ?? "{}";
            var lookup = JsonConvert.DeserializeObject<Dictionary<string, string>>(lookupJson) ?? EmptyLookup;
            return lookup;
        }

        return EmptyLookup;
    }

    private async Task<TokenValidationParameters> RetrieveTokenValidationParameters()
    {
        var tokenValidationConfig = _appSettings.GetSection<TokenValidationConfig>("TokenValidation");

        var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(tokenValidationConfig.MetadataAddress,
                                                                                        new OpenIdConnectConfigurationRetriever(),
                                                                                        new HttpDocumentRetriever());

        var discoveryDocument = await configurationManager.GetConfigurationAsync(default);
        var signingKeys = discoveryDocument.SigningKeys;

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = tokenValidationConfig.ValidIssuer,
            ValidAudience = tokenValidationConfig.ValidAudience,
            IssuerSigningKeys = signingKeys,
        };

        if (!string.IsNullOrWhiteSpace(tokenValidationConfig.IssuerSigningKey))
        {
            tokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(tokenValidationConfig.IssuerSigningKey));
        }

        return tokenValidationParameters;
    }
}
