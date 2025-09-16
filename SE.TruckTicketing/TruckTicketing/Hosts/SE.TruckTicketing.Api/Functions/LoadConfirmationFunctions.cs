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

using Newtonsoft.Json;

using SE.Enterprise.Contracts.Constants;
using SE.Enterprise.Contracts.Models;
using SE.Enterprise.Contracts.Models.InvoiceDelivery;
using SE.Shared.Domain;
using SE.Shared.Domain.Entities.EntityStatus;
using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.TruckTicketing.Contracts.Api.Models;
using SE.TruckTicketing.Contracts.Models.LoadConfirmations;
using SE.TruckTicketing.Contracts.Models.Statuses;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.Contracts.Security;

using Trident.Azure.Functions;
using Trident.Azure.Security;
using Trident.Contracts.Api;
using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions;

[UseHttpFunction(HttpFunctionApiMethod.GetById,
                 Route = Routes.LoadConfirmation.Id,
                 ClaimsAuthorizeResource = Permissions.Resources.LoadConfirmation,
                 ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Search,
                 Route = Routes.LoadConfirmation.Search,
                 ClaimsAuthorizeResource = Permissions.Resources.LoadConfirmation,
                 ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Create,
                 Route = Routes.LoadConfirmation.Base,
                 ClaimsAuthorizeResource = Permissions.Resources.LoadConfirmation,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Update,
                 Route = Routes.LoadConfirmation.Id,
                 ClaimsAuthorizeResource = Permissions.Resources.LoadConfirmation,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Patch,
                 Route = Routes.LoadConfirmation.Id,
                 ClaimsAuthorizeResource = Permissions.Resources.LoadConfirmation,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
public partial class LoadConfirmationFunctions : HttpFunctionApiBase<LoadConfirmation, LoadConfirmationEntity, Guid>
{
    private readonly ILoadConfirmationAttachmentManager _attachmentManager;

    private readonly IProvider<Guid, EntityStatusEntity> _entityStatusProvider;

    private readonly ILoadConfirmationApprovalWorkflow _loadConfirmationApprovalWorkflow;

    private readonly ILoadConfirmationManager _manager;

    private readonly IMapperRegistry _mapper;

    public LoadConfirmationFunctions(ILog log,
                                     IMapperRegistry mapper,
                                     ILoadConfirmationManager manager,
                                     ILoadConfirmationAttachmentManager attachmentManager,
                                     IProvider<Guid, EntityStatusEntity> entityStatusProvider,
                                     ILoadConfirmationApprovalWorkflow loadConfirmationApprovalWorkflow)
        : base(log, mapper, manager)
    {
        _mapper = mapper;
        _manager = manager;
        _attachmentManager = attachmentManager;
        _entityStatusProvider = entityStatusProvider;
        _loadConfirmationApprovalWorkflow = loadConfirmationApprovalWorkflow;
    }

    [Function(nameof(GetMany))]
    [OpenApiOperation(nameof(GetMany), nameof(LoadConfirmationFunctions), Summary = Routes.LoadConfirmation.FetchMany)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(List<EntityStatus>))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.LoadConfirmation, Permissions.Operations.Read)]
    public async Task<HttpResponseData> GetMany([HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.LoadConfirmation.FetchMany)] HttpRequestData httpRequestData)
    {
        return await HandleRequest(httpRequestData,
                                   nameof(GetMany),
                                   async httpResponseData =>
                                   {
                                       // list of keys
                                       var request = await httpRequestData.ReadFromJsonAsync<List<CompositeKey<Guid>>>();
                                       var lcIds = request.Select(k => k.Id).ToHashSet();

                                       // fetch all of them
                                       var statuses = await _entityStatusProvider.Get(s => lcIds.Contains(s.ReferenceEntityKey.Id),
                                                                                      EntityStatusEntity.GetPartitionKey(Databases.Discriminators.LoadConfirmation));

                                       // return all fetched LCs
                                       await httpResponseData.WriteAsJsonAsync(_mapper.Map<List<EntityStatus>>(statuses));
                                   });
    }

    [Function(nameof(SubmitLoadConfirmations))]
    [OpenApiOperation(nameof(SubmitLoadConfirmations), nameof(LoadConfirmationFunctions), Summary = Routes.LoadConfirmation.BulkAction)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(LoadConfirmationBulkResponse))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.LoadConfirmation, Permissions.Operations.Write)]
    public async Task<HttpResponseData> SubmitLoadConfirmations(
        [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.LoadConfirmation.BulkAction)] HttpRequestData httpRequestData)
    {
        return await HandleRequest(httpRequestData,
                                   nameof(SubmitLoadConfirmations),
                                   async httpResponseData =>
                                   {
                                       var request = await httpRequestData.ReadFromJsonAsync<LoadConfirmationBulkRequest>();

                                       // fan-out and queue all requests
                                       var response = await _manager.QueueLoadConfirmationAction(request);

                                       // all accepted
                                       await httpResponseData.WriteAsJsonAsync(response);
                                   });
    }

    [Function(nameof(ProcessApprovalEmail))]
    public async Task ProcessApprovalEmail([ServiceBusTrigger(ServiceBusConstants.Topics.ApprovalEmails,
                                                              ServiceBusConstants.Subscriptions.ApprovalEmailsInbound,
                                                              Connection = ServiceBusConstants.PrivateServiceBusNamespace)]
                                           string message,
                                           FunctionContext context)
    {
        var correlationId = context.BindingContext.BindingData.GetValueOrDefault(MessageConstants.CorrelationId);
        try
        {
            var request = JsonConvert.DeserializeObject<EntityEnvelopeModel<LoadConfirmationApprovalEmail>>(message)!;
            await _loadConfirmationApprovalWorkflow.ContinueFromApprovalEmail(request.Payload);
        }
        catch (Exception e)
        {
            AppLogger.Error<LoadConfirmationFunctions>(e, $"Unable to process the invoice delivery request. (CorrelationId: '{correlationId}')");
            throw;
        }
    }

    [Function(nameof(PreviewLatestLoadConfirmationDocument))]
    [OpenApiOperation(nameof(PreviewLatestLoadConfirmationDocument), nameof(LoadConfirmationFunctions), Summary = Routes.LoadConfirmation.Preview)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(UriDto))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.LoadConfirmation, Permissions.Operations.Read)]
    public async Task<HttpResponseData> PreviewLatestLoadConfirmationDocument(
        [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Get), Route = Routes.LoadConfirmation.Preview)] HttpRequestData httpRequestData,
        Guid id,
        string pk)
    {
        return await HandleRequest(httpRequestData,
                                   nameof(PreviewLatestLoadConfirmationDocument),
                                   async httpResponseData =>
                                   {
                                       var uri = await _loadConfirmationApprovalWorkflow.FetchLatestDocument(new(id, pk), DispositionTypeNames.Inline);
                                       var response = new UriDto { Uri = uri.ToString() };
                                       await httpResponseData.WriteAsJsonAsync(response);
                                   });
    }

    [Function(nameof(DownloadLatestLoadConfirmationDocument))]
    [OpenApiOperation(nameof(DownloadLatestLoadConfirmationDocument), nameof(LoadConfirmationFunctions), Summary = Routes.LoadConfirmation.Download)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(UriDto))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.LoadConfirmation, Permissions.Operations.Read)]
    public async Task<HttpResponseData> DownloadLatestLoadConfirmationDocument(
        [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Get), Route = Routes.LoadConfirmation.Download)] HttpRequestData httpRequestData,
        Guid id,
        string pk)
    {
        return await HandleRequest(httpRequestData,
                                   nameof(DownloadLatestLoadConfirmationDocument),
                                   async httpResponseData =>
                                   {
                                       var uri = await _loadConfirmationApprovalWorkflow.FetchLatestDocument(new(id, pk), DispositionTypeNames.Attachment);
                                       var response = new UriDto { Uri = uri.ToString() };
                                       await httpResponseData.WriteAsJsonAsync(response);
                                   });
    }

    [Function(nameof(ProcessFieldTicketUpdate))]
    public async Task ProcessFieldTicketUpdate([ServiceBusTrigger(ServiceBusConstants.Topics.InvoiceDelivery,
                                                                  ServiceBusConstants.Subscriptions.InvoiceDeliveryResponses,
                                                                  Connection = ServiceBusConstants.PrivateServiceBusNamespace)]
                                               string message,
                                               FunctionContext context)
    {
        var correlationId = context.BindingContext.BindingData.GetValueOrDefault(MessageConstants.CorrelationId);

        try
        {
            var invoiceDeliveryResponse = JsonConvert.DeserializeObject<DeliveryResponse>(message);
            await _loadConfirmationApprovalWorkflow.ProcessResponseFromInvoiceGateway(invoiceDeliveryResponse);
        }
        catch (Exception e)
        {
            AppLogger.Error<LoadConfirmationFunctions>(e, $"Unable to process the invoice delivery request. (CorrelationId: '{correlationId}')");
            throw;
        }
    }

    [Function(nameof(GetLcAttachmentDownloadUri))]
    [OpenApiOperation(nameof(GetLcAttachmentDownloadUri), nameof(LoadConfirmationFunctions), Summary = Routes.LoadConfirmation.AttachmentDownload)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(UriDto))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    public async Task<HttpResponseData> GetLcAttachmentDownloadUri([HttpTrigger(AuthorizationLevel.Anonymous,
                                                                                nameof(HttpMethod.Get),
                                                                                Route = Routes.LoadConfirmation.AttachmentDownload)]
                                                                   HttpRequestData httpRequestData,
                                                                   Guid id,
                                                                   string pk,
                                                                   Guid attachmentId)
    {
        return await HandleRequest(httpRequestData,
                                   nameof(GetLcAttachmentDownloadUri),
                                   async response =>
                                   {
                                       var uri = await _attachmentManager.GetDownloadUrl(new(id, pk), attachmentId);
                                       await response.WriteAsJsonAsync(new UriDto { Uri = uri.ToString() });
                                   });
    }

    [Function(nameof(GetLcAttachmentUploadUri))]
    [OpenApiOperation(nameof(GetLcAttachmentUploadUri), nameof(LoadConfirmationFunctions), Summary = Routes.LoadConfirmation.AttachmentUpload)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(UriDto))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    public async Task<HttpResponseData> GetLcAttachmentUploadUri([HttpTrigger(AuthorizationLevel.Anonymous,
                                                                              nameof(HttpMethod.Post),
                                                                              Route = Routes.LoadConfirmation.AttachmentUpload)]
                                                                 HttpRequestData httpRequestData,
                                                                 Guid id,
                                                                 string pk)
    {
        return await HandleRequest(httpRequestData,
                                   nameof(GetLcAttachmentUploadUri),
                                   async response =>
                                   {
                                       var request = await httpRequestData.ReadFromJsonAsync<LoadConfirmationAttachment>();
                                       var (uri, attachmentEntity) = _attachmentManager.GetUploadUrl(new(id, pk), request!.FileName);
                                       var attachment = _mapper.Map<LoadConfirmationAttachment>(attachmentEntity);
                                       attachment.Uri = uri;
                                       await response.WriteAsJsonAsync(attachment);
                                   });
    }

    [Function(nameof(RemoveAttachmentOnLoadConfirmation))]
    [OpenApiOperation(nameof(RemoveAttachmentOnLoadConfirmation), nameof(LoadConfirmationFunctions), Summary = Routes.LoadConfirmation.AttachmentRemove)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(LoadConfirmation))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.LoadConfirmation, Permissions.Operations.Write)]
    public async Task<HttpResponseData> RemoveAttachmentOnLoadConfirmation(
        [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Patch), Route = Routes.LoadConfirmation.AttachmentRemove)] HttpRequestData request,
        Guid id,
        string pk,
        Guid attachmentId)
    {
        return await HandleRequest(request,
                                   nameof(RemoveAttachmentOnLoadConfirmation),
                                   async response =>
                                   {
                                       var loadConfirmationEntity = await _attachmentManager.RemoveAttachmentOnLoadConfirmation(new(id, pk), attachmentId);
                                       var loadConfirmation = Mapper.Map<LoadConfirmation>(loadConfirmationEntity);
                                       await response.WriteAsJsonAsync(loadConfirmation);
                                   });
    }
}
