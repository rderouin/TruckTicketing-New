using System;
using System.Threading.Tasks;

using Trident.Contracts;

namespace SE.BillingService.Domain.Entities.InvoiceDelivery;

public interface IInvoiceDeliveryRequestManager : IManager<Guid, InvoiceDeliveryRequestEntity>
{
    Task ProcessRemoteStatusUpdates();
}
