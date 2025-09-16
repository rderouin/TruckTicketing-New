using System;

using SE.Shared.Domain.BusinessStream;
using SE.TruckTicketing.Contracts.Api.Models;
using SE.TruckTicketing.Contracts.Routes;

using Trident.Azure.Functions;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions;

[UseHttpFunction(HttpFunctionApiMethod.GetById, Route = Routes.BusinessStream_IdRoute)]
[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.BusinessStream_SearchRoute)]
public partial class BusinessStreamFunctions : HttpFunctionApiBase<BusinessStream, BusinessStreamEntity, Guid>
{
    public BusinessStreamFunctions(ILog log, IMapperRegistry mapper, IManager<Guid, BusinessStreamEntity> manager) : base(log, mapper, manager)
    {
    }
}
