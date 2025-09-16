using System;

using SE.Shared.Domain.Entities.VolumeChange;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.Contracts.Security;

using Trident.Azure.Functions;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions;

[UseHttpFunction(HttpFunctionApiMethod.Create, Route = Routes.VolumeChange.Base, ClaimsAuthorizeResource = Permissions.Resources.TruckTicket, ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.VolumeChange.Search, ClaimsAuthorizeResource = Permissions.Resources.VolumeChangeReport, ClaimsAuthorizeOperation = Permissions.Operations.View)]
[UseHttpFunction(HttpFunctionApiMethod.GetById, Route = Routes.VolumeChange.Id, ClaimsAuthorizeResource = Permissions.Resources.VolumeChangeReport, ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Update, Route = Routes.VolumeChange.Id, ClaimsAuthorizeResource = Permissions.Resources.TruckTicket, ClaimsAuthorizeOperation = Permissions.Operations.Write)]
public partial class VolumeChangeFunctions : HttpFunctionApiBase<VolumeChange, VolumeChangeEntity, Guid>
{
    public VolumeChangeFunctions(ILog log,
                                 IMapperRegistry mapper,
                                 IManager<Guid, VolumeChangeEntity> manager)
        : base(log, mapper, manager)
    {
    }
}
