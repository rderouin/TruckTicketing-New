using System;

using SE.Shared.Domain.Entities.TicketType;
using SE.TruckTicketing.Contracts.Models;
using SE.TruckTicketing.Contracts.Routes;

using Trident.Azure.Functions;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions;

[UseHttpFunction(HttpFunctionApiMethod.GetById, Route = Routes.TicketType_IdRouteTemplate)]
[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.TicketType_SearchRoute)]
public partial class TicketTypeFunctions : HttpFunctionApiBase<TicketType, TicketTypeEntity, Guid>
{
    public TicketTypeFunctions(ILog log, IMapperRegistry mapper, IManager<Guid, TicketTypeEntity> manager) : base(log, mapper, manager)
    {
    }
}
