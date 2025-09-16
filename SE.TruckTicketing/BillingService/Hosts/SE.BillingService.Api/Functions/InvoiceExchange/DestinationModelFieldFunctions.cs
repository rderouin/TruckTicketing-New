using System;

using SE.BillingService.Contracts.Api.Models;
using SE.BillingService.Domain.Entities.InvoiceExchange;
using SE.TruckTicketing.Contracts.Routes;

using Trident.Azure.Functions;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.BillingService.Api.Functions.InvoiceExchange;

[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.InvoiceExchange_DestinationFields_SearchRoute)]
public partial class DestinationModelFieldFunctions : HttpFunctionApiBase<DestinationFieldDto, DestinationModelFieldEntity, Guid>
{
    public DestinationModelFieldFunctions(ILog log, IMapperRegistry mapper, IManager<Guid, DestinationModelFieldEntity> manager) : base(log, mapper, manager)
    {
    }
}
