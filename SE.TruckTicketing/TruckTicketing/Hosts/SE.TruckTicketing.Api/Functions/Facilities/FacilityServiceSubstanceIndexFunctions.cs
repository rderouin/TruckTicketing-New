using System;

using SE.TruckTicketing.Contracts.Models.FacilityServices;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.Domain.Entities.FacilityService;

using Trident.Azure.Functions;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions.Facilities;

[UseHttpFunction(HttpFunctionApiMethod.GetById, Route = Routes.FacilityServiceSubstanceIndex.Id)]
[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.FacilityServiceSubstanceIndex.Search)]
public partial class FacilityServiceSubstanceIndexFunctions : HttpFunctionApiBase<FacilityServiceSubstanceIndex, FacilityServiceSubstanceIndexEntity, Guid>
{
    public FacilityServiceSubstanceIndexFunctions(ILog log,
                                                  IMapperRegistry mapper,
                                                  IManager<Guid, FacilityServiceSubstanceIndexEntity> manager)
        : base(log, mapper, manager)
    {
    }
}
