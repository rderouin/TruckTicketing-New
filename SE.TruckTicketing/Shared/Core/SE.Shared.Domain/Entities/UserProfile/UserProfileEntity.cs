using System;
using System.Collections.Generic;

using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Security;

using Trident.Data;
using Trident.Domain;
using Trident.SourceGeneration.Attributes;

namespace SE.Shared.Domain.Entities.UserProfile;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Accounts, nameof(DocumentType), Containers.Partitions.Accounts, PartitionKeyType.WellKnown)]
[Discriminator(nameof(EntityType), Databases.Discriminators.UserProfile)]
[GenerateProvider]
public class UserProfileEntity : TTAuditableEntityBase
{
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string GivenName { get; set; }

    public string Surname { get; set; }

    public string DisplayName { get; set; }

    public string Email { get; set; }

    public string LocalAuthId { get; set; }

    public string ExternalAuthId { get; set; }

    public bool EnforceSpecificFacilityAccessLevels { get; set; }

    public string AllFacilitiesAccessLevel { get; set; } = FacilityAccessLevels.InheritedRoles;

    public List<UserProfileFacilityAccessEntity> SpecificFacilityAccessAssignments { get; set; } = new();

    public List<UserProfileRoleEntity> Roles { get; set; } = new();
}

public class UserProfileRoleEntity : OwnedLookupEntityBase<Guid>
{
    public Guid RoleId { get; set; }

    public string RoleName { get; set; }
}

public class UserProfileFacilityAccessEntity : OwnedLookupEntityBase<Guid>
{
    public bool IsAuthorized { get; set; }

    public string FacilityId { get; set; }

    public string FacilityDisplayName { get; set; }

    public string Level { get; set; }
}
