using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.TruckTicketing.Contracts.Security;

using Trident.Contracts.Configuration;
using Trident.Extensions;
using Trident.Security;
using Trident.Testing.TestScopes;

namespace SE.TridentContrib.Extensions.Security.Tests;

[TestClass]
public class TruckTicketAuthorizationServiceTests
{
    [DataTestMethod]
    [DataRow(Permissions.Operations.Write)]
    [DataRow(Permissions.Operations.Approve)]
    [DataRow(Permissions.Operations.Read)]
    [DataRow(Permissions.Operations.Delete)]
    public void HasPermission_ShouldReturnTrueForAnyOperation_WhenUserHasAdminAccessLevelForAllFacility(string operation)
    {
        // arrange
        var scope = new DefaultScope();
        var facilityAccessClaim = scope.BuildFacilityAccessClaim((FacilityAccessConstants.AllFacilityAccessFacilityId, FacilityAccessLevels.Admin));
        var user = scope.CreateClaimsPrincipal("", facilityAccessClaim);

        // act
        var hasPermission = scope.InstanceUnderTest.HasPermission(user, scope.TestPermissionsResource, operation);

        // assert
        hasPermission.Should().BeTrue();
    }

    [DataTestMethod]
    [DataRow(Permissions.Operations.Write)]
    [DataRow(Permissions.Operations.Approve)]
    [DataRow(Permissions.Operations.Read)]
    [DataRow(Permissions.Operations.Delete)]
    public void HasPermissionInAnyFacility_ShouldReturnTrueForAnyOperation_WhenUserHasAdminAccessLevelForAnyFacility(string operation)
    {
        // arrange
        var scope = new DefaultScope();
        var facilityAccessClaim = scope.BuildFacilityAccessClaim((scope.AuthourizedFacilityId, FacilityAccessLevels.Admin));
        var user = scope.CreateClaimsPrincipal("", facilityAccessClaim);

        // act
        var hasPermission = scope.InstanceUnderTest.HasPermissionInAnyFacility(user, scope.TestPermissionsResource, operation);

        // assert
        hasPermission.Should().BeTrue();
    }

    [DataTestMethod]
    [DataRow(Permissions.Operations.Write)]
    [DataRow(Permissions.Operations.Approve)]
    [DataRow(Permissions.Operations.Read)]
    [DataRow(Permissions.Operations.Delete)]
    public void HasPermission_ShouldReturnFalseForAnyOperation_WhenUserIsNotAuthenticated(string operation)
    {
        // arrange
        var scope = new DefaultScope();
        var facilityAccessClaim = scope.BuildFacilityAccessClaim((FacilityAccessConstants.AllFacilityAccessFacilityId, FacilityAccessLevels.Admin));
        var user = scope.CreateClaimsPrincipal("", facilityAccessClaim, false);

        // act
        var hasPermission = scope.InstanceUnderTest.HasPermission(user, scope.TestPermissionsResource, operation, FacilityAccessConstants.AllFacilityAccessFacilityId);

        // assert
        hasPermission.Should().BeFalse();
    }

    [DataTestMethod]
    [DataRow(Permissions.Operations.Write)]
    [DataRow(Permissions.Operations.Approve)]
    [DataRow(Permissions.Operations.Read)]
    [DataRow(Permissions.Operations.Delete)]
    public void HasPermissionWithFacility_ShouldReturnTrueForAnyOperation_WhenUserHasAdminAccessLevelForSpecificFacility(string operation)
    {
        // arrange
        var scope = new DefaultScope();
        var facilityAccessClaim = scope.BuildFacilityAccessClaim((FacilityAccessConstants.AllFacilityAccessFacilityId, FacilityAccessLevels.Admin));
        var user = scope.CreateClaimsPrincipal("", facilityAccessClaim);

        // act
        var hasPermission = scope.InstanceUnderTest.HasPermission(user, scope.TestPermissionsResource, operation, FacilityAccessConstants.AllFacilityAccessFacilityId);

        // assert
        hasPermission.Should().BeTrue();
    }

