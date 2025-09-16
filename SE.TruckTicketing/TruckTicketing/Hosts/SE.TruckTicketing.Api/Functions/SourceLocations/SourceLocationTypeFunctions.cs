using System;
using SE.Shared.Domain.Entities.SourceLocationType;
using SE.TruckTicketing.Contracts.Models.SourceLocations;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.Contracts.Security;

using Trident.Azure.Functions;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions.SourceLocations;

[UseHttpFunction(HttpFunctionApiMethod.GetById, Route = Routes.SourceLocationType_IdRoute, ClaimsAuthorizeResource = Permissions.Resources.SourceLocationType, ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.SourceLocationType_SearchRoute, ClaimsAuthorizeResource = Permissions.Resources.SourceLocationType, ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Create, Route = Routes.SourceLocationType_BaseRoute, ClaimsAuthorizeResource = Permissions.Resources.SourceLocationType, ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Update, Route = Routes.SourceLocationType_IdRoute, ClaimsAuthorizeResource = Permissions.Resources.SourceLocationType, ClaimsAuthorizeOperation = Permissions.Operations.Write)]
public partial class SourceLocationTypeFunctions : HttpFunctionApiBase<SourceLocationType, SourceLocationTypeEntity, Guid>
{
    public SourceLocationTypeFunctions(ILog log, IMapperRegistry mapper, IManager<Guid, SourceLocationTypeEntity> manager) : base(log, mapper, manager)
    {
    }
}
