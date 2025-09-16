using System;

using SE.Shared.Domain.Entities.EDIValidationPatternLookup;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Routes;

using Trident.Azure.Functions;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions;

[UseHttpFunction(HttpFunctionApiMethod.GetById, Route = Routes.EDIValidationPatternLookup_IdRouteTemplate)]
[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.EDIValidationPatternLookup_SearchRoute)]
[UseHttpFunction(HttpFunctionApiMethod.Create, Route = Routes.EDIValidationPatternLookup_BaseRoute)]
[UseHttpFunction(HttpFunctionApiMethod.Update, Route = Routes.EDIValidationPatternLookup_IdRouteTemplate)]
[UseHttpFunction(HttpFunctionApiMethod.Patch, Route = Routes.EDIValidationPatternLookup_IdRouteTemplate)]
[UseHttpFunction(HttpFunctionApiMethod.Delete, Route = Routes.EDIValidationPatternLookup_IdRouteTemplate)]
public partial class EDIValidationPatternLookupFunctions : HttpFunctionApiBase<EDIValidationPatternLookup, EDIValidationPatternLookupEntity, Guid>
{
    public EDIValidationPatternLookupFunctions(ILog log,
                                               IMapperRegistry mapper,
                                               IManager<Guid, EDIValidationPatternLookupEntity> manager)
        : base(log, mapper, manager)
    {
    }
}
