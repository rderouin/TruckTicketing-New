using System;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Models.InvoiceDelivery;
using SE.TruckTicketing.Contracts.Models.LoadConfirmations;

using Trident.Contracts.Api;

namespace SE.Shared.Domain.Entities.LoadConfirmation;

public interface ILoadConfirmationApprovalWorkflow
{
    Task DoLoadConfirmationAction(LoadConfirmationSingleRequest request);

    public Task StartFromBeginning(CompositeKey<Guid> loadConfirmationKey, string additionalNotes, bool ignoreCurrentStatus);

    public Task ContinueFromApprovalEmail(LoadConfirmationApprovalEmail approvalEmail);

    public Task ProcessResponseFromInvoiceGateway(DeliveryResponse invoiceResponse);

    public Task<Uri> FetchLatestDocument(CompositeKey<Guid> loadConfirmationKey, string contentDisposition);
}
