using System;
using System.Collections.Generic;

using Trident.Contracts.Api;

namespace SE.TruckTicketing.Contracts.Models.Accounts;

public class UserProfile : GuidApiModelBase
{
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string UserName { get; set; }

    public string GivenName { get; set; }

    public string Surname { get; set; }

    public string DisplayName { get; set; }

    public string Email { get; set; }

    public string LocalAuthId { get; set; }

    public string ExternalAuthId { get; set; }

    public bool EnforceSpecificFacilityAccessLevels { get; set; }

    public string AllFacilitiesAccessLevel { get; set; }

    public List<UserProfileFacilityAccess> SpecificFacilityAccessAssignments { get; set; } = new();

    public List<UserProfileRole> Roles { get; set; } = new();
}

public class UserProfileRole
{
    public Guid RoleId { get; set; }

    public string RoleName { get; set; }
}

public class UserProfileFacilityAccess : ApiModelBase<Guid>
{
    public bool IsAuthorized { get; set; }

    public string FacilityId { get; set; }

    public string FacilityDisplayName { get; set; }

    public string Level { get; set; }
}
