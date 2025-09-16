using System;

using SE.TruckTicketing.Contracts.Api.Models.SpartanProductParameters;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.Contracts.Security;
using SE.TruckTicketing.Domain.Entities.SpartanProductParameters;

using Trident.Azure.Functions;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions.SpartanProductParameters;

[UseHttpFunction(HttpFunctionApiMethod.GetById, Route = Routes.SpartanProductParameter_IdRoute, ClaimsAuthorizeResource = Permissions.Resources.SpartanProductParameter,
                 ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.SpartanProductParameter_SearchRoute, ClaimsAuthorizeResource = Permissions.Resources.SpartanProductParameter,
                 ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Create, Route = Routes.SpartanProductParameter_BaseRoute, ClaimsAuthorizeResource = Permissions.Resources.SpartanProductParameter,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Update, Route = Routes.SpartanProductParameter_IdRoute, ClaimsAuthorizeResource = Permissions.Resources.SpartanProductParameter,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Patch, Route = Routes.SpartanProductParameter_IdRoute, ClaimsAuthorizeResource = Permissions.Resources.SpartanProductParameter,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Delete, Route = Routes.SpartanProductParameter_IdRoute, ClaimsAuthorizeResource = Permissions.Resources.SpartanProductParameter,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
public partial class SpartanProductParameterFunctions : HttpFunctionApiBase<SpartanProductParameter, SpartanProductParameterEntity, Guid>
{
    public SpartanProductParameterFunctions(ILog log, IMapperRegistry mapper, IManager<Guid, SpartanProductParameterEntity> manager) : base(log, mapper, manager)
    {
    }
}
