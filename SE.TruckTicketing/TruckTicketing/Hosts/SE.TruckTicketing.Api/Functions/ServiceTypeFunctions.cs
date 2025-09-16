using System;

using SE.Shared.Domain.Entities.ServiceType;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.Contracts.Security;

using Trident.Azure.Functions;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions;

[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.ServiceType_SearchRoute, ClaimsAuthorizeResource = Permissions.Resources.ServiceType,
                 ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.GetById, Route = Routes.ServiceType_IdRouteTemplate, ClaimsAuthorizeResource = Permissions.Resources.ServiceType,
                 ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Create, Route = Routes.ServiceType_BaseRoute, ClaimsAuthorizeResource = Permissions.Resources.ServiceType,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Update, Route = Routes.ServiceType_IdRouteTemplate, ClaimsAuthorizeResource = Permissions.Resources.ServiceType,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Patch, Route = Routes.ServiceType_IdRouteTemplate, ClaimsAuthorizeResource = Permissions.Resources.ServiceType,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
public partial class ServiceTypeFunctions : HttpFunctionApiBase<ServiceType, ServiceTypeEntity, Guid>
{
    public ServiceTypeFunctions(ILog log,
                                IMapperRegistry mapper,
                                IManager<Guid, ServiceTypeEntity> manager)
        : base(log, mapper, manager)
    {
    }
}
