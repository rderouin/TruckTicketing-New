using System;

using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.Contracts.Security;

using Trident.Azure.Functions;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions;

[UseHttpFunction(HttpFunctionApiMethod.GetById,
                 Route = Routes.BillingConfiguration_IdRouteTemplate,
                 ClaimsAuthorizeResource = Permissions.Resources.Account,
                 ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Search,
                 Route = Routes.BillingConfiguration_SearchRoute,
                 ClaimsAuthorizeResource = Permissions.Resources.Account,
                 ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Create,
                 Route = Routes.BillingConfiguration_BaseRoute,
                 ClaimsAuthorizeResource = Permissions.Resources.Account,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Update,
                 Route = Routes.BillingConfiguration_IdRouteTemplate,
                 ClaimsAuthorizeResource = Permissions.Resources.Account,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Patch, Route = Routes.BillingConfiguration_IdRouteTemplate,
                 ClaimsAuthorizeResource = Permissions.Resources.Account,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
public partial class BillingConfigurationFunctions : HttpFunctionApiBase<BillingConfiguration, BillingConfigurationEntity, Guid>
{
    public BillingConfigurationFunctions(ILog log,
                                         IMapperRegistry mapper,
                                         IManager<Guid, BillingConfigurationEntity> manager)
        : base(log, mapper, manager)
    {
    }
}
