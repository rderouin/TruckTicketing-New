using System.Threading.Tasks;

using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.Invoices;
using SE.Shared.Domain.Entities.TruckTicket;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

public interface ITruckTicketInvoiceService
{
    Task<InvoiceEntity> GetTruckTicketInvoice(TruckTicketEntity truckTicket, BillingConfigurationEntity billingConfig, int salesLineCount = 0, double amount = 0);
    Task<string> EvaluateInvoiceConfigurationThreshold(TruckTicketEntity truckTicket, BillingConfigurationEntity billingConfig, int salesLineCount = 0, double salesLineTotalValue = 0);

}
