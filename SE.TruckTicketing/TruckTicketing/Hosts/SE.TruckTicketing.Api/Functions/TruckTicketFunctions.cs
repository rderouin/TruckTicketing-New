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

using SE.Shared.Common;
using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.Infrastructure;
using SE.Shared.Functions;
using SE.TruckTicketing.Contracts.Api.Models;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.TruckTicket;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.Contracts.Security;
using SE.TruckTicketing.Domain.Entities.SalesLine;
using SE.TruckTicketing.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Domain.Entities.TruckTicket.LocalReporting;
using SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

using Trident.Api.Search;
using Trident.Azure.Functions;
using Trident.Azure.Security;
using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.Search;
using Trident.SourceGeneration.Attributes;

using CompareOperators = Trident.Search.CompareOperators;

namespace SE.TruckTicketing.Api.Functions;

[UseHttpFunction(HttpFunctionApiMethod.Create, Route = Routes.TruckTickets.Base, ClaimsAuthorizeResource = Permissions.Resources.TruckTicket, ClaimsAuthorizeOperation = Permissions.Operations.Write,
                 AuthorizeFacilityAccessWith = typeof(TruckTicket))]
[UseHttpFunction(HttpFunctionApiMethod.GetById, Route = Routes.TruckTicket_IdRouteTemplate, ClaimsAuthorizeResource = Permissions.Resources.TruckTicket,
                 ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Patch, Route = Routes.TruckTicket_IdRouteTemplate, ClaimsAuthorizeResource = Permissions.Resources.TruckTicket,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Update, Route = Routes.TruckTicket_IdRouteTemplate, ClaimsAuthorizeResource = Permissions.Resources.TruckTicket,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write, AuthorizeFacilityAccessWith = typeof(TruckTicket))]
[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.TruckTicket_SearchRoute, ClaimsAuthorizeResource = Permissions.Resources.TruckTicket,
                 ClaimsAuthorizeOperation = Permissions.Operations.Read)]
public partial class TruckTicketFunctions : HttpFunctionApiBase<TruckTicket, TruckTicketEntity, Guid>
{
    private readonly IProvider<Guid, BillingConfigurationEntity> _billingConfigurationProvider;

    private readonly ILeaseObjectBlobStorage _blobStorage;

    private readonly IMapperRegistry _mapper;

    private readonly IProvider<Guid, SalesLineEntity> _salesLineProvider;

    private readonly ITruckTicketInvoiceService _truckTicketInvoiceService;

    private readonly ITruckTicketManager _truckTicketManager;

    private readonly ITruckTicketPdfRenderer _truckTicketPdfRenderer;

    private readonly ITruckTicketSalesManager _truckTicketSalesManager;

    public TruckTicketFunctions(ILog log,
                                IMapperRegistry mapper,
                                ITruckTicketManager truckTicketManager,
                                ITruckTicketPdfRenderer truckTicketPdfRenderer,
                                ITruckTicketSalesManager truckTicketSalesManager,
                                ITruckTicketInvoiceService truckTicketInvoiceService,
                                IProvider<Guid, BillingConfigurationEntity> billingConfigurationProvider,
                                ILeaseObjectBlobStorage blobStorage,
                                IProvider<Guid, SalesLineEntity> salesLineProvider)
        : base(log, mapper, truckTicketManager)
    {
        _truckTicketManager = truckTicketManager;
        _truckTicketPdfRenderer = truckTicketPdfRenderer;
        _truckTicketSalesManager = truckTicketSalesManager;
        _truckTicketInvoiceService = truckTicketInvoiceService;
        _billingConfigurationProvider = billingConfigurationProvider;
        _blobStorage = blobStorage;
        _salesLineProvider = salesLineProvider;
        _mapper = mapper;
    }

