using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Models.Invoices;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Contracts.Api;
using Trident.Contracts.Api.Client;
using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Service.Resources.invoices)]
public class InvoiceService : ServiceBase<InvoiceService, Invoice, Guid>, IInvoiceService
{
    public InvoiceService(ILogger<InvoiceService> logger, IHttpClientFactory httpClientFactory) : base(logger, httpClientFactory)
    {
    }

    public async Task<Response<Invoice, TTErrorCodes>> PostInvoiceAction(PostInvoiceActionRequest postInvoiceActionRequest)
    {
        var url = Routes.Invoice.PostInvoiceAction;
        return await SendRequest<Invoice, TTErrorCodes>(HttpMethod.Post.ToString(), url, postInvoiceActionRequest);
    }

    public async Task<Response<Invoice, TTErrorCodes>> InvoiceAdvanceEmailAction(InvoiceAdvancedEmailRequest emailUpdateRequest)
    {
        return await SendRequest<Invoice, TTErrorCodes>(HttpMethod.Post.ToString(), Routes.Invoice.AdvancedEmail, emailUpdateRequest);
    }

    public async Task<Response<Invoice>> VoidInvoice(Invoice invoiceRequest)
    {
        return await SendRequest<Invoice>(HttpMethod.Post.ToString(), Routes.Invoice.VoidInvoice, invoiceRequest);
    }

    public async Task<Response<ReverseInvoiceResponse>> ReverseInvoice(ReverseInvoiceRequest request)
    {
        return await SendRequest<ReverseInvoiceResponse>(HttpMethod.Post.Method, Routes.Invoice.ReverseInvoice, request);
    }

    public async Task<Response<string>> GetAttachmentDownloadUrl(CompositeKey<Guid> invoiceKey, Guid attachmentId)
    {
        var uri = Routes.Invoice.AttachmentDownload
                        .Replace(Routes.Invoice.Parameters.Id, invoiceKey.Id.ToString())
                        .Replace(Routes.Invoice.Parameters.AttachmentId, attachmentId.ToString())
                        .Replace(Routes.Invoice.Parameters.Pk, invoiceKey.PartitionKey);

        return await SendRequest<string>(HttpMethod.Post.ToString(), uri);
    }

    public async Task<Response<InvoiceAttachmentUpload>> GetAttachmentUploadUrl(CompositeKey<Guid> invoiceKey, string filename, string contentType)
    {
        var newAttachment = new InvoiceAttachment
        {
            FileName = filename,
            ContentType = contentType,
        };

        var uri = new Dictionary<string, string>
        {
            [Routes.Invoice.Parameters.Id] = $"{invoiceKey.Id}",
            [Routes.Invoice.Parameters.Pk] = $"{invoiceKey.PartitionKey}",
        }.Aggregate(Routes.Invoice.AttachmentUpload, (current, next) => current.Replace(next.Key, next.Value));

        return await SendRequest<InvoiceAttachmentUpload>(HttpMethod.Post.Method, uri, newAttachment);
    }

    public async Task<Response<Invoice>> MarkFileUploaded(CompositeKey<Guid> invoiceKey, Guid attachmentId)
    {
        var uri = new Dictionary<string, string>
        {
            [Routes.Invoice.Parameters.Id] = $"{invoiceKey.Id}",
            [Routes.Invoice.Parameters.AttachmentId] = $"{attachmentId}",
            [Routes.Invoice.Parameters.Pk] = $"{invoiceKey.PartitionKey}",
        }.Aggregate(Routes.Invoice.AttachmentMarkUploaded, (current, next) => current.Replace(next.Key, next.Value));

        return await SendRequest<Invoice>(HttpMethod.Patch.Method, uri);
    }

    public string GetBlobPath(Guid loadConfirmationId, string filename)
    {
        throw new NotImplementedException();
    }

    public Task<Response<object>> RegenInvoicePdfs(InvoicePdfRegenRequest request)
    {
        throw new NotImplementedException();
    }

    public async Task<Response<object>> UpdateCollectionInfo(InvoiceCollectionModel model)
    {
        var uri = Routes.Invoice.UpdateCollectionInfo;
        return await SendRequest<object>(HttpMethod.Post.Method, uri, model);
    }

    public async Task<Response<object>> PublishSalesOrder(CompositeKey<Guid> invoiceKey)
    {
        var uri = Routes.Invoice.PublishSalesOrder
                        .Replace(Routes.Invoice.Parameters.Id, invoiceKey.Id.ToString())
                        .Replace(Routes.Invoice.Parameters.Pk, invoiceKey.PartitionKey);

        return await SendRequest<object>(HttpMethod.Post.Method, uri);
    }
}
