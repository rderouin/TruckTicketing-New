using System;

using SE.Shared.Domain.Entities.AdditionalServicesConfiguration;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.Contracts.Security;

using Trident.Azure.Functions;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions;

[UseHttpFunction(HttpFunctionApiMethod.Create,
                 Route = Routes.AdditionalServicesConfiguration_BaseRoute,
                 ClaimsAuthorizeResource = Permissions.Resources.AdditionalServicesConfiguration,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Search,
                 Route = Routes.AdditionalServicesConfiguration_SearchRoute,
                 ClaimsAuthorizeResource = Permissions.Resources.AdditionalServicesConfiguration,
                 ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.GetById,
                 Route = Routes.AdditionalServicesConfiguration_IdRouteTemplate,
                 ClaimsAuthorizeResource = Permissions.Resources.AdditionalServicesConfiguration,
                 ClaimsAuthorizeOperation = Permissions.Operations.Read,
                 AuthorizeFacilityAccessWith = typeof(AdditionalServicesConfiguration))]
[UseHttpFunction(HttpFunctionApiMethod.Update,
                 Route = Routes.AdditionalServicesConfiguration_IdRouteTemplate,
                 ClaimsAuthorizeResource = Permissions.Resources.AdditionalServicesConfiguration,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write,
                 AuthorizeFacilityAccessWith = typeof(AdditionalServicesConfiguration))]
[UseHttpFunction(HttpFunctionApiMethod.Patch,
                 Route = Routes.AdditionalServicesConfiguration_IdRouteTemplate,
                 ClaimsAuthorizeResource = Permissions.Resources.AdditionalServicesConfiguration,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
public partial class AdditionalServicesConfigurationFunctions : HttpFunctionApiBase<AdditionalServicesConfiguration, AdditionalServicesConfigurationEntity, Guid>
{
    public AdditionalServicesConfigurationFunctions(ILog log,
                                                    IMapperRegistry mapper,
                                                    IManager<Guid, AdditionalServicesConfigurationEntity> manager)
        : base(log, mapper, manager)
    {
    }
}
