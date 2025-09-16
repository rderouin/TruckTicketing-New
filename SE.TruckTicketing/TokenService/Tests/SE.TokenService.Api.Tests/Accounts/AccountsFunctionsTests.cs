using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using AutoMapper;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.UserProfile;
using SE.TokenService.Api.Configuration;
using SE.TokenService.Api.Functions;
using SE.TokenService.Contracts.Api.Models.Accounts;
using SE.TokenService.Domain.Security;

using Trident.Contracts;
using Trident.Extensions;
using Trident.Logging;
using Trident.Mapper;
using Trident.Testing.TestScopes;
using Trident.Validation;

namespace SE.TokenService.Api.Tests.Accounts;

[TestClass]
public class AccountFunctionsTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task PreSignUp_ShouldReturnContinuationResponse_ForValidRequest()
    {
        // arrange
        var scope = new DefaultScope();
        var apiConnectorRequest = scope.CreateValidB2CApiconnectorRequest();
        var functionsRequest = scope.CreateHttpRequest(null, apiConnectorRequest.ToJson());

        scope.UserManagerMock.Setup(manager => manager.Save(It.IsAny<UserProfileEntity>(), It.IsAny<bool>()))
             .Returns<UserProfileEntity, bool>((profile, _) => Task.FromResult(profile));

        // act
        var httpResponseData = await scope.InstanceUnderTest.PreSignUp(functionsRequest);
        var response = await httpResponseData.ReadJsonToObject<B2CApiConnectorResponse>();

        // assert
        scope.UserManagerMock.Verify(manager => manager.Save(It.Is<UserProfileEntity>(profile => profile.ExternalAuthId == apiConnectorRequest.Identities.First().IssuerAssignedId),
                                                             It.IsAny<bool>()));

        httpResponseData.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Action.Should().Be(B2CApiConnectorContinuationResponse.ActionDescriptor);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task B2CFunctions_PreSignUp_ShouldReturnBlockingResponse_ForInvalidRequest()
    {
        // arrange
        var scope = new DefaultScope();
        var apiConnectorRequest = scope.CreateValidB2CApiconnectorRequest();
        var functionsRequest = scope.CreateHttpRequest(null, apiConnectorRequest.ToJson());

        scope.UserManagerMock.Setup(scope => scope.Save(It.IsAny<UserProfileEntity>(), It.IsAny<bool>()))
             .ThrowsAsync(new ValidationRollupException());

        // act
        var httpResponseData = await scope.InstanceUnderTest.PreSignUp(functionsRequest);
        var response = await httpResponseData.ReadJsonToObject<B2CApiConnectorResponse>();

        // assert
        httpResponseData.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Action.Should().Be(B2CApiConnectorBlockingResponse.ActionDescriptor);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task B2CFunctions_PreTokenIssuance_ShouldReturnContinueResponse_WhenUserProfileClaimsCanBeRetrieved()
    {
        // arrange
        var scope = new DefaultScope();
        var apiConnectorRequest = scope.CreateValidB2CApiconnectorRequest();
        var functionsRequest = scope.CreateHttpRequest(null, apiConnectorRequest.ToJson());

        const string testRolesClaim = nameof(testRolesClaim);
        const string testPermissionsClaim = nameof(testPermissionsClaim);
        const string testFacilityAccessClaim = nameof(testFacilityAccessClaim);

        var expectedClaims = new SecurityClaimsBag
        {
            Roles = nameof(testRolesClaim),
            Permissions = nameof(testPermissionsClaim),
            FacilityAccess = nameof(testFacilityAccessClaim),
        };

        scope.ClaimsManagerMock.Setup(scope => scope.GetUserSecurityClaimsByExternalAuthId(It.IsAny<string>()))
             .ReturnsAsync(expectedClaims);

        // act
        var httpResponseData = await scope.InstanceUnderTest.PreTokenIssuance(functionsRequest);
        var response = await httpResponseData.ReadJsonToObject<B2CApiConnectorClaimsContinuationResponse>();

        // assert
        httpResponseData.StatusCode.Should().Be(HttpStatusCode.OK);
        response.RolesClaim.Should().BeEquivalentTo(expectedClaims.Roles);
        response.PermissionsClaim.Should().BeEquivalentTo(expectedClaims.Permissions);
        response.FacilityAccessClaim.Should().BeEquivalentTo(expectedClaims.FacilityAccess);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task B2CFunctions_PreTokenIssuance_ShouldReturnBlockingResponse_IfManagerThrowsException()
    {
        // arrange
        var scope = new DefaultScope();
        var apiConnectorRequest = scope.CreateValidB2CApiconnectorRequest();
        var functionsRequest = scope.CreateHttpRequest(null, apiConnectorRequest.ToJson());

        scope.ClaimsManagerMock.Setup(scope => scope.GetUserSecurityClaimsByExternalAuthId(It.IsAny<string>()))
             .ThrowsAsync(new ArgumentException());

        // act
        var httpResponseData = await scope.InstanceUnderTest.PreTokenIssuance(functionsRequest);
        var response = await httpResponseData.ReadJsonToObject<B2CApiConnectorResponse>();

        // assert
        httpResponseData.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Action.Should().Be(B2CApiConnectorBlockingResponse.ActionDescriptor);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task B2CFunctions_PreTokenIssuance_ShouldReturnBlockingResponse_IfManagerReturnsNull()
    {
        // arrange
        var scope = new DefaultScope();
        var apiConnectorRequest = scope.CreateValidB2CApiconnectorRequest();
        var functionsRequest = scope.CreateHttpRequest(null, apiConnectorRequest.ToJson());

        scope.ClaimsManagerMock.Setup(scope => scope.GetUserSecurityClaimsByExternalAuthId(It.IsAny<string>()))
             .Returns(Task.FromResult<SecurityClaimsBag>(null));

        // act
        var httpResponseData = await scope.InstanceUnderTest.PreTokenIssuance(functionsRequest);
        var response = await httpResponseData.ReadJsonToObject<B2CApiConnectorResponse>();

        // assert
        httpResponseData.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Action.Should().Be(B2CApiConnectorBlockingResponse.ActionDescriptor);
    }

    private class DefaultScope : HttpTestScope<AccountFunctions>
    {
        public DefaultScope()
        {
            MapperRegistry = new ServiceMapperRegistry(new Profile[] { new ApiMapperProfile() });

            InstanceUnderTest = new(LogMock.Object,
                                    MapperRegistry,
                                    UserManagerMock.Object,
                                    ClaimsManagerMock.Object);
        }

        public IMapperRegistry MapperRegistry { get; }

        public Mock<ILog> LogMock { get; } = new();

        public Mock<IManager<Guid, UserProfileEntity>> UserManagerMock { get; } = new();

        public Mock<ISecurityClaimsManager> ClaimsManagerMock { get; } = new();

        public B2CApiConnectorRequest CreateValidB2CApiconnectorRequest()
        {
            return new()
            {
                GivenName = "Jack",
                Surname = "Jackson",
                DisplayName = "Jack Jackson",
                Email = "jjackson@secure-energy.com",
                ObjectId = Guid.NewGuid().ToString(),
                Identities = new[]
                {
                    new Identity
                    {
                        SignInType = "federated",
                        Issuer = "https://login.microsoftonline.com",
                        IssuerAssignedId = "11111111-0000-0000-0000-000000000000",
                    },
                },
            };
        }
    }
}
