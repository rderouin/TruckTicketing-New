using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Newtonsoft.Json;

using SE.Shared.Domain.Entities.Role;
using SE.Shared.Domain.Entities.UserProfile;
using SE.TokenService.Domain.Security;
using SE.TruckTicketing.Contracts.Security;

using Trident.Data.Contracts;
using Trident.Domain;
using Trident.Extensions;
using Trident.Testing.TestScopes;

namespace SE.TokenService.Domain.Tests.Security;

[TestClass]
public class SecurityClaimsManagerTests
{
    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow("")]
    [DataRow(null)]
    public async Task GetUserSecurityClaimsByExternalAuthId_ShouldReturnNull_WhenExternalAuthIdIsNullOrEmtpy(string externalAuthId)
    {
        // arrange
        var scope = new DefaultScope();

        // act
        var claims = await scope.InstanceUnderTest.GetUserSecurityClaimsByExternalAuthId(externalAuthId);

        // assert
        claims.Should().BeNull();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GetUserSecurityClaimsByExternalAuthId_ShouldReturnNull_WhenUserProfileDoesNotExist()
    {
        // arrange
        var scope = new DefaultScope();
        scope.SetExistingUserProfiles(scope.GenerateUserProfileEntity());

        // act
        var claims = await scope.InstanceUnderTest.GetUserSecurityClaimsByExternalAuthId("An inexistent user profile external auth id");

        // assert
        claims.Should().BeNull();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GetUserSecurityClaimsByExternalAuthId_ShouldReturnClaimSetWithNoRoles_WhenUserProfileDoesNotHaveAnyRoles()
    {
        // arrange
        var scope = new DefaultScope();
        scope.SetExistingRoles(scope.TestRoles);

        var userProfile = scope.GenerateUserProfileEntity();
        scope.SetExistingUserProfiles(userProfile);

        // act
        var claims = await scope.InstanceUnderTest.GetUserSecurityClaimsByExternalAuthId(userProfile.ExternalAuthId);

        // assert
        claims.Roles.Should().BeNullOrEmpty();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GetUserSecurityClaimsByExternalAuthId_ShouldReturnClaimSetWithNoPermissions_WhenUserProfileDoesNotHaveAnyRoles()
    {
        // arrange
        var scope = new DefaultScope();
        scope.SetExistingRoles(scope.TestRoles);

        var userProfile = scope.GenerateUserProfileEntity();
        scope.SetExistingUserProfiles(userProfile);

        // act
        var claims = await scope.InstanceUnderTest.GetUserSecurityClaimsByExternalAuthId(userProfile.ExternalAuthId);

        // assert
        claims.Permissions.Should().BeEquivalentTo(scope.EncodedEmptyPermissionsLookup);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GetUserSecurityClaimsByExternalAuthId_ShouldReturnClaimSetWithRolesAndNoPermissions_WhenUserProfileHasRoleWithNoPermissions()
    {
        // arrange
        var scope = new DefaultScope();
        scope.SetExistingRoles(scope.TestRoles);

        var userProfile = scope.GenerateUserProfileEntity(scope.RoleWithNoPermissions);
        scope.SetExistingUserProfiles(userProfile);

        // act
        var claims = await scope.InstanceUnderTest.GetUserSecurityClaimsByExternalAuthId(userProfile.ExternalAuthId);

        // assert
        claims.Roles.Should().Contain(scope.RoleWithNoPermissions.Name);
        claims.Permissions.Should().BeEquivalentTo(scope.EncodedEmptyPermissionsLookup);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GetUserSecurityClaimsByExternalAuthId_ShouldReturnClaimSetWithPermissions_WhenUserProfileHasRoleWithPermissions()
    {
        // arrange
        var scope = new DefaultScope();
        scope.SetExistingRoles(scope.TestRoles);

        var userProfile = scope.GenerateUserProfileEntity(scope.RoleWithReadPermissions);
        scope.SetExistingUserProfiles(userProfile);
        var assignedPermissionResources = scope.RoleWithReadPermissions.Permissions.Select(permission => permission.Code);
        var assignedOperations = scope.TestResourceRead.AssignedOperations.Select(operation => operation.Value);

        // act
        var claims = await scope.InstanceUnderTest.GetUserSecurityClaimsByExternalAuthId(userProfile.ExternalAuthId);
        var actualPermissionsLookup = scope.DecodeLookupClaim(claims.Permissions);

        // assert
        actualPermissionsLookup.Should().ContainKeys(assignedPermissionResources);
        actualPermissionsLookup![scope.TestResourceRead.Code].Should().ContainAll(assignedOperations);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GetUserSecurityClaimsByExternalAuthId_ShouldReturnClaimSetWithNoRolesAndPermissions_WhenUserProfileHasDeletedRoleWithPermissions()
    {
        // arrange
        var scope = new DefaultScope();
        scope.SetExistingRoles(scope.TestRoles);

        var userProfile = scope.GenerateUserProfileEntity(scope.DeletedRoleWithReadPermissions);
        scope.SetExistingUserProfiles(userProfile);

        // act
        var claims = await scope.InstanceUnderTest.GetUserSecurityClaimsByExternalAuthId(userProfile.ExternalAuthId);

        // assert
        claims.Roles.Should().BeNullOrEmpty();
        claims.Permissions.Should().BeEquivalentTo(scope.EncodedEmptyPermissionsLookup);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GetUserSecurityClaimsByExternalAuthId_ShouldReturnClaimSetWithMultipleRoles_WhenUserProfileHasMultipleRoles()
    {
        // arrange
        var scope = new DefaultScope();
        scope.SetExistingRoles(scope.TestRoles);

        var assignedRoles = new[] { scope.RoleWithReadPermissions, scope.RoleWithReadWritePermissions };
        var userProfile = scope.GenerateUserProfileEntity(assignedRoles);
        scope.SetExistingUserProfiles(userProfile);

        // act
        var claims = await scope.InstanceUnderTest.GetUserSecurityClaimsByExternalAuthId(userProfile.ExternalAuthId);

        // assert
        claims.Roles.Should().ContainAll(assignedRoles.Select(role => role.Name));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GetUserSecurityClaimsByExternalAuthId_ShouldReturnClaimSetWithDistinctUnionOfAssignedOperationsPermissions_WhenUserProfileHasRolesWithOverlappingPermissions()
    {
        // arrange
        var scope = new DefaultScope();
        scope.SetExistingRoles(scope.TestRoles);

        var assignedRoles = new[] { scope.RoleWithReadPermissions, scope.RoleWithReadWritePermissions, scope.RoleWithDeletePermissions };
        var userProfile = scope.GenerateUserProfileEntity(assignedRoles);
        scope.SetExistingUserProfiles(userProfile);

        var assignedPermissionResources = assignedRoles.SelectMany(role => role.Permissions).Select(permission => permission.Code);
        var assignedOperations = new[] { scope.TestResourceReadWrite, scope.TestResourceDelete }
                                .SelectMany(permission => permission.AssignedOperations)
                                .Select(operation => operation.Value);

        // act
        var claims = await scope.InstanceUnderTest.GetUserSecurityClaimsByExternalAuthId(userProfile.ExternalAuthId);
        var actualPermissionsLookup = scope.DecodeLookupClaim(claims.Permissions);

        // assert
        actualPermissionsLookup.Should().ContainKeys(assignedPermissionResources);
        actualPermissionsLookup![scope.TestResourceRead.Code].Should().ContainAll(assignedOperations);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GetUserSecurityClaimsByExternalAuthId_ShouldReturnClaimSetWithAllFacilitiesAccessInheritedRolesLevel_ForADefaultUserProfile()
    {
        // arrange
        var scope = new DefaultScope();
        scope.SetExistingRoles(scope.TestRoles);

        var userProfile = scope.GenerateUserProfileEntity();
        scope.SetExistingUserProfiles(userProfile);

        // act
        var claims = await scope.InstanceUnderTest.GetUserSecurityClaimsByExternalAuthId(userProfile.ExternalAuthId);
        var facilityAccess = scope.DecodeLookupClaim(claims.FacilityAccess);

        // assert
        facilityAccess.Should().ContainKey(FacilityAccessConstants.AllFacilityAccessFacilityId);
        facilityAccess![FacilityAccessConstants.AllFacilityAccessFacilityId].Should().Be(FacilityAccessLevels.InheritedRoles);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GetUserSecurityClaimsByExternalAuthId_ShouldReturnClaimSetWithNoFacility_ForADefaultUserProfileWithSpecificFacilityAccessEnforced()
    {
        // arrange
        var scope = new DefaultScope();
        scope.SetExistingRoles(scope.TestRoles);

        var userProfile = scope.GenerateUserProfileEntity();
        userProfile.EnforceSpecificFacilityAccessLevels = true;
        scope.SetExistingUserProfiles(userProfile);

        // act
        var claims = await scope.InstanceUnderTest.GetUserSecurityClaimsByExternalAuthId(userProfile.ExternalAuthId);
        var facilityAccess = scope.DecodeLookupClaim(claims.FacilityAccess);

        // assert
        facilityAccess.Should().BeEmpty();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GetUserSecurityClaimsByExternalAuthId_ShouldReturnClaimSetWithNoFacility_ForAUserProfileWithDisabledFacilityAccessAssignment()
    {
        // arrange
        var scope = new DefaultScope();
        scope.SetExistingRoles(scope.TestRoles);

        var userProfile = scope.GenerateUserProfileEntity();
        userProfile.EnforceSpecificFacilityAccessLevels = true;
        userProfile.SpecificFacilityAccessAssignments.Add(scope.DisabledTestFacilityAccess);
        scope.SetExistingUserProfiles(userProfile);

        // act
        var claims = await scope.InstanceUnderTest.GetUserSecurityClaimsByExternalAuthId(userProfile.ExternalAuthId);
        var facilityAccess = scope.DecodeLookupClaim(claims.FacilityAccess);

        // assert
        facilityAccess.Should().BeEmpty();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GetUserSecurityClaimsByExternalAuthId_ShouldReturnClaimSetWithOnlyEnabledFacilityAccess_ForAUserProfileWithSpecificFacilityAccessAssignments()
    {
        // arrange
        var scope = new DefaultScope();
        scope.SetExistingRoles(scope.TestRoles);

        var userProfile = scope.GenerateUserProfileEntity();
        userProfile.EnforceSpecificFacilityAccessLevels = true;
        userProfile.SpecificFacilityAccessAssignments.Add(scope.TestFacilityAccess);
        userProfile.SpecificFacilityAccessAssignments.Add(scope.DisabledTestFacilityAccess);
        scope.SetExistingUserProfiles(userProfile);

        // act
        var claims = await scope.InstanceUnderTest.GetUserSecurityClaimsByExternalAuthId(userProfile.ExternalAuthId);
        var facilityAccess = scope.DecodeLookupClaim(claims.FacilityAccess);

        // assert
        facilityAccess.Should().ContainKey(scope.TestFacilityAccess.FacilityId);
        facilityAccess.Should().NotContainKey(scope.DisabledTestFacilityAccess.FacilityId);
        facilityAccess![scope.TestFacilityAccess.FacilityId].Should().Be(scope.TestFacilityAccess.Level);
    }

    private class DefaultScope : TestScope<SecurityClaimsManager>
    {
        public const string TestResource = nameof(TestResource);

        public UserProfileFacilityAccessEntity DisabledTestFacilityAccess = new()
        {
            IsAuthorized = false,
            FacilityDisplayName = nameof(DisabledTestFacilityAccess),
            FacilityId = nameof(DisabledTestFacilityAccess),
            Level = FacilityAccessLevels.Admin,
        };

        public UserProfileFacilityAccessEntity TestFacilityAccess = new()
        {
            IsAuthorized = true,
            FacilityDisplayName = nameof(TestFacilityAccess),
            FacilityId = nameof(TestFacilityAccess),
            Level = FacilityAccessLevels.ReadOnly,
        };

        public readonly PermissionLookupEntity TestResourceDelete = new()
        {
            Code = TestResource,
            AssignedOperations = new() { new() { Value = "D" } },
        };

        public readonly PermissionLookupEntity TestResourceRead = new()
        {
            Code = TestResource,
            AssignedOperations = new() { new() { Value = "R" } },
        };

        public readonly PermissionLookupEntity TestResourceReadWrite = new()
        {
            Code = TestResource,
            AssignedOperations = new()
            {
                new() { Value = "R" },
                new() { Value = "W" },
                new() { Value = "D" },
            },
        };

        public DefaultScope()
        {
            InstanceUnderTest = new(UserProfileProviderMock.Object,
                                    RoleProviderMock.Object);

            RoleWithNoPermissions = new()
            {
                Id = Guid.NewGuid(),
                Name = nameof(RoleWithNoPermissions),
            };

            RoleWithReadWritePermissions = new()
            {
                Id = Guid.NewGuid(),
                Name = nameof(RoleWithReadWritePermissions),
                Permissions = new() { TestResourceReadWrite },
            };

            RoleWithReadPermissions = new()
            {
                Id = Guid.NewGuid(),
                Name = nameof(RoleWithReadPermissions),
                Permissions = new() { TestResourceRead },
            };

            RoleWithDeletePermissions = new()
            {
                Id = Guid.NewGuid(),
                Name = nameof(RoleWithDeletePermissions),
                Permissions = new() { TestResourceDelete },
            };

            DeletedRoleWithReadPermissions = new()
            {
                Id = Guid.NewGuid(),
                Name = nameof(DeletedRoleWithReadPermissions),
                Deleted = true,
                Permissions = new() { TestResourceRead },
            };

            TestRoles = new[] { RoleWithNoPermissions, RoleWithReadPermissions, RoleWithReadWritePermissions, DeletedRoleWithReadPermissions };
        }

        public Mock<IProvider<Guid, UserProfileEntity>> UserProfileProviderMock { get; } = new();

        public Mock<IProvider<Guid, RoleEntity>> RoleProviderMock { get; } = new();

        public RoleEntity RoleWithNoPermissions { get; }

        public RoleEntity RoleWithReadWritePermissions { get; }

        public RoleEntity RoleWithReadPermissions { get; }

        public RoleEntity RoleWithDeletePermissions { get; }

        public RoleEntity DeletedRoleWithReadPermissions { get; }

        public string EncodedEmptyPermissionsLookup => new Dictionary<string, string>().ToJson().ToBase64();

        public RoleEntity[] TestRoles { get; }

        public Dictionary<string, T> DecodeLookupClaim<T>(string encoded)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, T>>(encoded.FromBase64());
        }

        public Dictionary<string, string> DecodeLookupClaim(string encoded)
        {
            return DecodeLookupClaim<string>(encoded);
        }

        public UserProfileEntity GenerateUserProfileEntity(params RoleEntity[] assignedRoles)
        {
            return new()
            {
                Id = Guid.NewGuid(),
                ExternalAuthId = Guid.NewGuid().ToString(),
                Roles = assignedRoles.Select(role => new UserProfileRoleEntity
                {
                    RoleId = role.Id,
                    RoleName = role.Name,
                }).ToList(),
            };
        }

        public void SetExistingUserProfiles(params UserProfileEntity[] userProfiles)
        {
            ConfigureProviderMockGetValues(UserProfileProviderMock, userProfiles);
        }

        public void SetExistingRoles(params RoleEntity[] roles)
        {
            ConfigureProviderMockGetValues(RoleProviderMock, roles);
        }

        private void ConfigureProviderMockGetValues<TId, TEntity>(Mock<IProvider<TId, TEntity>> mock, TEntity[] entities) where TEntity : EntityBase<TId>
        {
            mock.Setup(x => x.Get(It.IsAny<Expression<Func<TEntity, bool>>>(),
                                  It.IsAny<Func<IQueryable<TEntity>,
                                      IOrderedQueryable<TEntity>>>(),
                                  It.IsAny<IEnumerable<string>>(),
                                  It.IsAny<bool>(),
                                  It.IsAny<bool>(),
                                  It.IsAny<bool>()))
                .ReturnsAsync((Expression<Func<TEntity, bool>> filter,
                               Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> _,
                               List<string> __,
                               bool ___,
                               bool ____,
                               bool _____) => entities.Where(filter.Compile()));

            mock.Setup(x => x.GetByIds(It.IsAny<IEnumerable<TId>>(),
                                       It.IsAny<bool>(),
                                       It.IsAny<bool>(),
                                       It.IsAny<bool>()))
                .ReturnsAsync((IEnumerable<TId> ids, bool _, bool __, bool ___) => entities.Where(entity => ids.Contains(entity.Id)));
        }
    }
}
