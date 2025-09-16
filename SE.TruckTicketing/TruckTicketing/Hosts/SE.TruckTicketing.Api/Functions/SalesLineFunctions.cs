using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Net.Http.Headers;

using SE.Shared.Common.Constants;
using SE.Shared.Domain.EmailTemplates;
using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.Infrastructure;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.LoadConfirmations;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.Contracts.Security;
using SE.TruckTicketing.Domain.Entities.SalesLine;

using Trident.Azure.Functions;
using Trident.Azure.Security;
using Trident.Contracts;
using Trident.Contracts.Api;
using Trident.Contracts.Configuration;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions;

[UseHttpFunction(HttpFunctionApiMethod.Create, Route = Routes.SalesLine.Base, ClaimsAuthorizeResource = Permissions.Resources.SalesLine, ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.SalesLine.Search, ClaimsAuthorizeResource = Permissions.Resources.SalesLine, ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.GetById, Route = Routes.SalesLine.Id, ClaimsAuthorizeResource = Permissions.Resources.SalesLine, ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Update, Route = Routes.SalesLine.Id, ClaimsAuthorizeResource = Permissions.Resources.SalesLine, ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Delete, Route = Routes.SalesLine.Id, ClaimsAuthorizeResource = Permissions.Resources.SalesLine, ClaimsAuthorizeOperation = Permissions.Operations.Write)]
public partial class SalesLineFunctions : HttpFunctionApiBase<SalesLine, SalesLineEntity, Guid>
{
    private readonly IAppSettings _appSettings;

    private readonly ILeaseObjectBlobStorage _blobStorage;

    private readonly IEmailTemplateSender _emailTemplateSender;

    private readonly ILoadConfirmationPdfRenderer _loadConfirmationPdfRenderer;

    private readonly ISalesLineManager _manager;

    private readonly ISalesLinesPublisher _salesLinesPublisher;

    private readonly IManager<Guid, TruckTicketEntity> _truckTicketManager;

    public SalesLineFunctions(ILog log,
                              IMapperRegistry mapper,
                              IManager<Guid, SalesLineEntity> manager,
                              ISalesLineManager salesLineManager,
                              IManager<Guid, TruckTicketEntity> truckTicketManager,
                              ILoadConfirmationPdfRenderer loadConfirmationPdfRenderer,
                              ISalesLinesPublisher salesLinesPublisher,
                              IEmailTemplateSender emailTemplateSender,
                              ILeaseObjectBlobStorage blobStorage,
                              IAppSettings appSettings)
        : base(log, mapper, manager)
    {
        _manager = salesLineManager;
        _truckTicketManager = truckTicketManager;
        _loadConfirmationPdfRenderer = loadConfirmationPdfRenderer;
        _salesLinesPublisher = salesLinesPublisher;
        _emailTemplateSender = emailTemplateSender;
        _blobStorage = blobStorage;
        _appSettings = appSettings;
    }

