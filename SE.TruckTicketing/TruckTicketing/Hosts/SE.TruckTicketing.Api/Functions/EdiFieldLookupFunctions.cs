using System;

using SE.Shared.Domain.Entities.EDIFieldLookup;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Routes;

using Trident.Azure.Functions;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions;

[UseHttpFunction(HttpFunctionApiMethod.GetById, Route = Routes.EDIFieldLookup_IdRouteTemplate)]
[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.EDIFieldLookup_SearchRoute)]
[UseHttpFunction(HttpFunctionApiMethod.Create, Route = Routes.EDIFieldLookup_BaseRoute)]
[UseHttpFunction(HttpFunctionApiMethod.Update, Route = Routes.EDIFieldLookup_IdRouteTemplate)]
[UseHttpFunction(HttpFunctionApiMethod.Patch, Route = Routes.EDIFieldLookup_IdRouteTemplate)]
[UseHttpFunction(HttpFunctionApiMethod.Delete, Route = Routes.EDIFieldLookup_IdRouteTemplate)]
public partial class EDIFieldLookupFunctions : HttpFunctionApiBase<EDIFieldLookup, EDIFieldLookupEntity, Guid>
{
    public EDIFieldLookupFunctions(ILog log,
                                   IMapperRegistry mapper,
                                   IManager<Guid, EDIFieldLookupEntity> manager)
        : base(log, mapper, manager)
    {
    }
}
