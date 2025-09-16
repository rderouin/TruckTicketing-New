using System;
using System.Threading.Tasks;

using Trident.Contracts;

namespace SE.BillingService.Domain.Entities.InvoiceExchange;

public interface IInvoiceExchangeManager : IManager<Guid, InvoiceExchangeEntity>
{
    Task<InvoiceExchangeEntity> GetFinalInvoiceExchangeConfig(string platformCode, Guid customerId);
}
