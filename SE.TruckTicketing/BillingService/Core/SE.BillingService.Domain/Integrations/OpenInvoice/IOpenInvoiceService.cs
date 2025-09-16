using System.Collections.Generic;
using System.Threading.Tasks;

namespace SE.BillingService.Domain.Integrations.OpenInvoice;

public interface IOpenInvoiceService
{
    Task<OpenInvoiceReceiptsResult> QueryReceiptsAsync(IList<string> receiptNumbers);
}
