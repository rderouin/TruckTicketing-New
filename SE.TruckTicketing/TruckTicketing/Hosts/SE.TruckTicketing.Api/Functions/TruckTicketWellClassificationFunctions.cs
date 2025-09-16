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

[UseHttpFunction(HttpFunctionApiMethod.GetById, Route = Routes.TruckTicketWellClassification.Id)]
[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.TruckTicketWellClassification.Search)]
[UseHttpFunction(HttpFunctionApiMethod.Delete, Route = Routes.TruckTicketWellClassification.Id)]
public partial class TruckTicketWellClassificationFunctions : HttpFunctionApiBase<TruckTicketWellClassification, TruckTicketWellClassificationUsageEntity, Guid>
{
    public TruckTicketWellClassificationFunctions(ILog log, IMapperRegistry mapper, IManager<Guid, TruckTicketWellClassificationUsageEntity> manager) : base(log, mapper, manager)
    {
    }
}
