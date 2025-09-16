using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

using Newtonsoft.Json;

using SE.Enterprise.Contracts.Constants;
using SE.Enterprise.Contracts.Models;
using SE.Shared.Common.Constants;
using SE.Shared.Domain.Entities.Invoices;
using SE.TruckTicketing.Contracts.Models.Invoices;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.Contracts.Security;
using SE.TruckTicketing.Domain.Entities.Invoices;
using SE.TruckTicketing.Domain.Entities.Invoices.InvoiceReversal;
using SE.TruckTicketing.Domain.Entities.Invoices.Tasks;

using Trident.Azure.Functions;
using Trident.Azure.Security;
using Trident.Contracts;
using Trident.Extensions.OpenApi.Attributes;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions;

[UseHttpFunction(HttpFunctionApiMethod.Create, Route = Routes.Invoice.Base, ClaimsAuthorizeResource = Permissions.Resources.Invoice, ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.Invoice.Search, ClaimsAuthorizeResource = Permissions.Resources.Invoice, ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.GetById, Route = Routes.Invoice.Id, ClaimsAuthorizeResource = Permissions.Resources.Invoice, ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Update, Route = Routes.Invoice.Id, ClaimsAuthorizeResource = Permissions.Resources.Invoice, ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Delete, Route = Routes.Invoice.Id, ClaimsAuthorizeResource = Permissions.Resources.Invoice, ClaimsAuthorizeOperation = Permissions.Operations.Write)]
public partial class InvoiceFunctions : HttpFunctionApiBase<Invoice, InvoiceEntity, Guid>
{
    private readonly IInvoiceReversalWorkflow _invoiceReversalWorkflow;

    private readonly IInvoiceWorkflowOrchestrator _invoiceWorkflowOrchestrator;

    private readonly IInvoiceManager _manager;

    private readonly ISalesOrderPublisher _salesOrderPublisher;

    public InvoiceFunctions(ILog log,
                            IMapperRegistry mapper,
                            IManager<Guid, InvoiceEntity> manager,
                            IInvoiceManager invoiceManager,
                            IInvoiceReversalWorkflow invoiceReversalWorkflow,
                            ISalesOrderPublisher salesOrderPublisher,
                            IInvoiceWorkflowOrchestrator invoiceWorkflowOrchestrator)
        : base(log, mapper, manager)
    {
        _manager = invoiceManager;
        _invoiceReversalWorkflow = invoiceReversalWorkflow;
        _salesOrderPublisher = salesOrderPublisher;
        _invoiceWorkflowOrchestrator = invoiceWorkflowOrchestrator;
    }

    [Function(nameof(MergeInvoice))]
    public async Task MergeInvoice([ServiceBusTrigger(ServiceBusConstants.Topics.InvoiceMerge,
                                                      ServiceBusConstants.Subscriptions.InvoiceMergeRequests,
                                                      Connection = ServiceBusConstants.PrivateServiceBusNamespace)]
                                   string message,
                                   FunctionContext context)
    {
        var correlationId = context.BindingContext.BindingData.GetValueOrDefault(MessageConstants.CorrelationId);
        try
        {
            var request = JsonConvert.DeserializeObject<EntityEnvelopeModel<InvoiceMergeModel>>(message)!;
            await _manager.MergeInvoiceFiles(request.Payload);
        }
        catch (Exception e)
        {
            AppLogger.Error<LoadConfirmationFunctions>(e, $"Unable to process the invoice delivery request. (CorrelationId: '{correlationId}')");
            throw;
        }
    }