    [DataTestMethod]
    [DataRow(Permissions.Operations.Write)]
    [DataRow(Permissions.Operations.Approve)]
    [DataRow(Permissions.Operations.Read)]
    [DataRow(Permissions.Operations.Delete)]
    public void HasPermissionWithFacility_ShouldReturnFalseForAnyOperation_WhenUserHasDoesNotHaveFacilityAccess(string operation)
    {
        // arrange
        var scope = new DefaultScope();
        var facilityAccessClaim = scope.BuildFacilityAccessClaim((scope.AuthourizedFacilityId, FacilityAccessLevels.Admin));
        var user = scope.CreateClaimsPrincipal("", facilityAccessClaim);

        // act
        var hasPermission = scope.InstanceUnderTest.HasPermission(user, scope.TestPermissionsResource, operation, scope.UnAuthorizedFacilityId);

        // assert
        hasPermission.Should().BeFalse();
    }

    [DataTestMethod]
    [DataRow(Permissions.Operations.Write, false)]
    [DataRow(Permissions.Operations.Approve, false)]
    [DataRow(Permissions.Operations.Read, true)]
    [DataRow(Permissions.Operations.Delete, false)]
    public void HasPermissionInAnyFacility_ShouldReturnTrueForAnyReadOperation_WhenUserHasReadOnlyAccesLevelForAnyFacility(string operation, bool expected)
    {
        // arrange
        var scope = new DefaultScope();
        var facilityAccessClaim = scope.BuildFacilityAccessClaim((scope.AuthourizedFacilityId, FacilityAccessLevels.ReadOnly));
        var user = scope.CreateClaimsPrincipal("", facilityAccessClaim);

        // act
        var hasPermission = scope.InstanceUnderTest.HasPermissionInAnyFacility(user, scope.TestPermissionsResource, operation);

        // assert
        hasPermission.Should().Be(expected);
    }

    [DataTestMethod]
    [DataRow(Permissions.Operations.Write, false)]
    [DataRow(Permissions.Operations.Approve, false)]
    [DataRow(Permissions.Operations.Read, true)]
    [DataRow(Permissions.Operations.Delete, false)]
    public void HasPermissionWithFacility_ShouldReturnTrueForAnyReadOperation_WhenUserHasReadOnlyAccesLevelForASpecificFacility(string operation, bool expected)
    {
        // arrange
        var scope = new DefaultScope();
        var facilityAccessClaim = scope.BuildFacilityAccessClaim((scope.AuthourizedFacilityId, FacilityAccessLevels.ReadOnly), ("AnotherFacilityId", FacilityAccessLevels.InheritedRoles));
        var user = scope.CreateClaimsPrincipal("", facilityAccessClaim);

        // act
        var hasPermission = scope.InstanceUnderTest.HasPermission(user, scope.TestPermissionsResource, operation, scope.AuthourizedFacilityId);

        // assert
        hasPermission.Should().Be(expected);
    }

    [DataTestMethod]
    [DataRow(Permissions.Operations.Write, true)]
    [DataRow(Permissions.Operations.Approve, false)]
    [DataRow(Permissions.Operations.Read, false)]
    [DataRow(Permissions.Operations.Delete, true)]
    public void HasPermissionWithFacility_ShouldReturnTrueForSpecificOperation_WhenUserHasInheritedRoleLevelForSpecificFacility(string operation, bool expected)
    {
        // arrange
        var scope = new DefaultScope();
        var facilityAccessClaim = scope.BuildFacilityAccessClaim((scope.AuthourizedFacilityId, FacilityAccessLevels.InheritedRoles), ("AnotherFacilityId", FacilityAccessLevels.Admin));
        var permissionsClaim = scope.BuildPermissionsClaim(Permissions.Operations.Write, Permissions.Operations.Delete);
        var user = scope.CreateClaimsPrincipal(permissionsClaim, facilityAccessClaim);

        // act
        var hasPermission = scope.InstanceUnderTest.HasPermission(user, scope.TestPermissionsResource, operation, scope.AuthourizedFacilityId);

        // assert
        hasPermission.Should().Be(expected);
    }

