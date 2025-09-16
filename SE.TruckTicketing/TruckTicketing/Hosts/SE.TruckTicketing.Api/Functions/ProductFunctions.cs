using System;

using SE.Shared.Domain.Product;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Routes;

using Trident.Azure.Functions;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions;

[UseHttpFunction(HttpFunctionApiMethod.GetById, Route = Routes.Product_IdRouteTemplate)]
[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.Product_SearchRoute)]
[UseHttpFunction(HttpFunctionApiMethod.Create, Route = Routes.Product_BaseRoute)]
[UseHttpFunction(HttpFunctionApiMethod.Update, Route = Routes.Product_IdRouteTemplate)]
[UseHttpFunction(HttpFunctionApiMethod.Patch, Route = Routes.Product_IdRouteTemplate)]
[UseHttpFunction(HttpFunctionApiMethod.Delete, Route = Routes.Product_IdRouteTemplate)]
public partial class ProductFunctions : HttpFunctionApiBase<Product, ProductEntity, Guid>
{
    public ProductFunctions(ILog log,
                            IMapperRegistry mapper,
                            IManager<Guid, ProductEntity> manager)
        : base(log, mapper, manager)
    {
    }
}