    [Function(nameof(CreateTruckTicketStubs))]
    [OpenApiOperation(nameof(CreateTruckTicketStubs), nameof(TruckTicketFunctions), Summary = nameof(Routes.TruckTicket_Stubs))]
    [OpenApiRequestBody("application/json", typeof(SearchCriteriaModel))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Pdf, typeof(TruckTicket))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.TruckTicket, Permissions.Operations.Write)]
    public async Task<HttpResponseData> CreateTruckTicketStubs([HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.TruckTicket_Stubs)] HttpRequestData req)
    {
        return await HandleRequest(req,
                                   nameof(CreateTruckTicketStubs),
                                   async response =>
                                   {
                                       var request = await req.ReadFromJsonAsync<TruckTicketStubCreationRequest>();
                                       var renderedTickets = Array.Empty<byte>();

                                       Task RenderTicketStubs(IEnumerable<TruckTicketEntity> tickets)
                                       {
                                           if (!request.GeneratePdf)
                                           {
                                               return Task.CompletedTask;
                                           }

                                           renderedTickets = _truckTicketPdfRenderer.RenderTruckTicketStubs(tickets.ToList());
                                           return Task.CompletedTask;
                                       }

                                       await _truckTicketManager.CreatePrePrintedTruckTicketStubs(request.FacilityId, request.Count, RenderTicketStubs);
                                       await response.WriteBytesAsync(renderedTickets);
                                   });
    }

    [Function(nameof(GetMatchingBillingConfigurations))]
    [OpenApiOperation(nameof(GetMatchingBillingConfigurations), nameof(TruckTicketFunctions), Summary = nameof(Routes.TruckTicket_MatchingBillingConfiguration))]
    [OpenApiRequestBody(ContentTypes.JSON, typeof(TruckTicket))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ContentTypes.JSON, typeof(TruckTicket))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.TruckTicket, Permissions.Operations.Read)]
    public async Task<HttpResponseData> GetMatchingBillingConfigurations(
        [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Get), nameof(HttpMethod.Post), Route = Routes.TruckTicket_MatchingBillingConfiguration)] HttpRequestData req)
    {
        return await HandleRequest(req, nameof(GetMatchingBillingConfigurations), async response =>
                                                                                  {
                                                                                      var request = await req.ReadFromJsonAsync<TruckTicket>();
                                                                                      var entity = _mapper.Map<TruckTicketEntity>(request);
                                                                                      var billingConfigurationEntities = await _truckTicketManager.GetMatchingBillingConfigurations(entity) ?? new();
                                                                                      await response.WriteAsJsonAsync(Mapper.Map<IEnumerable<BillingConfiguration>>(billingConfigurationEntities));
                                                                                  });
    }

    [Function(nameof(InitializeTruckTicketBillingAndSales))]
    [OpenApiOperation(nameof(InitializeTruckTicketBillingAndSales), nameof(TruckTicketFunctions), Summary = nameof(Routes.TruckTicket_Initialize_Sales_Billing))]
    [OpenApiRequestBody(ContentTypes.JSON, typeof(TruckTicket))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ContentTypes.JSON, typeof(TruckTicketInitRequestModel))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.TruckTicket, Permissions.Operations.Read)]
    public async Task<HttpResponseData> InitializeTruckTicketBillingAndSales(
        [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Get), nameof(HttpMethod.Post), Route = Routes.TruckTicket_Initialize_Sales_Billing)] HttpRequestData req)
    {
        return await HandleRequest(req, nameof(InitializeTruckTicketBillingAndSales), async response =>
                                                                                      {
                                                                                          var request = await req.ReadFromJsonAsync<TruckTicketInitRequestModel>();
                                                                                          var entity = _mapper.Map<TruckTicketEntity>(request.TruckTicket);
                                                                                          var billingConfigSalesResponse = new TruckTicketInitResponseModel();
                                                                                          //Load Matching BillingConfiguration
                                                                                          var selectedBillingConfigurationEntities = new List<BillingConfigurationEntity>();
                                                                                          var selectedSalesLines = new List<SalesLineEntity>();

                                                                                          if (!request.ShouldRunBillingConfiguration)
                                                                                          {
                                                                                              return;
                                                                                          }

                                                                                          if (entity.Status is TruckTicketStatus.New or TruckTicketStatus.Open or TruckTicketStatus.Stub
                                                                                                            or TruckTicketStatus.Hold)
                                                                                          {
                                                                                              selectedBillingConfigurationEntities =
                                                                                                  await _truckTicketManager.GetMatchingBillingConfigurations(entity) ?? new();

                                                                                              selectedBillingConfigurationEntities = selectedBillingConfigurationEntities
                                                                                                 .OrderByDescending(config => config.IncludeForAutomation)
                                                                                                 .ThenBy(config => config.Name)
                                                                                                 .ToList();

                                                                                              billingConfigSalesResponse.IsUpdateBillingConfiguration = true;
                                                                                          }
                                                                                          else
                                                                                          {
                                                                                              var billingConfig =
                                                                                                  await _billingConfigurationProvider.GetById(entity.BillingConfigurationId.GetValueOrDefault());

                                                                                              selectedBillingConfigurationEntities.Add(billingConfig);
                                                                                          }

                                                                                          if (!entity.TicketNumber.HasText())
                                                                                          {
                                                                                              return;
                                                                                          }

                                                                                          var salesLineSearch = await _salesLineProvider.Search(new()
                                                                                          {
                                                                                              Filters =
                                                                                              {
                                                                                                  { nameof(SalesLineEntity.TruckTicketId), entity.Id },
                                                                                                  {
                                                                                                      nameof(SalesLineEntity.Status), new Compare
                                                                                                      {
                                                                                                          Operator = CompareOperators.ne,
                                                                                                          Value = SalesLineStatus.Void.ToString(),
                                                                                                      }
                                                                                                  },
                                                                                              },
                                                                                          });

                                                                                          selectedSalesLines.AddRange(salesLineSearch?.Results?.ToList() ?? new());

                                                                                          var selectedBillingConfigModel = Mapper.Map<List<BillingConfiguration>>(selectedBillingConfigurationEntities);
                                                                                          var selectedSalesLineModel = Mapper.Map<List<SalesLine>>(selectedSalesLines);
                                                                                          billingConfigSalesResponse.BillingConfigurations.AddRange(selectedBillingConfigModel);
                                                                                          billingConfigSalesResponse.SalesLines.AddRange(selectedSalesLineModel);
                                                                                          await response.WriteAsJsonAsync(Mapper.Map<TruckTicketInitResponseModel>(billingConfigSalesResponse));
                                                                                      });
    }

    [Function(nameof(GetAttachmentUploadUri))]
    [OpenApiOperation(nameof(GetAttachmentUploadUri), nameof(TruckTicketFunctions), Summary = Routes.TruckTickets.AttachmentUpload)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(UriDto))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.TruckTicket, Permissions.Operations.Write)]
    public async Task<HttpResponseData> GetAttachmentUploadUri([HttpTrigger(AuthorizationLevel.Anonymous,
                                                                            nameof(HttpMethod.Post),
                                                                            Route = Routes.TruckTickets.AttachmentUpload)]
                                                               HttpRequestData httpRequestData,
                                                               Guid id, string pk)
    {
        return await HandleRequest(httpRequestData,
                                   nameof(GetAttachmentUploadUri),
                                   async response =>
                                   {
                                       var attachment = await httpRequestData.ReadFromJsonAsync<TruckTicketAttachment>();
                                       if (attachment == null)
                                       {
                                           response.StatusCode = HttpStatusCode.BadRequest;
                                           return;
                                       }

                                       var (attachmentEntity, uri) = await _truckTicketManager.GetUploadUrl(new(id, pk), attachment.File, attachment.ContentType);
                                       await response.WriteAsJsonAsync(new TruckTicketAttachmentUpload
                                       {
                                           Attachment = Mapper.Map<TruckTicketAttachment>(attachmentEntity),
                                           Uri = uri,
                                       });
                                   });
    }

    [Function(nameof(MarkFileUploadedOnTruckTicket))]
    [OpenApiOperation(nameof(MarkFileUploadedOnTruckTicket), nameof(TruckTicketFunctions), Summary = Routes.TruckTickets.AttachmentMarkUploaded)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(TruckTicket))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.TruckTicket, Permissions.Operations.Write)]
    public async Task<HttpResponseData> MarkFileUploadedOnTruckTicket(
        [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Patch), Route = Routes.TruckTickets.AttachmentMarkUploaded)] HttpRequestData request,
        Guid id,
        string pk,
        Guid attachmentId)
    {
        return await HandleRequest(request,
                                   nameof(MarkFileUploadedOnTruckTicket),
                                   async response =>
                                   {
                                       var truckTicketEntity = await _truckTicketManager.MarkFileUploaded(new(id, pk), attachmentId);
                                       var truckTicket = Mapper.Map<TruckTicket>(truckTicketEntity);
                                       await response.WriteAsJsonAsync(truckTicket);
                                   });
    }

    [Function(nameof(RemoveAttachmentOnTruckTicket))]
    [OpenApiOperation(nameof(RemoveAttachmentOnTruckTicket), nameof(TruckTicketFunctions), Summary = Routes.TruckTickets.AttachmentRemove)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(TruckTicket))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.TruckTicket, Permissions.Operations.Write)]
    public async Task<HttpResponseData> RemoveAttachmentOnTruckTicket(
        [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Patch), Route = Routes.TruckTickets.AttachmentRemove)] HttpRequestData request,
        Guid id,
        string pk,
        Guid attachmentId)
    {
        return await HandleRequest(request,
                                   nameof(RemoveAttachmentOnTruckTicket),
                                   async response =>
                                   {
                                       var truckTicketEntity = await _truckTicketManager.RemoveAttachmentOnTruckTicket(new(id, pk), attachmentId);
                                       var truckTicket = Mapper.Map<TruckTicket>(truckTicketEntity);
                                       await response.WriteAsJsonAsync(truckTicket);
                                   });
    }

    [Function(nameof(GetAttachmentDownloadUri))]
    [OpenApiOperation(nameof(GetAttachmentDownloadUri), nameof(TruckTicketFunctions), Summary = Routes.TruckTickets.AttachmentDownload)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(UriDto))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    public async Task<HttpResponseData> GetAttachmentDownloadUri([HttpTrigger(AuthorizationLevel.Anonymous,
                                                                              nameof(HttpMethod.Post),
                                                                              Route = Routes.TruckTickets.AttachmentDownload)]
                                                                 HttpRequestData httpRequestData,
                                                                 Guid id,
                                                                 string pk,
                                                                 Guid attachmentId)
    {
        return await HandleRequest(httpRequestData,
                                   nameof(GetAttachmentDownloadUri),
                                   async response =>
                                   {
                                       var uri = await _truckTicketManager.GetDownloadUrl(new(id, pk), attachmentId);
                                       await response.WriteAsJsonAsync(uri);
                                   });
    }

    [Function(nameof(PersistTruckTicketAndSalesLines))]
    [OpenApiOperation(nameof(PersistTruckTicketAndSalesLines), nameof(TruckTicketFunctions))]
    [OpenApiRequestBody(ContentTypes.JSON, typeof(TruckTicketSalesPersistenceRequest))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ContentTypes.JSON, typeof(TruckTicketSalesPersistenceResponse))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.TruckTicket, Permissions.Operations.Write)]
    [AuthorizeFacilityAccessWith(typeof(TruckTicketSalesPersistenceRequest))]
    public async Task<HttpResponseData> PersistTruckTicketAndSalesLines(
        [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.TruckTickets.TicketAndSalesPersistence)] HttpRequestData req)
    {
        return await HandleRequest(req,
                                   nameof(PersistTruckTicketAndSalesLines),
                                   async response =>
                                   {
                                       var request = await req.ReadFromJsonAsync<TruckTicketSalesPersistenceRequest>();
                                       var truckTicket = Mapper.Map<TruckTicketEntity>(request.TruckTicket);
                                       var salesLines = Mapper.Map<List<SalesLineEntity>>(request.SalesLines);

                                       async Task<bool> ProcessTruckTicketAndSalesLines()
                                       {
                                           await _truckTicketSalesManager.PersistTruckTicketAndSalesLines(truckTicket, salesLines);
                                           return true;
                                       }

                                       await _blobStorage.AcquireLeaseAndExecute(async () => await ProcessTruckTicketAndSalesLines(), truckTicket.GetLockLeaseBlobName());
                                       var result = new TruckTicketSalesPersistenceResponse
                                       {
                                           TruckTicket = Mapper.Map<TruckTicket>(truckTicket),
                                           SalesLines = Mapper.Map<List<SalesLine>>(salesLines),
                                       };

                                       await response.WriteAsJsonAsync(result);
                                   });
    }

    [Function(nameof(ConfirmCustomerOnTickets))]
    [OpenApiOperation(nameof(ConfirmCustomerOnTickets), nameof(TruckTicketFunctions))]
    [OpenApiRequestBody(ContentTypes.JSON, typeof(List<TruckTicket>))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ContentTypes.JSON, typeof(bool))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    public async Task<HttpResponseData> ConfirmCustomerOnTickets(
        [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.TruckTickets.ConfirmCustomerOnTruckTickets)] HttpRequestData req)
    {
        return await HandleRequest(req, nameof(ConfirmCustomerOnTickets), async response =>
                                                                          {
                                                                              var tickets = _mapper.Map<List<TruckTicketEntity>>(await req.ReadFromJsonAsync<List<TruckTicket>>());
                                                                              var responseData = await _truckTicketManager.ConfirmCustomerOnTickets(tickets);
                                                                              await response.WriteAsJsonAsync(responseData);
                                                                          });
    }

    [Function(nameof(TruckTicketSplitTickets))]
    [OpenApiOperation(nameof(TruckTicketSplitTickets), nameof(TruckTicketFunctions))]
    [OpenApiRequestBody(ContentTypes.JSON, typeof(List<TruckTicket>))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ContentTypes.JSON, typeof(List<TruckTicket>))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.TruckTicket, Permissions.Operations.Write)]
    public async Task<HttpResponseData> TruckTicketSplitTickets([HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.TruckTickets.SplitTruckTicket)] HttpRequestData req,
                                                                Guid id, string pk)
    {
        return await HandleRequest(req, nameof(TruckTicketSplitTickets), async response =>
                                                                         {
                                                                             var tickets = _mapper.Map<List<TruckTicketEntity>>(await req.ReadFromJsonAsync<List<TruckTicket>>());
                                                                             var responseData = await _truckTicketManager.SplitTruckTicket(tickets, new(id, pk));
                                                                             await response.WriteAsJsonAsync(_mapper.Map<List<TruckTicket>>(responseData));
                                                                         });
    }

    [Function(nameof(EvaluateTruckTicketInvoiceThreshold))]
    [OpenApiOperation(nameof(EvaluateTruckTicketInvoiceThreshold), nameof(TruckTicketFunctions))]
    [OpenApiRequestBody(ContentTypes.JSON, typeof(TruckTicketAssignInvoiceRequest))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ContentTypes.JSON, typeof(string))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.TruckTicket, Permissions.Operations.Read)]
    public async Task<HttpResponseData> EvaluateTruckTicketInvoiceThreshold(
        [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.TruckTickets.EvaluateInvoiceThreshold)] HttpRequestData req)
    {
        return await HandleRequest(req,
                                   nameof(EvaluateTruckTicketInvoiceThreshold),
                                   async response =>
                                   {
                                       var request = await req.ReadFromJsonAsync<TruckTicketAssignInvoiceRequest>();
                                       if (request == null)
                                       {
                                           return;
                                       }

                                       var truckTicket = Mapper.Map<TruckTicketEntity>(request.TruckTicket);
                                       if (request.BillingConfigurationId == default)
                                       {
                                           return;
                                       }

                                       var billingConfiguration = await _billingConfigurationProvider.GetById(request.BillingConfigurationId);
                                       var responseData =
                                           await _truckTicketInvoiceService.EvaluateInvoiceConfigurationThreshold(truckTicket, billingConfiguration, request.SalesLineCount, request.SalesTotalValue);

                                       await response.WriteAsJsonAsync(responseData);
                                   });
    }
}
