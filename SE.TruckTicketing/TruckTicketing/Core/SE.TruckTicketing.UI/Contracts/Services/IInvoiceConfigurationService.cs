using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using SE.TruckTicketing.Contracts.Models.InvoiceConfigurations;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.UI.Contracts.Services;

public interface IInvoiceConfigurationService : IServiceBase<InvoiceConfiguration, Guid>
{
    Task<List<BillingConfiguration>> GetInvalidBillingConfiguration(InvoiceConfiguration invoiceConfiguration);

    Task<Response<CloneInvoiceConfigurationModel>> CloneInvoiceConfiguration(CloneInvoiceConfigurationModel model);
}
