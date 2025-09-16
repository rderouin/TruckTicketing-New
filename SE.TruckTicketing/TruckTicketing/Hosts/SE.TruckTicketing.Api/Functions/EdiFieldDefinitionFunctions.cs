using System;

using SE.Shared.Domain.Entities.EDIFieldDefinition;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Routes;

using Trident.Azure.Functions;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions;

[UseHttpFunction(HttpFunctionApiMethod.GetById, Route = Routes.EDIFieldDefinition_IdRouteTemplate)]
[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.EDIFieldDefinition_SearchRoute)]
[UseHttpFunction(HttpFunctionApiMethod.Create, Route = Routes.EDIFieldDefinition_BaseRoute)]
[UseHttpFunction(HttpFunctionApiMethod.Update, Route = Routes.EDIFieldDefinition_IdRouteTemplate)]
[UseHttpFunction(HttpFunctionApiMethod.Patch, Route = Routes.EDIFieldDefinition_IdRouteTemplate)]
[UseHttpFunction(HttpFunctionApiMethod.Delete, Route = Routes.EDIFieldDefinition_IdRouteTemplate)]
public partial class EDIFieldDefinitionFunctions : HttpFunctionApiBase<EDIFieldDefinition, EDIFieldDefinitionEntity, Guid>
{
    public EDIFieldDefinitionFunctions(ILog log,
                                       IMapperRegistry mapper,
                                       IManager<Guid, EDIFieldDefinitionEntity> manager)
        : base(log, mapper, manager)
    {
    }
}
