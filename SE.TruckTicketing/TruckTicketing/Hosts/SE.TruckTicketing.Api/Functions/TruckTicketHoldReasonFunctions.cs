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

[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.TruckTicketHoldReason_SearchRoute)]
public partial class TruckTicketHoldReasonFunctions : HttpFunctionApiBase<TruckTicketHoldReason, TruckTicketHoldReasonEntity, Guid>
{
    public TruckTicketHoldReasonFunctions(ILog log,
                                          IMapperRegistry mapper,
                                          IManager<Guid, TruckTicketHoldReasonEntity> manager)
        : base(log, mapper, manager)
    {
    }
}
