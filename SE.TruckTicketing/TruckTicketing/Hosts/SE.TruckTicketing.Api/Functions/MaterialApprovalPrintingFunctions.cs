using System;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

using SE.Shared.Domain.Entities.MaterialApproval;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.Domain.Entities.TruckTicket.LocalReporting;

using Trident.Azure.Functions;
using Trident.Logging;
using Trident.Mapper;

namespace SE.TruckTicketing.Api.Functions;

public sealed class MaterialApprovalPrintingFunctions : HttpFunctionApiBase<MaterialApproval, MaterialApprovalEntity, Guid>
{
    private readonly IMaterialApprovalManager _materialApprovalManager;

    private readonly ITruckTicketPdfManager _truckTicketPdfManager;

    private readonly IMapperRegistry _mapper;

    public MaterialApprovalPrintingFunctions(ILog log,
                                             IMapperRegistry mapper,
                                             IMaterialApprovalManager materialApprovalManager,
                                             ITruckTicketPdfManager truckTicketPdfManager)
        : base(log, mapper, materialApprovalManager)
    {
        _mapper = mapper;
        _materialApprovalManager = materialApprovalManager;
        _truckTicketPdfManager = truckTicketPdfManager;
    }

    [Function(nameof(DownloadMaterialApprovalPdf))]
    [OpenApiOperation(nameof(DownloadMaterialApprovalPdf), nameof(MaterialApprovalFunctions), Summary = nameof(Routes.MaterialApprovalPdf))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Pdf, typeof(MaterialApproval))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    public async Task<HttpResponseData> DownloadMaterialApprovalPdf([HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.MaterialApprovalPdf)] HttpRequestData req,
                                                                    Guid id)
    {
        return await HandleRequest(req,
                                   nameof(DownloadMaterialApprovalPdf),
                                   async response =>
                                   {
                                       var renderedTicket = await _materialApprovalManager.CreateMaterialApprovalPdf(id);
                                       await response.WriteBytesAsync(renderedTicket);
                                   });
    }
}