    [Function(nameof(ReverseInvoice))]
    [OpenApiOperation(nameof(ReverseInvoice), nameof(InvoiceFunctions))]
    [OpenApiRequestBody(ContentTypes.JSON, typeof(Invoice))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ContentTypes.JSON, typeof(Invoice))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.Invoice, Permissions.Operations.Write)]
    public async Task<HttpResponseData> ReverseInvoice([HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.Invoice.ReverseInvoice)] HttpRequestData request)
    {
        return await HandleRequest(request, nameof(ReverseInvoice), async response =>
                                                                    {
                                                                        try
                                                                        {
                                                                            var reverseInvoiceRequest = await request.ReadFromJsonAsync<ReverseInvoiceRequest>();
                                                                            var reversalInfo = await _invoiceReversalWorkflow.ReverseInvoice(reverseInvoiceRequest);
                                                                            await response.WriteAsJsonAsync(new ReverseInvoiceResponse
                                                                            {
                                                                                OriginalInvoiceId = reversalInfo.OriginalInvoice?.Id,
                                                                                ReversalInvoiceId = reversalInfo.ReversalInvoice?.Id,
                                                                                ProformaInvoiceId = reversalInfo.ProformaInvoice?.Id,
                                                                                ErrorMessage = reversalInfo.ErrorMessage
                                                                            });
                                                                        }
                                                                        catch (Exception e)
                                                                        {
                                                                            AppLogger.Error<InvoiceFunctions>(e);
                                                                            throw;
                                                                        }
                                                                    });
    }

    [Function(nameof(UpdateCollectionInfo))]
    [OpenApiOperation(nameof(UpdateCollectionInfo), nameof(InvoiceFunctions))]
    [OpenApiRequestBody(MediaTypeNames.Application.Json, typeof(InvoiceCollectionModel))]
    [OpenApiResponseWithoutBody(HttpStatusCode.NoContent)]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.Invoice, Permissions.Operations.Write)]
    public async Task<HttpResponseData> UpdateCollectionInfo([HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.Invoice.UpdateCollectionInfo)] HttpRequestData request)
    {
        return await HandleRequest(request,
                                   nameof(UpdateCollectionInfo),
                                   async response =>
                                   {
                                       var model = await request.ReadFromJsonAsync<InvoiceCollectionModel>();
                                       await _manager.SaveCollectionInfo(model);
                                       response.StatusCode = HttpStatusCode.NoContent;
                                   });
    }

    [Function(nameof(PostInvoiceAction))]
    [OpenApiOperation(nameof(PostInvoiceAction), nameof(InvoiceFunctions))]
    [OpenApiRequestBody(ContentTypes.JSON, typeof(Invoice))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ContentTypes.JSON, typeof(Invoice))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.Invoice, Permissions.Operations.Write)]
    public async Task<HttpResponseData> PostInvoiceAction([HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.Invoice.PostInvoiceAction)] HttpRequestData req)
    {
        return await HandleRequest(req,
                                   nameof(PostInvoiceAction),
                                   async response =>
                                   {
                                       var request = await req.ReadFromJsonAsync<PostInvoiceActionRequest>();
                                       var entity = await _manager.PostInvoiceAction(request);
                                       var invoice = Mapper.Map<Invoice>(entity);
                                       await response.WriteAsJsonAsync(invoice);
                                   });
    }

    [Function(nameof(VoidInvoice))]
    [OpenApiOperation(nameof(VoidInvoice), nameof(InvoiceFunctions))]
    [OpenApiRequestBody(ContentTypes.JSON, typeof(Invoice))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ContentTypes.JSON, typeof(Invoice))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.Invoice, Permissions.Operations.Write)]
    public async Task<HttpResponseData> VoidInvoice([HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.Invoice.VoidInvoice)] HttpRequestData req)
    {
        return await HandleRequest(req,
                                   nameof(PostInvoiceAction),
                                   async response =>
                                   {
                                       var request = await req.ReadFromJsonAsync<Invoice>();
                                       var invoiceEntity = Mapper.Map<InvoiceEntity>(request);
                                       var entity = await _invoiceWorkflowOrchestrator.VoidInvoice(invoiceEntity);
                                       var invoice = Mapper.Map<Invoice>(entity);
                                       await response.WriteAsJsonAsync(invoice);
                                   });
    }

    [Function(nameof(InvoiceAdvanceEmailAction))]
    [OpenApiOperation(nameof(InvoiceAdvanceEmailAction), nameof(InvoiceFunctions))]
    [OpenApiRequestBody(ContentTypes.JSON, typeof(Invoice))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ContentTypes.JSON, typeof(Invoice))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.Invoice, Permissions.Operations.Write)]
    public async Task<HttpResponseData> InvoiceAdvanceEmailAction([HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.Invoice.AdvancedEmail)] HttpRequestData req)
    {
        return await HandleRequest(req,
                                   nameof(InvoiceAdvanceEmailAction),
                                   async response =>
                                   {
                                       var request = await req.ReadFromJsonAsync<InvoiceAdvancedEmailRequest>();
                                       var entity = await _manager.InvoiceAdvanceEmailAction(request);
                                       var invoice = Mapper.Map<Invoice>(entity);
                                       await response.WriteAsJsonAsync(invoice);
                                   });
    }

    [Function(nameof(GetAttachmentDownloadUrl))]
    [OpenApiOperation(nameof(GetAttachmentDownloadUrl), nameof(InvoiceFunctions), Summary = Routes.Invoice.AttachmentDownload)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(string))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.Invoice, Permissions.Operations.Read)]
    public async Task<HttpResponseData> GetAttachmentDownloadUrl([HttpTrigger(AuthorizationLevel.Anonymous,
                                                                              nameof(HttpMethod.Post),
                                                                              Route = Routes.Invoice.AttachmentDownload)]
                                                                 HttpRequestData request,
                                                                 Guid id,
                                                                 string pk,
                                                                 Guid attachmentId)
    {
        return await HandleRequest(request,
                                   nameof(GetAttachmentDownloadUrl),
                                   async response =>
                                   {
                                       var uri = await _manager.GetAttachmentDownloadUrl(new(id, pk), attachmentId);
                                       await response.WriteAsJsonAsync(uri);
                                   });
    }

    [Function(nameof(GetAttachmentUploadUrl))]
    [OpenApiOperation(nameof(GetAttachmentUploadUrl), nameof(InvoiceFunctions), Summary = Routes.Invoice.AttachmentUpload)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(InvoiceAttachmentUpload))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.Invoice, Permissions.Operations.Write)]
    public async Task<HttpResponseData> GetAttachmentUploadUrl([HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.Invoice.AttachmentUpload)] HttpRequestData request,
                                                               Guid id,
                                                               string pk)
    {
        return await HandleRequest(request,
                                   nameof(GetAttachmentUploadUrl),
                                   async response =>
                                   {
                                       var attachment = await request.ReadFromJsonAsync<InvoiceAttachment>();
                                       if (attachment == null)
                                       {
                                           response.StatusCode = HttpStatusCode.BadRequest;
                                           return;
                                       }

                                       var (attachmentEntity, uri) = await _manager.GetAttachmentUploadUrl(new(id, pk), attachment.FileName, attachment.ContentType);

                                       await response.WriteAsJsonAsync(new InvoiceAttachmentUpload
                                       {
                                           Attachment = Mapper.Map<InvoiceAttachment>(attachmentEntity),
                                           Uri = uri.ToString(),
                                       });
                                   });
    }

    [Function(nameof(MarkFileUploaded))]
    [OpenApiOperation(nameof(MarkFileUploaded), nameof(InvoiceFunctions), Summary = Routes.Invoice.AttachmentMarkUploaded)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(Invoice))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.Invoice, Permissions.Operations.Write)]
    public async Task<HttpResponseData> MarkFileUploaded([HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Patch), Route = Routes.Invoice.AttachmentMarkUploaded)] HttpRequestData request,
                                                         Guid id,
                                                         string pk,
                                                         Guid attachmentId)
    {
        return await HandleRequest(request,
                                   nameof(MarkFileUploaded),
                                   async response =>
                                   {
                                       var invoiceEntity = await _manager.MarkFileUploaded(new(id, pk), attachmentId);
                                       var invoice = Mapper.Map<Invoice>(invoiceEntity);
                                       await response.WriteAsJsonAsync(invoice);
                                   });
    }

    [Function(nameof(PublishSalesOrder))]
    [OpenApiOperation(nameof(PublishSalesOrder), nameof(InvoiceFunctions), Summary = Routes.Invoice.PublishSalesOrder)]
    [OpenApiResponseWithoutBody(HttpStatusCode.OK)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.Invoice, Permissions.Operations.Write)]
    public async Task<HttpResponseData> PublishSalesOrder([HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.Invoice.PublishSalesOrder)] HttpRequestData request,
                                                          Guid id,
                                                          string pk)
    {
        return await HandleRequest(request,
                                   nameof(PublishSalesOrder),
                                   async _ =>
                                   {
                                       await _salesOrderPublisher.PublishSalesOrder(new(id, pk));
                                   });
    }
}
