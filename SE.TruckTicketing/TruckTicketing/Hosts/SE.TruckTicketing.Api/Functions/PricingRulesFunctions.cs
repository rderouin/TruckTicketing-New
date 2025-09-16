using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

using SE.Shared.Common;
using SE.Shared.Domain.PricingRules;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Routes;

using Trident.Azure.Functions;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions;

[UseHttpFunction(HttpFunctionApiMethod.GetById, Route = Routes.PricingSelection_IdRouteTemplate)]
[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.PricingSelection_SearchRoute)]
public partial class PricingRulesFunctions : HttpFunctionApiBase<PricingRule, PricingRuleEntity, Guid>
{
    private readonly IPricingRuleManager _pricingRuleManager;

    public PricingRulesFunctions(ILog log, IMapperRegistry mapper, IManager<Guid, PricingRuleEntity> manager, IPricingRuleManager pricingRuleManager) : base(log, mapper, manager)
    {
        _pricingRuleManager = pricingRuleManager;
    }

    [Function(nameof(PricingSelection))]
    [OpenApiOperation(nameof(PricingSelection), nameof(PricingRulesFunctions), Summary = nameof(Routes.PricingSelection_ComputeRoute))]
    [OpenApiRequestBody("application/json", typeof(ComputePricingRequest))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ContentTypes.JSON, typeof(ComputePricingResponse))]
    [OpenApiResponseWithoutBody(HttpStatusCode.NotFound)]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    public async Task<HttpResponseData> PricingSelection([HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.PricingSelection_ComputeRoute)] HttpRequestData req)
    {
        return await HandleRequest(req,
                                   nameof(ComputePricingRequest),
                                   async response =>
                                   {
                                       var request = await req.ReadFromJsonAsync<ComputePricingRequest>();
                                       var result = await _pricingRuleManager.ComputePrice(request);
                                       await response.WriteAsJsonAsync(result);
                                   });
    }
}
