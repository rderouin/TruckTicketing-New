using System.ComponentModel;

namespace SE.Shared.Domain;

public enum ErrorCodes
{
    [Description("Display Name is required.")]
    UserProfile_DisplayNameRequired = 1000,

    [Description("Given Name is required.")]
    UserProfile_GivenNameRequired = 1010,

    [Description("Surname is required.")]
    UserProfile_SurnameRequired = 1020,

    [Description("Email is required.")]
    UserProfile_EmailRequired = 1030,

    [Description("Missing ExternalAuthId.")]
    UserProfile_ExternalAuthIdRequired = 1035,

    [Description("ExternalAuthId cannot be changed.")]
    UserProfile_ExternalAuthIdImmutable = 1036,

    [Description("ExternalAuthId must be unique.")]
    UserProfile_ExternalAuthIdUnique = 1037,

    [Description("User roles list must be defined.")]
    UserProfile_RolesRequired = 1040,

    [Description("User specific facility access list must be defined.")]
    UserProfile_SpecifcFacilityAccessListRequired = 1041,
}
