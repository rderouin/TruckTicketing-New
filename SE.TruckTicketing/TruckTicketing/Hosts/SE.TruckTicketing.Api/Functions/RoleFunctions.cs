using System;

using SE.Shared.Domain.Entities.Role;
using SE.TruckTicketing.Contracts.Models.Accounts;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.Contracts.Security;

using Trident.Azure.Functions;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions;

[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.Role_SearchRoute, ClaimsAuthorizeResource = Permissions.Resources.Roles, ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.GetById, Route = Routes.Role_IdRouteTemplate, ClaimsAuthorizeResource = Permissions.Resources.Roles, ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Create, Route = Routes.Role_BaseRoute, ClaimsAuthorizeResource = Permissions.Resources.Roles, ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Update, Route = Routes.Role_IdRouteTemplate, ClaimsAuthorizeResource = Permissions.Resources.Roles, ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Patch, Route = Routes.Role_IdRouteTemplate, ClaimsAuthorizeResource = Permissions.Resources.Roles, ClaimsAuthorizeOperation = Permissions.Operations.Write)]
public partial class RoleFunctions : HttpFunctionApiBase<Role, RoleEntity, Guid>
{
    public RoleFunctions(ILog log,
                         IMapperRegistry mapper,
                         IManager<Guid, RoleEntity> manager)
        : base(log, mapper, manager)
    {
    }
}
