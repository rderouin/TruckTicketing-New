using System;

using SE.TruckTicketing.Contracts.Models.Sampling;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.Contracts.Security;
using SE.TruckTicketing.Domain.Entities.Sampling;

using Trident.Azure.Functions;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions.Sampling;

[UseHttpFunction(HttpFunctionApiMethod.Create, Route = Routes.LandfillSamplingRule.Base, ClaimsAuthorizeResource = Permissions.Resources.LandfillSampleRule, ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.LandfillSamplingRule.Search, ClaimsAuthorizeResource = Permissions.Resources.LandfillSampleRule, ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.GetById, Route = Routes.LandfillSamplingRule.Id, ClaimsAuthorizeResource = Permissions.Resources.LandfillSampleRule, ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Update, Route = Routes.LandfillSamplingRule.Id, ClaimsAuthorizeResource = Permissions.Resources.LandfillSampleRule, ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Delete, Route = Routes.LandfillSamplingRule.Id, ClaimsAuthorizeResource = Permissions.Resources.LandfillSampleRule, ClaimsAuthorizeOperation = Permissions.Operations.Write)]
public partial class LandfillSamplingRuleFunctions : HttpFunctionApiBase<LandfillSamplingRuleDto, LandfillSamplingRuleEntity, Guid>
{
    public LandfillSamplingRuleFunctions(
        ILog log,
        IMapperRegistry mapper,
        IManager<Guid, LandfillSamplingRuleEntity> manager)
        : base(log, mapper, manager)
    {
    }
}
