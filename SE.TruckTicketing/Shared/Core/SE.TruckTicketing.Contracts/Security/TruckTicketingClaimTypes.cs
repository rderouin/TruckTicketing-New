namespace SE.TruckTicketing.Contracts.Security;

public static class TruckTicketingClaimTypes
{
    internal const string ExtensionsPrefix = "extension_TT";

    public const string Roles = ExtensionsPrefix + nameof(Roles);

    public const string Permissions = ExtensionsPrefix + nameof(Permissions);

    public const string FacilityAccess = ExtensionsPrefix + nameof(FacilityAccess);
}
