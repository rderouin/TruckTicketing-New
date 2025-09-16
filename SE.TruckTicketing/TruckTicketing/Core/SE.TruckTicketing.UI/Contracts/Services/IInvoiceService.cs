using System;
using System.Threading.Tasks;

using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Models.Invoices;

using Trident.Contracts.Api;
using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.UI.Contracts.Services;

public interface IInvoiceService : IServiceBase<Invoice, Guid>
{
    Task<Response<ReverseInvoiceResponse>> ReverseInvoice(ReverseInvoiceRequest request);

    Task<Response<Invoice, TTErrorCodes>> PostInvoiceAction(PostInvoiceActionRequest postInvoiceActionRequest);

    Task<Response<Invoice, TTErrorCodes>> InvoiceAdvanceEmailAction(InvoiceAdvancedEmailRequest invoiceEmailUpdateRequest);

    Task<Response<Invoice>> VoidInvoice(Invoice invoice);

    Task<Response<string>> GetAttachmentDownloadUrl(CompositeKey<Guid> invoiceKey, Guid attachmentId);

    Task<Response<InvoiceAttachmentUpload>> GetAttachmentUploadUrl(CompositeKey<Guid> invoiceKey, string filename, string contentType);

    Task<Response<Invoice>> MarkFileUploaded(CompositeKey<Guid> invoiceKey, Guid attachmentId);

    string GetBlobPath(Guid loadConfirmationId, string filename);

    Task<Response<object>> RegenInvoicePdfs(InvoicePdfRegenRequest request);

    Task<Response<object>> UpdateCollectionInfo(InvoiceCollectionModel model);

    Task<Response<object>> PublishSalesOrder(CompositeKey<Guid> invoiceKey);
}
