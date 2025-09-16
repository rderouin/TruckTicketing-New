using System;

using SE.Shared.Domain.Entities.Facilities;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.Contracts.Security;

using Trident.Azure.Functions;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions.Facilities;

[UseHttpFunction(HttpFunctionApiMethod.Create, Route = Routes.Facility_BaseRoute, ClaimsAuthorizeResource = Permissions.Resources.Facility, ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.Facility_SearchRoute, ClaimsAuthorizeResource = Permissions.Resources.Facility, ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.GetById, Route = Routes.Facility_IdRouteTemplate, ClaimsAuthorizeResource = Permissions.Resources.Facility, ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Update, Route = Routes.Facility_IdRouteTemplate, ClaimsAuthorizeResource = Permissions.Resources.Facility, ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Patch, Route = Routes.Facility_IdRouteTemplate, ClaimsAuthorizeResource = Permissions.Resources.Facility, ClaimsAuthorizeOperation = Permissions.Operations.Write)]
public partial class FacilityFunctions : HttpFunctionApiBase<Facility, FacilityEntity, Guid>
{
    public FacilityFunctions(ILog log,
                             IMapperRegistry mapper,
                             IManager<Guid, FacilityEntity> manager)
        : base(log, mapper, manager)
    {
    }
}
