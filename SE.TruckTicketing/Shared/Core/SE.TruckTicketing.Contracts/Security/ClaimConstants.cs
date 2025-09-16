namespace SE.TruckTicketing.Contracts.Security;

public static class ClaimConstants
{
    // Summary: Name claim: "name".
    public const string Name = "name";

    // Summary: Old Object Id claim: http://schemas.microsoft.com/identity/claims/objectidentifier.
    public const string ObjectId = "http://schemas.microsoft.com/identity/claims/objectidentifier";

    // Summary: New Object id claim: "oid".
    public const string Oid = "oid";

    // Summary:
    // PreferredUserName: "preferred_username".
    public const string PreferredUserName = "preferred_username";

    // Summary: Old TenantId claim: "http://schemas.microsoft.com/identity/claims/tenantid".
    public const string TenantId = "http://schemas.microsoft.com/identity/claims/tenantid";

    // Summary: New Tenant Id claim: "tid".
    public const string Tid = "tid";

    // Summary: ClientInfo claim: "client_info".
    public const string ClientInfo = "client_info";

    // Summary:
    // UniqueObjectIdentifier: "uid". Home Object Id.
    public const string UniqueObjectIdentifier = "uid";

    // Summary:
    // UniqueTenantIdentifier: "utid". Home Tenant Id.
    public const string UniqueTenantIdentifier = "utid";

    // Summary: Older scope claim: "http://schemas.microsoft.com/identity/claims/scope".
    public const string Scope = "http://schemas.microsoft.com/identity/claims/scope";

    // Summary: Newer scope claim: "scp".
    public const string Scp = "scp";

    // Summary: New Roles claim = "roles".
    public const string Roles = "roles";

    // Summary: Old Role claim: "http://schemas.microsoft.com/ws/2008/06/identity/claims/role".
    public const string Role = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

    // Summary: Subject claim: "sub".
    public const string Sub = "sub";

    // Summary: Acr claim: "acr".
    public const string Acr = "acr";

    // Summary: UserFlow claim: "http://schemas.microsoft.com/claims/authnclassreference".
    public const string UserFlow = "http://schemas.microsoft.com/claims/authnclassreference";

    // Summary: Tfp claim: "tfp".
    public const string Tfp = "tfp";

    // Summary: Name Identifier ID claim: "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier".
    public const string NameIdentifierId = "name";

    public const string Permissions = "permissions";

    public const string Emails = "emails";
}
