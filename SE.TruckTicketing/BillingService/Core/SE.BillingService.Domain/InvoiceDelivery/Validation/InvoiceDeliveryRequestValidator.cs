using System.Collections.Generic;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Models.InvoiceDelivery;
using SE.Shared.Common.Extensions;

namespace SE.BillingService.Domain.InvoiceDelivery.Validation;

public class InvoiceDeliveryRequestValidator : IInvoiceDeliveryRequestValidator
{
    public Task<IList<string>> Validate(DeliveryRequest invoiceRequest)
    {
        var errors = new List<string>();

        if (invoiceRequest.GetMessageType() == null)
        {
            errors.Add("Message Type is not specified.");
        }

        if (invoiceRequest.Payload is null)
        {
            errors.Add("Payload for the invoice delivery must be provided.");
        }

        if (!invoiceRequest.Platform.HasText())
        {
            errors.Add($"{nameof(invoiceRequest.Platform)} is not provided.");
        }

        if (invoiceRequest.CustomerId == default)
        {
            errors.Add("Invoice Account has not been specified.");
        }

        if (invoiceRequest.SourceId.IsNullOrDefault())
        {
            errors.Add($"{nameof(invoiceRequest.SourceId)} is not specified.");
        }

        if (!invoiceRequest.InvoiceId.HasText())
        {
            errors.Add("Invoice ID is missing.");
        }

        return Task.FromResult((IList<string>)errors);
    }
}