    [Function(nameof(GenerateAdHocLoadConfirmation))]
    [OpenApiOperation(nameof(GenerateAdHocLoadConfirmation), nameof(SalesLineFunctions))]
    [OpenApiRequestBody(ContentTypes.JSON, typeof(SalesLine[]))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Pdf, typeof(byte[]))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.SalesLine, Permissions.Operations.Read)]
    public async Task<HttpResponseData> GenerateAdHocLoadConfirmation(
        [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.SalesLine.PreviewLoadConfirmation)] HttpRequestData request)
    {
        return await HandleRequest(request,
                                   nameof(GenerateAdHocLoadConfirmation),
                                   async response =>
                                   {
                                       var model = await request.ReadFromJsonAsync<LoadConfirmationAdhocModel>();
                                       if (model.SalesLineKeys!.Count < 1)
                                       {
                                           return;
                                       }

                                       var report = await _loadConfirmationPdfRenderer.RenderAdHocLoadConfirmationPdf(model);
                                       if (report == null)
                                       {
                                           return;
                                       }

                                       response.Headers.Add(HeaderNames.ContentType, new[] { MediaTypeNames.Application.Pdf });
                                       response.Headers.Add(HeaderNames.ContentDisposition, new[] { DispositionTypeNames.Inline });
                                       await response.WriteBytesAsync(report);
                                   });
    }

    [Function(nameof(SendAdHocLoadConfirmation))]
    [OpenApiOperation(nameof(SendAdHocLoadConfirmation), nameof(SalesLineFunctions))]
    [OpenApiRequestBody(ContentTypes.JSON, typeof(EmailTemplateDeliveryRequest))]
    [OpenApiResponseWithoutBody(HttpStatusCode.NoContent)]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.SalesLine, Permissions.Operations.Read)]
    public async Task<HttpResponseData> SendAdHocLoadConfirmation(
        [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.SalesLine.SendAdHocLoadConfirmation)] HttpRequestData request)
    {
        return await HandleRequest(request,
                                   nameof(SendAdHocLoadConfirmation),
                                   async response =>
                                   {
                                       var emailRequest = await request.ReadFromJsonAsync<EmailTemplateDeliveryRequest>();

                                       // fix Guid parsing
                                       if (emailRequest.ContextBag.TryGetValue(nameof(LoadConfirmationEntity.BillingCustomerId), out var val) && val is string strVal)
                                       {
                                           if (Guid.TryParse(strVal, out var accId))
                                           {
                                               emailRequest.ContextBag[nameof(LoadConfirmationEntity.BillingCustomerId)] = accId;
                                           }
                                           else
                                           {
                                               emailRequest.ContextBag[nameof(LoadConfirmationEntity.BillingCustomerId)] = null;
                                           }
                                       }

                                       await _emailTemplateSender.Dispatch(emailRequest);
                                       response.StatusCode = HttpStatusCode.NoContent;
                                   });
    }

    [Function(nameof(SalesLinePreview))]
    [OpenApiOperation(nameof(SalesLinePreview), nameof(SalesLineFunctions))]
    [OpenApiRequestBody(ContentTypes.JSON, typeof(SalesLinePreviewRequest))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ContentTypes.JSON, typeof(List<SalesLine>))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    public async Task<HttpResponseData> SalesLinePreview([HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.SalesLine.Preview)] HttpRequestData req)
    {
        return await HandleRequest(req,
                                   nameof(SalesLinePreview),
                                   async response =>
                                   {
                                       var request = await req.ReadFromJsonAsync<SalesLinePreviewRequest>();
                                       if (request == null)
                                       {
                                           return;
                                       }

                                       bool.TryParse(_appSettings.GetKeyOrDefault("UseNewPreviewSalesLinesLogic", "true"), out var newPreviewSalesLines);

                                       request.UseNew = newPreviewSalesLines;
                                       var resultEntities = await _manager.GeneratePreviewSalesLines(request);

                                       var responseBody = Mapper.Map<List<SalesLine>>(resultEntities);

                                       await response.WriteAsJsonAsync(responseBody);
                                   });
    }

    [Function(nameof(SalesLinePriceRefresh))]
    [OpenApiOperation(nameof(SalesLinePriceRefresh), nameof(SalesLineFunctions))]
    [OpenApiRequestBody(ContentTypes.JSON, typeof(IEnumerable<SalesLine>))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ContentTypes.JSON, typeof(IEnumerable<SalesLine>))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.SalesLine, Permissions.Operations.Write)]
    public async Task<HttpResponseData> SalesLinePriceRefresh([HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.SalesLine.PriceRefresh)] HttpRequestData req)
    {
        return await HandleRequest(req,
                                   nameof(SalesLinePriceRefresh),
                                   async response =>
                                   {
                                       var request = await req.ReadFromJsonAsync<IEnumerable<SalesLine>>();
                                       var salesLineEntities = Mapper.Map<IEnumerable<SalesLineEntity>>(request).ToList();

                                       var resultEntities = await _manager.PriceRefresh(salesLineEntities);
                                       var responseBody = Mapper.Map<List<SalesLine>>(resultEntities);

                                       await response.WriteAsJsonAsync(responseBody);
                                   });
    }

    [Function(nameof(GetPrice))]
    [OpenApiOperation(nameof(GetPrice), nameof(SalesLineFunctions))]
    [OpenApiRequestBody(ContentTypes.JSON, typeof(SalesLinePriceRequest))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ContentTypes.JSON, typeof(IEnumerable<Double>))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    public async Task<HttpResponseData> GetPrice([HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.SalesLine.Price)] HttpRequestData req)
    {
        return await HandleRequest(req,
                                   nameof(GetPrice),
                                   async response =>
                                   {
                                       var request = await req.ReadFromJsonAsync<SalesLinePriceRequest>();

                                       var price = await _manager.GetPrice(request);

                                       await response.WriteAsJsonAsync(new[] { price });
                                   });
    }

    [Function(nameof(SalesLineBulkSave))]
    [OpenApiOperation(nameof(SalesLineBulkSave), nameof(SalesLineFunctions))]
    [OpenApiRequestBody(ContentTypes.JSON, typeof(IEnumerable<SalesLine>))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ContentTypes.JSON, typeof(IEnumerable<SalesLine>))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.SalesLine, Permissions.Operations.Write)]
    public async Task<HttpResponseData> SalesLineBulkSave([HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.SalesLine.Bulk)] HttpRequestData req)
    {
        return await HandleRequest(req,
                                   nameof(SalesLineBulkSave),
                                   async response =>
                                   {
                                       var request = await req.ReadFromJsonAsync<SalesLineResendAckRemovalRequest>();
                                       IEnumerable<SalesLineEntity> resultEntities = null;
                                       if (request != null)
                                       {
                                           var salesLineEntities = Mapper.Map<IEnumerable<SalesLineEntity>>(request.SalesLines).ToList();
                                           salesLineEntities.ForEach(sl => sl.ApplyFoRounding());

                                           if (request.IsPublishOnly)
                                           {
                                               resultEntities = await _manager.GetByIds(salesLineEntities.Select(sl => sl.Key).ToHashSet()); // PK - OK
                                           }
                                           else
                                           {
                                               resultEntities = await _manager.BulkSave(salesLineEntities);
                                           }

                                           await _salesLinesPublisher.PublishSalesLines(resultEntities);

                                           var responseBody = Mapper.Map<List<SalesLine>>(resultEntities);
                                           await response.WriteAsJsonAsync(responseBody);
                                       }
                                   });
    }

    [Function(nameof(SalesLineRemoveFromLoadConfirmationOrInvoice))]
    [OpenApiOperation(nameof(SalesLineRemoveFromLoadConfirmationOrInvoice), nameof(SalesLineFunctions))]
    [OpenApiRequestBody(ContentTypes.JSON, typeof(IEnumerable<Guid>))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ContentTypes.JSON, typeof(IEnumerable<SalesLine>))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.SalesLine, Permissions.Operations.Write)]
    public async Task<HttpResponseData> SalesLineRemoveFromLoadConfirmationOrInvoice(
        [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.SalesLine.Remove)] HttpRequestData req)
    {
        return await HandleRequest(req,
                                   nameof(SalesLineBulkSave),
                                   async response =>
                                   {
                                       var truckTicketKeys = await req.ReadFromJsonAsync<IEnumerable<CompositeKey<Guid>>>();
                                       var addedUpdatedSalesLineEntities = new List<SalesLineEntity>();
                                       var truckTickets = await _truckTicketManager.GetByIds(truckTicketKeys); // PK - OK

                                       async Task<IEnumerable<SalesLineEntity>> ProcessSalesLineRemovalFromLoadConfirmationOrInvoice(TruckTicketEntity truckTicket)
                                       {
                                           if (truckTicket.Status != TruckTicketStatus.Open)
                                           {
                                               // validation should catch trying to update a truck ticket that has already been invoiced
                                               truckTicket.Status = TruckTicketStatus.Open;
                                               await _truckTicketManager.Update(truckTicket, true);
                                           }

                                           return await _manager.RemoveSalesLinesFromLoadConfirmationOrInvoice(new List<Guid> { truckTicket.Id });
                                       }

                                       foreach (var truckTicket in truckTickets)
                                       {
                                           var salesLines = await _blobStorage.AcquireLeaseAndExecute(async () => await ProcessSalesLineRemovalFromLoadConfirmationOrInvoice(truckTicket),
                                                                                                      truckTicket.GetLockLeaseBlobName());

                                           addedUpdatedSalesLineEntities.AddRange(salesLines);
                                       }

                                       var responseBody = Mapper.Map<IEnumerable<SalesLine>>(addedUpdatedSalesLineEntities);

                                       await response.WriteAsJsonAsync(responseBody);
                                   });
    }
}
