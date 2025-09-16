using System;
using System.Net;
using System.Threading.Tasks;
using System.Net.Http;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

using SE.Shared.Common;
using SE.TruckTicketing.Contracts.Models.Sampling;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.Contracts.Security;
using SE.TruckTicketing.Domain.Entities.Sampling;

using Trident.Azure.Functions;
using Trident.Azure.Security;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions.Sampling;

[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.LandfillSampling.Search, ClaimsAuthorizeResource = Permissions.Resources.TruckTicket, ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.GetById, Route = Routes.LandfillSampling.Id, ClaimsAuthorizeResource = Permissions.Resources.TruckTicket, ClaimsAuthorizeOperation = Permissions.Operations.Read)]
public partial class LandfillSamplingFunctions : HttpFunctionApiBase<LandfillSamplingDto, LandfillSamplingEntity, Guid>
{
    private readonly ILandfillSamplingStatusCheckManager _landfillSamplingStatusCheckManager;
    
    public LandfillSamplingFunctions(
        ILog log,
        IMapperRegistry mapper,
        IManager<Guid, LandfillSamplingEntity> manager,
        ILandfillSamplingStatusCheckManager landfillSamplingStatusCheckManager)
        : base(log, mapper, manager)
    {
        _landfillSamplingStatusCheckManager = landfillSamplingStatusCheckManager;
    }
    
    [Function(nameof(LandfillSamplingCheckStatus))]
    [OpenApiOperation(nameof(LandfillSamplingCheckStatus), new[] { nameof(LandfillSamplingFunctions) })]
    [OpenApiRequestBody(ContentTypes.JSON, typeof(LandfillSamplingStatusCheckRequestDto))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ContentTypes.JSON, typeof(LandfillSamplingStatusCheckDto))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.TruckTicket, Permissions.Operations.Write)]
    public async Task<HttpResponseData> LandfillSamplingCheckStatus([HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.LandfillSampling.CheckStatus)] HttpRequestData req)
    {
        return await HandleRequest(req,
                                   nameof(LandfillSamplingCheckStatus),
                                   async response =>
                                   {
                                       var request = await req.ReadFromJsonAsync<LandfillSamplingStatusCheckRequestDto>();
                                       var resultDto = await _landfillSamplingStatusCheckManager.GetSamplingStatus(request);

                                       await response.WriteAsJsonAsync(resultDto);
                                   });
    }
}
