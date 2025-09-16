using System;

using SE.BillingService.Contracts.Api.Models;
using SE.BillingService.Domain.Entities.InvoiceExchange;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.Contracts.Security;

using Trident.Azure.Functions;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.BillingService.Api.Functions.InvoiceExchange;

[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.InvoiceExchange_SearchRoute, ClaimsAuthorizeResource = Permissions.Resources.InvoiceExchangeConfiguration, ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.GetById, Route = Routes.InvoiceExchange_IdRoute, ClaimsAuthorizeResource = Permissions.Resources.InvoiceExchangeConfiguration, ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Create, Route = Routes.InvoiceExchange_BaseRoute, ClaimsAuthorizeResource = Permissions.Resources.InvoiceExchangeConfiguration, ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Update, Route = Routes.InvoiceExchange_IdRoute, ClaimsAuthorizeResource = Permissions.Resources.InvoiceExchangeConfiguration, ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Patch, Route = Routes.InvoiceExchange_IdRoute, ClaimsAuthorizeResource = Permissions.Resources.InvoiceExchangeConfiguration, ClaimsAuthorizeOperation = Permissions.Operations.Write)]
public partial class InvoiceExchangeFunctions : HttpFunctionApiBase<InvoiceExchangeDto, InvoiceExchangeEntity, Guid>
{
    public InvoiceExchangeFunctions(ILog log, IMapperRegistry mapper, IManager<Guid, InvoiceExchangeEntity> manager) : base(log, mapper, manager)
    {
    }
}
