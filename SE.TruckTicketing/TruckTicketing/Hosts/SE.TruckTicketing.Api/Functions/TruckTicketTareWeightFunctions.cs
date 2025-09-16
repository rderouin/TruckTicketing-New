using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

using SE.Shared.Common;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.Contracts.Security;
using SE.TruckTicketing.Domain.Entities.TruckTicket;

using Trident.Azure.Functions;
using Trident.Azure.Security;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions;

[UseHttpFunction(HttpFunctionApiMethod.GetById, Route = Routes.TruckTicketTareWeight.Id,
                 ClaimsAuthorizeResource = Permissions.Resources.TruckTicketTareWeight,
                 ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.TruckTicketTareWeight.Search,
                 ClaimsAuthorizeResource = Permissions.Resources.TruckTicketTareWeight,
                 ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Delete, Route = Routes.TruckTicketTareWeight.Id,
                 ClaimsAuthorizeResource = Permissions.Resources.TruckTicketTareWeight,
                 ClaimsAuthorizeOperation = Permissions.Operations.Delete)]
[UseHttpFunction(HttpFunctionApiMethod.Update, Route = Routes.TruckTicketTareWeight.Id,
                 ClaimsAuthorizeResource = Permissions.Resources.TruckTicketTareWeight,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
public partial class TruckTicketTareWeightFunctions : HttpFunctionApiBase<TruckTicketTareWeight, TruckTicketTareWeightEntity, Guid>
{
    private readonly ILog _appLogger;

    private readonly ITruckTicketTareWeightManager _manager;

    private readonly IMapperRegistry _mapper;

    public TruckTicketTareWeightFunctions(ILog log, IMapperRegistry mapper, ITruckTicketTareWeightManager manager) :
        base(log, mapper, manager)
    {
        _mapper = mapper;
        _appLogger = log;
        _manager = manager;
    }

    [Function(nameof(UploadTruckTicketTareWeight))]
    [OpenApiOperation(nameof(UploadTruckTicketTareWeight), nameof(TruckTicketTareWeightFunctions), Summary = nameof(Routes.TruckTicketTareWeight))]
    [OpenApiRequestBody(ContentTypes.JSON, typeof(IEnumerable<TruckTicketTareWeightCsvResult>))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ContentTypes.JSON, typeof(IEnumerable<TruckTicketTareWeightCsvResult>))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.TruckTicketTareWeight, Permissions.Operations.Write)]
    public async Task<HttpResponseData> UploadTruckTicketTareWeight([HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.TruckTicketTareWeight.Base)] HttpRequestData req)
    {
        return await HandleRequest(req,
                                   nameof(UploadTruckTicketTareWeight),
                                   async response =>
                                   {
                                       var request = await req.ReadFromJsonAsync<IEnumerable<TruckTicketTareWeightCsvResult>>();

                                       var unProcessedList =
                                           await _manager.TruckTicketTareWeightCsvProcessing(_mapper.Map<IEnumerable<TruckTicketTareWeightCsvResponse>>(request));

                                       await response.WriteAsJsonAsync(_mapper.Map<List<TruckTicketTareWeightCsvResult>>(unProcessedList));
                                   });
    }
}