    [DataTestMethod]
    [DataRow("FacilityA")]
    [DataRow("FacilityB")]
    [DataRow(FacilityAccessConstants.AllFacilityAccessFacilityId)]
    public void HasFacilityAccess_ShouldReturnTrueForAnyFacility_WhenUserHasAllFacilityAccess(string facility)
    {
        // arrange
        var scope = new DefaultScope();
        var facilityAccessClaim = scope.BuildFacilityAccessClaim((FacilityAccessConstants.AllFacilityAccessFacilityId, FacilityAccessLevels.InheritedRoles));
        var user = scope.CreateClaimsPrincipal("", facilityAccessClaim);

        // act
        var hasFacilityAccess = scope.InstanceUnderTest.HasFacilityAccess(user, facility);

        // assert
        hasFacilityAccess.Should().BeTrue();
    }

    [TestMethod]
    public void HasFacilityAccess_ShouldReturnTrueForSpecificFacility_WhenUserHasSpecificFacilityAccess()
    {
        // arrange
        var scope = new DefaultScope();
        var facilityAccessClaim = scope.BuildFacilityAccessClaim((scope.AuthourizedFacilityId, FacilityAccessLevels.InheritedRoles));
        var user = scope.CreateClaimsPrincipal("", facilityAccessClaim);

        // act
        var hasFacilityAccess = scope.InstanceUnderTest.HasFacilityAccess(user, scope.AuthourizedFacilityId);

        // assert
        hasFacilityAccess.Should().BeTrue();
    }

    [TestMethod]
    public void HasFacilityAccess_ShouldReturnFalseForSpecificFacility_WhenUserHasDifferentSpecificFacilityAccess()
    {
        // arrange
        var scope = new DefaultScope();
        var facilityAccessClaim = scope.BuildFacilityAccessClaim((scope.AuthourizedFacilityId, FacilityAccessLevels.InheritedRoles));
        var user = scope.CreateClaimsPrincipal("", facilityAccessClaim);

        // act
        var hasFacilityAccess = scope.InstanceUnderTest.HasFacilityAccess(user, scope.UnAuthorizedFacilityId);

        // assert
        hasFacilityAccess.Should().BeFalse();
    }

    [TestMethod]
    public void HasFacilityAccess_ShouldReturnFalseForSpecificFacility_WhenUserDoesNotHaveSpecificFacilityAccess()
    {
        // arrange
        var scope = new DefaultScope();
        var user = scope.CreateClaimsPrincipal("", "");

        // act
        var hasFacilityAccess = scope.InstanceUnderTest.HasFacilityAccess(user, scope.UnAuthorizedFacilityId);

        // assert
        hasFacilityAccess.Should().BeFalse();
    }

    [DataTestMethod]
    [DataRow("FacilityA")]
    [DataRow("FacilityB")]
    [DataRow(FacilityAccessConstants.AllFacilityAccessFacilityId)]
    public void HasFacilityAccess_ShouldReturnFalseForAnyFacility_WhenUserDoesNotHaveFacilityAccessClaim(string facility)
    {
        // arrange
        var scope = new DefaultScope();
        var user = new ClaimsPrincipal(new ClaimsIdentity("Test"));

        // act
        var hasFacilityAccess = scope.InstanceUnderTest.HasFacilityAccess(user, facility);

        // assert
        hasFacilityAccess.Should().BeFalse();
    }

    [DataTestMethod]
    [DataRow("FacilityA")]
    [DataRow("FacilityB")]
    [DataRow(FacilityAccessConstants.AllFacilityAccessFacilityId)]
    public void HasFacilityAccess_ShouldReturnFalseForAnyFacility_WhenUserIsNull(string facility)
    {
        // arrange
        var scope = new DefaultScope();

        // act
        var hasFacilityAccess = scope.InstanceUnderTest.HasFacilityAccess(null, facility);

        // assert
        hasFacilityAccess.Should().BeFalse();
    }

