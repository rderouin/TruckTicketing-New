using System;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.Domain.Entities.TruckTicket;

using Trident.Azure.Functions;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions;

[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.TruckTicketVoidReason_SearchRoute)]
public partial class TruckTicketVoidReasonFunctions : HttpFunctionApiBase<TruckTicketVoidReason, TruckTicketVoidReasonEntity, Guid>
{
    public TruckTicketVoidReasonFunctions(ILog log,
                                          IMapperRegistry mapper,
                                          IManager<Guid, TruckTicketVoidReasonEntity> manager)
        : base(log, mapper, manager)
    {
    }
}
