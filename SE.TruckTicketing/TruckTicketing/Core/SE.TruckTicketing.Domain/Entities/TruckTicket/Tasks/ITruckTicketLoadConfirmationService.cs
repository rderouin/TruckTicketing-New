using System.Threading.Tasks;

using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.Invoices;
using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.Shared.Domain.Entities.TruckTicket;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

public interface ITruckTicketLoadConfirmationService
{
    Task<LoadConfirmationEntity> GetTruckTicketLoadConfirmation(TruckTicketEntity truckTicket,
                                                                BillingConfigurationEntity billingConfig,
                                                                InvoiceEntity invoice,
                                                                int salesLineCount = 0,
                                                                double amount = 0);
}
