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

[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.InvoiceExchange_ValueFormats_SearchRoute)]
public partial class ValueFormatFunctions : HttpFunctionApiBase<ValueFormatDto, ValueFormatEntity, Guid>
{
    public ValueFormatFunctions(ILog log, IMapperRegistry mapper, IManager<Guid, ValueFormatEntity> manager) : base(log, mapper, manager)
    {
    }
}
