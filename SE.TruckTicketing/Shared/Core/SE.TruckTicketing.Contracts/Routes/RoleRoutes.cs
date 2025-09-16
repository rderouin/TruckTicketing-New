namespace SE.TruckTicketing.Contracts.Routes;

public static class RoleRoutes
{
    public const string SearchMethod = "post";

    public const string Search = "roles/search";

    public const string RolesMethod = "get";

    public const string Roles = "roles/list";

    public const string RoleByIdMethod = "get";

    public const string RoleById = "roles/{id}";

    public const string UpdateMethod = "put";

    public const string Update = "roles/{id}";

    public const string CreateMethod = "post";

    public const string Create = "roles";

    public const string DeleteMethod = "delete";

    public const string Delete = "roles/{id}";
}
