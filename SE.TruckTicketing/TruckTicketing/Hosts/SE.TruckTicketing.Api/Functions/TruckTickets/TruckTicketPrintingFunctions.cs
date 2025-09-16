using System;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Domain.Entities.TruckTicket.LocalReporting;

using Trident.Azure.Functions;
using Trident.Logging;
using Trident.Mapper;

namespace SE.TruckTicketing.Api.Functions.TruckTickets;

public sealed class TruckTicketPrintingFunctions : HttpFunctionApiBase<TruckTicket, TruckTicketEntity, Guid>
{
    private readonly ITruckTicketManager _truckTicketManager;

    private readonly ITruckTicketPdfManager _truckTicketPdfManager;

    private readonly ITruckTicketXlsxManager _truckTicketXlsxManager;

    private readonly IMapperRegistry _mapper;

    public TruckTicketPrintingFunctions(ILog log,
                                        IMapperRegistry mapper,
                                        ITruckTicketManager truckTicketManager,
                                        ITruckTicketPdfManager truckTicketPdfManager,
                                        ITruckTicketXlsxManager truckTicketXlsxManager)
        : base(log, mapper, truckTicketManager)
    {
        _mapper = mapper;
        _truckTicketManager = truckTicketManager;
        _truckTicketPdfManager = truckTicketPdfManager;
        _truckTicketXlsxManager = truckTicketXlsxManager;
    }

    [Function(nameof(DownloadCompleteTicket))]
    [OpenApiOperation(nameof(DownloadCompleteTicket), nameof(TruckTicketFunctions), Summary = nameof(Routes.TruckTickets.TicketDownload))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Pdf, typeof(TruckTicket))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    public async Task<HttpResponseData> DownloadCompleteTicket([HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.TruckTickets.TicketDownload)] HttpRequestData req,
                                                               Guid id, string pk)
    {
        return await HandleRequest(req,
                                   nameof(DownloadCompleteTicket),
                                   async response =>
                                   {
                                       var renderedTicket = await _truckTicketPdfManager.CreateTicketPrint(new(id, pk));
                                       await response.WriteBytesAsync(renderedTicket);
                                   });
    }

    [Function(nameof(DownloadLandfillDailyTicket))]
    [OpenApiOperation(nameof(DownloadLandfillDailyTicket), nameof(TruckTicketFunctions), Summary = nameof(Routes.TruckTickets.LandfillTicketDownload))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Pdf, typeof(TruckTicket))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    public async Task<HttpResponseData> DownloadLandfillDailyTicket(
        [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.TruckTickets.LandfillTicketDownload)] HttpRequestData req)
    {
        return await HandleRequest(req,
                                   nameof(DownloadLandfillDailyTicket),
                                   async response =>
                                   {
                                       var request = await req.ReadFromJsonAsync<LandfillDailyReportRequest>();
                                       var renderedTicket = await _truckTicketPdfManager.CreateLandfillDailyReport(request);
                                       await response.WriteBytesAsync(renderedTicket);
                                   });
    }

    [Function(nameof(DownloadLandfillDailyTicketXlsx))]
    [OpenApiOperation(nameof(DownloadLandfillDailyTicketXlsx), nameof(TruckTicketFunctions), Summary = nameof(Routes.TruckTickets.LandfillTicketDownloadXlsx))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", typeof(TruckTicket))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    public async Task<HttpResponseData> DownloadLandfillDailyTicketXlsx(
    [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.TruckTickets.LandfillTicketDownloadXlsx)] HttpRequestData req)
    {
        return await HandleRequest(req,
                                   nameof(DownloadLandfillDailyTicketXlsx),
                                   async response =>
                                   {
                                       var request = await req.ReadFromJsonAsync<LandfillDailyReportRequest>();
                                       var renderedTicket = await _truckTicketXlsxManager.CreateLandfillDailyReport(request);
                                       await response.WriteBytesAsync(renderedTicket);
                                   });
    }

    [Function(nameof(DownloadFstDailyWorkTickets))]
    [OpenApiOperation(nameof(DownloadFstDailyWorkTickets), nameof(TruckTicketFunctions), Summary = nameof(Routes.TruckTickets.FstDailyWorkTicketDownload))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Pdf, typeof(TruckTicket))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    public async Task<HttpResponseData> DownloadFstDailyWorkTickets(
        [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.TruckTickets.FstDailyWorkTicketDownload)] HttpRequestData req)
    {
        return await HandleRequest(req,
                                   nameof(DownloadFstDailyWorkTickets),
                                   async response =>
                                   {
                                       var fstReportRequest = await req.ReadFromJsonAsync<FSTWorkTicketRequest>();
                                       var renderedTicket = await _truckTicketPdfManager.CreateFstDailyReport(fstReportRequest);
                                       await response.WriteBytesAsync(renderedTicket);
                                   });
    }

    [Function(nameof(DownloadFstDailyWorkTicketsXlsx))]
    [OpenApiOperation(nameof(DownloadFstDailyWorkTicketsXlsx), nameof(TruckTicketFunctions), Summary = nameof(Routes.TruckTickets.FstDailyWorkTicketDownloadXlsx))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", typeof(TruckTicket))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    public async Task<HttpResponseData> DownloadFstDailyWorkTicketsXlsx(
        [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.TruckTickets.FstDailyWorkTicketDownloadXlsx)] HttpRequestData req)
    {
        return await HandleRequest(req,
                                   nameof(DownloadFstDailyWorkTicketsXlsx),
                                   async response =>
                                   {
                                       var fstReportRequest = await req.ReadFromJsonAsync<FSTWorkTicketRequest>();
                                       var renderedTicket = await _truckTicketXlsxManager.CreateFstDailyReport(fstReportRequest);
                                       await response.WriteBytesAsync(renderedTicket);
                                   });
    }

    [Function(nameof(DownloadLoadSummaryTicket))]
    [OpenApiOperation(nameof(DownloadLoadSummaryTicket), nameof(TruckTicketFunctions), Summary = nameof(Routes.TruckTickets.LoadSummaryTicketDownload))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Pdf, typeof(TruckTicket))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    public async Task<HttpResponseData> DownloadLoadSummaryTicket(
        [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.TruckTickets.LoadSummaryTicketDownload)] HttpRequestData req)
    {
        return await HandleRequest(req,
                                   nameof(DownloadLoadSummaryTicket),
                                   async response =>
                                   {
                                       var loadSummaryReportRequest = await req.ReadFromJsonAsync<LoadSummaryReportRequest>();
                                       var renderedTicket = await _truckTicketPdfManager.CreateLoadSummaryReport(loadSummaryReportRequest);
                                       await response.WriteBytesAsync(renderedTicket);
                                   });
    }

    [Function(nameof(DownloadProducerReport))]
    [OpenApiOperation(nameof(DownloadProducerReport), nameof(TruckTicketFunctions), Summary = nameof(Routes.TruckTickets.ProducerReportDownload))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Pdf, typeof(TruckTicket))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    public async Task<HttpResponseData> DownloadProducerReport(
        [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.TruckTickets.ProducerReportDownload)] HttpRequestData req)
    {
        return await HandleRequest(req,
                                   nameof(DownloadProducerReport),
                                   async response =>
                                   {
                                       var productReportRequest = await req.ReadFromJsonAsync<ProducerReportRequest>();
                                       var renderedTicket = await _truckTicketPdfManager.CreateProducerReport(productReportRequest);
                                       await response.WriteBytesAsync(renderedTicket);
                                   });
    }
}
