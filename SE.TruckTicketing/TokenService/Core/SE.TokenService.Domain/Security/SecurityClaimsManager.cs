using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.Role;
using SE.Shared.Domain.Entities.UserProfile;
using SE.TruckTicketing.Contracts.Security;

using Trident.Data.Contracts;
using Trident.Extensions;

namespace SE.TokenService.Domain.Security;

public class SecurityClaimsManager : ISecurityClaimsManager
{
    private readonly IProvider<Guid, RoleEntity> _roleProvider;

    private readonly IProvider<Guid, UserProfileEntity> _userProfileProvider;

    public SecurityClaimsManager(IProvider<Guid, UserProfileEntity> userProfileProvider,
                                 IProvider<Guid, RoleEntity> roleProvider)
    {
        _userProfileProvider = userProfileProvider;
        _roleProvider = roleProvider;
    }

    public async Task<SecurityClaimsBag> GetUserSecurityClaimsByExternalAuthId(string externalAuthId)
    {
        if (string.IsNullOrEmpty(externalAuthId))
        {
            return null;
        }

        var userProfileEntity = (await _userProfileProvider.Get(userProfile => userProfile.ExternalAuthId == externalAuthId)).FirstOrDefault();

        if (userProfileEntity == null)
        {
            return null;
        }

        return await GetUserSecurityClaims(userProfileEntity);
    }

    private async Task<SecurityClaimsBag> GetUserSecurityClaims(UserProfileEntity userProfileEntity)
    {
        var assignedRoles = (await _roleProvider.GetByIds(userProfileEntity.Roles.Select(role => role.RoleId)))
                           .Where(assignedRoles => !assignedRoles.Deleted)
                           .ToArray();

        var assignedPermissions = assignedRoles.SelectMany(role => role.Permissions)
                                               .Select(permission => (resource: permission.Code, operations: permission.AssignedOperations))
                                               .GroupBy(permission => permission.resource)
                                               .ToDictionary(group => group.Key,
                                                             group => string.Concat(group.SelectMany(permission => permission.operations).Select(operation => operation.Value).Distinct()));

        var assignedFacilities = GetFacilityAccessPermisionScopes(userProfileEntity)
           .ToDictionary(access => access.FacilityId, access => access.Level);

        return new()
        {
            Roles = string.Join(", ", assignedRoles.Select(role => role.Name)),
            Permissions = assignedPermissions.ToJson().ToBase64(),
            FacilityAccess = assignedFacilities.ToJson().ToBase64(),
        };
    }

    private IEnumerable<UserProfileFacilityAccessEntity> GetFacilityAccessPermisionScopes(UserProfileEntity userProfileEntity)
    {
        if (userProfileEntity.EnforceSpecificFacilityAccessLevels)
        {
            foreach (var scope in userProfileEntity.SpecificFacilityAccessAssignments)
            {
                if (scope.IsAuthorized)
                {
                    yield return scope;
                }
            }
        }
        else
        {
            yield return new()
            {
                FacilityId = FacilityAccessConstants.AllFacilityAccessFacilityId,
                Level = userProfileEntity.AllFacilitiesAccessLevel,
            };
        }
    }
}
