using System;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

using SE.Shared.Domain.Entities.SourceLocation;
using SE.TruckTicketing.Contracts.Models.SourceLocations;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.Contracts.Security;
using SE.TruckTicketing.Domain.Entities.SourceLocation;

using Trident.Azure.Functions;
using Trident.Azure.Security;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions.SourceLocations;

[UseHttpFunction(HttpFunctionApiMethod.GetById, Route = Routes.SourceLocation_IdRoute, 
                 ClaimsAuthorizeResource = Permissions.Resources.SourceLocation,
                 ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.SourceLocation_SearchRoute,
                 ClaimsAuthorizeResource = Permissions.Resources.SourceLocation,
                 ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Create, Route = Routes.SourceLocation_BaseRoute,
                 ClaimsAuthorizeResource = Permissions.Resources.SourceLocation,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Update, Route = Routes.SourceLocation_IdRoute,
                 ClaimsAuthorizeResource = Permissions.Resources.SourceLocation,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Patch, Route = Routes.SourceLocation_IdRoute,
                 ClaimsAuthorizeResource = Permissions.Resources.SourceLocation,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
public partial class SourceLocationFunctions : HttpFunctionApiBase<SourceLocation, SourceLocationEntity, Guid>
{
    private readonly ISourceLocationManager _sourceLocationManager;

    public SourceLocationFunctions(ILog log, IMapperRegistry mapper, ISourceLocationManager manager) :
        base(log, mapper, manager)
    {
        _sourceLocationManager = manager;
    }

    [Function(nameof(MarkSourceLocationDeleted))]
    [OpenApiOperation(nameof(MarkSourceLocationDeleted), nameof(SourceLocationFunctions), Summary = Routes.SourceLocation_MarkDelete)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(SourceLocation))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.SourceLocation, Permissions.Operations.Write)]
    public async Task<HttpResponseData> MarkSourceLocationDeleted(
        [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Patch), Route = Routes.SourceLocation_MarkDelete)] HttpRequestData request,
        Guid id)
    {
        return await HandleRequest(request,
                                   nameof(MarkSourceLocationDeleted),
                                   async response =>
                                   {
                                       var isDeletedMarked = await _sourceLocationManager.MarkSourceLocationDelete(sourceLocationId: id);
                                       await response.WriteAsJsonAsync(isDeletedMarked);
                                   });
    }
}