    [TestMethod]
    public async Task ValidateToken_ShouldReturnClaimsPrincipal_WhenTokenIsValid()
    {
        // arrange
        var scope = new DefaultScope();
        var validIssuer = "https://secureenergyappsdev.b2clogin.com/ee0d36b4-ff5d-4482-ae67-075ce25203c9/v2.0/";
        var validAudience = "12147717-a2b5-44f3-acd1-ca88bdc24fbe";
        var signingKey = Guid.NewGuid().ToString();

        scope.SettingsMock.Setup(m => m.GetSection<TokenValidationConfig>("TokenValidation")).Returns(new TokenValidationConfig
        {
            ValidIssuer = validIssuer,
            ValidAudience = validAudience,
            IssuerSigningKey = signingKey,
            MetadataAddress = "https://secureenergyappsdev.b2clogin.com/ee0d36b4-ff5d-4482-ae67-075ce25203c9/v2.0/.well-known/openid-configuration?p=B2C_1_SignUpSignIn",
        });

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.WriteToken(new JwtSecurityToken(validIssuer, validAudience,
                                                                 expires: DateTime.UtcNow.AddMinutes(5),
                                                                 signingCredentials: new(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256)));

        // act
        var principal = await scope.InstanceUnderTest.ValidateToken(token);

        // assert
        principal.Should().NotBeNull();
    }

    [TestMethod]
    public async Task ValidateToken_ShouldThrown_WhenTokenIsInValid()
    {
        // arrange
        var scope = new DefaultScope();
        scope.SettingsMock.Setup(m => m.GetSection<TokenValidationConfig>("TokenValidation")).Returns(new TokenValidationConfig
        {
            ValidIssuer = "https://secureenergyappsdev.b2clogin.com/ee0d36b4-ff5d-4482-ae67-075ce25203c9/v2.0/",
            ValidAudience = "12147717-a2b5-44f3-acd1-ca88bdc24fbe",
            MetadataAddress = "https://secureenergyappsdev.b2clogin.com/ee0d36b4-ff5d-4482-ae67-075ce25203c9/v2.0/.well-known/openid-configuration?p=B2C_1_SignUpSignIn",
        });

        // act
        var validateToken = () => scope.InstanceUnderTest.ValidateToken("An obviously invalid JWT Token");

        // assert
        await validateToken.Should().ThrowAsync<Exception>();
    }

    internal class DefaultScope : TestScope<ITruckTicketingAuthorizationService>
    {
        public string AuthourizedFacilityId = "1";

        public string TestPermissionsResource = nameof(TestPermissionsResource);

        public string UnAuthorizedFacilityId = "2";

        public DefaultScope()
        {
            InstanceUnderTest = new TruckTicketingAuthorizationService(SettingsMock.Object, LoggerMock.Object);
        }

        public Mock<ILogger<AuthorizationService>> LoggerMock { get; } = new();

        public Mock<IAppSettings> SettingsMock { get; } = new();

        public ClaimsPrincipal CreateClaimsPrincipal(string permissionsClaim, string facilityAccessClaim, bool isAuthenticated = true)
        {
            var identity =
                new
                    ClaimsIdentity(new[] { new Claim(ClaimConstants.Sub, Guid.NewGuid().ToString()), new Claim(TruckTicketingClaimTypes.Permissions, permissionsClaim), new Claim(TruckTicketingClaimTypes.FacilityAccess, facilityAccessClaim) },
                                   isAuthenticated ? "Test" : null);

            var prinipal = new ClaimsPrincipal(identity);
            return prinipal;
        }

        public string BuildPermissionsClaim(params string[] permissions)
        {
            return new Dictionary<string, string> { { TestPermissionsResource, string.Concat(permissions) } }.ToJson().ToBase64();
        }

        public string BuildFacilityAccessClaim(params (string facilityId, string accessLevel)[] facilityAccessLevels)
        {
            return facilityAccessLevels.ToDictionary(x => x.facilityId, x => x.accessLevel).ToJson().ToBase64();
        }
    }
}
