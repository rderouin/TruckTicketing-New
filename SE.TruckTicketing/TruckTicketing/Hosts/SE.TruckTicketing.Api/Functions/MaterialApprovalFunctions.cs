using System;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;

using SE.Shared.Common.Constants;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.MaterialApproval;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.Contracts.Security;
using SE.TruckTicketing.Domain.Entities.TruckTicket.LocalReporting;

using Trident.Azure.Functions;
using Trident.Azure.Security;
using Trident.Contracts;
using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions;

[UseHttpFunction(HttpFunctionApiMethod.GetById, Route = Routes.MaterialApproval_IdRoute,
                 ClaimsAuthorizeResource = Permissions.Resources.MaterialApproval,
                 ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.MaterialApproval_SearchRoute,
                 ClaimsAuthorizeResource = Permissions.Resources.MaterialApproval,
                 ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Create, Route = Routes.MaterialApproval_BaseRoute, ClaimsAuthorizeResource = Permissions.Resources.MaterialApproval,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write, AuthorizeFacilityAccessWith = typeof(MaterialApproval))]
[UseHttpFunction(HttpFunctionApiMethod.Update, Route = Routes.MaterialApproval_IdRoute, ClaimsAuthorizeResource = Permissions.Resources.MaterialApproval,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write, AuthorizeFacilityAccessWith = typeof(MaterialApproval))]
[UseHttpFunction(HttpFunctionApiMethod.Patch, Route = Routes.MaterialApproval_IdRoute, ClaimsAuthorizeResource = Permissions.Resources.MaterialApproval,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
public partial class MaterialApprovalFunctions : HttpFunctionApiBase<MaterialApproval, MaterialApprovalEntity, Guid>
{
    private readonly IProvider<Guid, FacilityEntity> _facilityProvider;

    private readonly IMaterialApprovalManager _materialApprovalManager;

    private readonly ITruckTicketPdfRenderer _truckTicketPdfRenderer;

    public MaterialApprovalFunctions(ILog log,
                                     IMapperRegistry mapper,
                                     IManager<Guid, MaterialApprovalEntity> manager,
                                     IMaterialApprovalManager materialApprovalManager,
                                     IProvider<Guid, FacilityEntity> facilityProvider,
                                     ITruckTicketPdfRenderer truckTicketPdfRenderer) : base(log, mapper, manager)
    {
        _materialApprovalManager = materialApprovalManager;
        _facilityProvider = facilityProvider;
        _truckTicketPdfRenderer = truckTicketPdfRenderer;
    }

    [Function(nameof(MaterialApprovalWasteCodeByFacility))]
    [OpenApiOperation(nameof(MaterialApprovalWasteCodeByFacility), nameof(MaterialApprovalFunctions), Summary = nameof(Routes.MaterialApproval_WasteCodeRoute))]
    [OpenApiParameter(Routes.Parameters.id, In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ContentTypes.JSON, typeof(MaterialApproval))]
    [OpenApiResponseWithoutBody(HttpStatusCode.NotFound)]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    public async Task<HttpResponseData> MaterialApprovalWasteCodeByFacility(
        [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Get), Route = Routes.MaterialApproval_WasteCodeRoute)] HttpRequestData req,
        Guid id)
    {
        var data = await _materialApprovalManager.GetWasteCodeByFacility(id);
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(data);
        return response;
    }

    [Function(nameof(DownloadMaterialApprovalScaleTicket))]
    [OpenApiOperation(nameof(DownloadMaterialApprovalScaleTicket), nameof(MaterialApproval), Summary = nameof(Routes.MaterialApproval_DownloadScaleTicketStub))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Pdf, typeof(TruckTicket))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.MaterialApproval, Permissions.Operations.Read)]
    public async Task<HttpResponseData> DownloadMaterialApprovalScaleTicket(
        [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.MaterialApproval_DownloadScaleTicketStub)] HttpRequestData req,
        Guid id)
    {
        return await HandleRequest(req,
                                   nameof(DownloadMaterialApprovalScaleTicket),
                                   async response =>
                                   {
                                       var materialApproval = await _materialApprovalManager.GetById(id);
                                       var facility = await _facilityProvider.GetById(materialApproval.FacilityId);
                                       var renderedTicket = _truckTicketPdfRenderer.RenderMaterialApprovalScaleTicket(materialApproval, facility);
                                       await response.WriteBytesAsync(renderedTicket);
                                   });
    }
}
