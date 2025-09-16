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

[UseHttpFunction(HttpFunctionApiMethod.GetById, Route = Routes.FacilityService.Id)]
[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.FacilityService.Search)]
[UseHttpFunction(HttpFunctionApiMethod.Create, Route = Routes.FacilityService.Base)]
[UseHttpFunction(HttpFunctionApiMethod.Update, Route = Routes.FacilityService.Id)]
[UseHttpFunction(HttpFunctionApiMethod.Patch, Route = Routes.FacilityService.Id)]
public partial class FacilityServiceFunctions : HttpFunctionApiBase<FacilityService, FacilityServiceEntity, Guid>
{
    public FacilityServiceFunctions(ILog log,
                                    IMapperRegistry mapper,
                                    IManager<Guid, FacilityServiceEntity> manager)
        : base(log, mapper, manager)
    {
    }
}
