using System;

using SE.Shared.Domain.Entities.Acknowledgement;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.Contracts.Security;

using Trident.Azure.Functions;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions.Acknowledgement;

[UseHttpFunction(HttpFunctionApiMethod.GetById,
                 Route = Routes.Acknowledgement.Id,
                 ClaimsAuthorizeResource = Permissions.Resources.Acknowledgement,
                 ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Search,
                 Route = Routes.Acknowledgement.Search,
                 ClaimsAuthorizeResource = Permissions.Resources.Acknowledgement,
                 ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Create,
                 Route = Routes.Acknowledgement.Base,
                 ClaimsAuthorizeResource = Permissions.Resources.Acknowledgement,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Update,
                 Route = Routes.Acknowledgement.Id,
                 ClaimsAuthorizeResource = Permissions.Resources.Acknowledgement,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Patch, Route = Routes.Acknowledgement.Id,
                 ClaimsAuthorizeResource = Permissions.Resources.Acknowledgement,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
public partial class AcknowledgementFunctions : HttpFunctionApiBase<Contracts.Models.Acknowledgement.Acknowledgement, AcknowledgementEntity, Guid>
{
    public AcknowledgementFunctions(ILog log,
                                    IMapperRegistry mapper,
                                    IManager<Guid, AcknowledgementEntity> manager)
        : base(log, mapper, manager)
    {
    }
}
