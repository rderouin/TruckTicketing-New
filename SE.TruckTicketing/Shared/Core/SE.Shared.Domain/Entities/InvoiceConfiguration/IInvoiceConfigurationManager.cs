using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.TruckTicketing.Contracts.Models.InvoiceConfigurations;

using Trident.Contracts;

namespace SE.Shared.Domain.Entities.InvoiceConfiguration;

public interface IInvoiceConfigurationManager : IManager<Guid, InvoiceConfigurationEntity>
{
    Task<List<BillingConfigurationEntity>> ValidateBillingConfiguration(InvoiceConfigurationEntity invoiceConfigurationEntity);

    Task CloneInvoiceConfiguration(CloneInvoiceConfigurationModel cloneInvoiceConfiguration);
}
