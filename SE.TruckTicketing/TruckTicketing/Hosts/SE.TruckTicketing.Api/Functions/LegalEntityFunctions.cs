using System;

using SE.Shared.Domain.LegalEntity;
using SE.TruckTicketing.Contracts.Models;
using SE.TruckTicketing.Contracts.Routes;

using Trident.Azure.Functions;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions;

[UseHttpFunction(HttpFunctionApiMethod.GetById, Route = Routes.LegalEntity_IdRoute)]
[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.LegalEntity_SearchRoute)]
public partial class LegalEntityFunctions : HttpFunctionApiBase<LegalEntity, LegalEntityEntity, Guid>
{
    public LegalEntityFunctions(ILog log, IMapperRegistry mapper, IManager<Guid, LegalEntityEntity> manager) : base(log, mapper, manager)
    {
    }
}
