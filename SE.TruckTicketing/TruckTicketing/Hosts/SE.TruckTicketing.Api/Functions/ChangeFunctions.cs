using SE.Shared.Domain.Entities.Changes;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Routes;

using Trident.Azure.Functions;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions;

[UseHttpFunction(HttpFunctionApiMethod.GetById,
                 Route = Routes.Change_IdRouteTemplate)]
[UseHttpFunction(HttpFunctionApiMethod.Search,
                 Route = Routes.Change_SearchRoute)]
public partial class ChangeFunctions : HttpFunctionApiBase<Change, ChangeEntity, string>
{
    public ChangeFunctions(ILog log,
                           IMapperRegistry mapper,
                           IManager<string, ChangeEntity> manager)
        : base(log, mapper, manager)
    {
    }
}
