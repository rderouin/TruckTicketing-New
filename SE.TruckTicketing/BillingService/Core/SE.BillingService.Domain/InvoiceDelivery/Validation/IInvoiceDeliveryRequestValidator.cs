using System.Collections.Generic;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Models.InvoiceDelivery;

namespace SE.BillingService.Domain.InvoiceDelivery.Validation;

public interface IInvoiceDeliveryRequestValidator
{
    Task<IList<string>> Validate(DeliveryRequest invoiceRequest);
}
