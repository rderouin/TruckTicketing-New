using System;

using SE.Shared.Domain.Entities.Substance;
using SE.TruckTicketing.Contracts.Models.Substances;
using SE.TruckTicketing.Contracts.Routes;

using Trident.Azure.Functions;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions;

[UseHttpFunction(HttpFunctionApiMethod.GetById, Route = Routes.Substance_IdRouteTemplate)]
[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.Substance_SearchRoute)]
public partial class SubstanceFunctions : HttpFunctionApiBase<Substance, SubstanceEntity, Guid>
{
    public SubstanceFunctions(ILog log,
                              IMapperRegistry mapper,
                              IManager<Guid, SubstanceEntity> manager)
        : base(log, mapper, manager)
    {
    }
}
