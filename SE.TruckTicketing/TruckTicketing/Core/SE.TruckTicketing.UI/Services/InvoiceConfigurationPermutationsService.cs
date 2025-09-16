using System;
using System.Net.Http;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Models.InvoiceConfigurations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SEBillingServiceApi, Service.Resources.invoiceconfigurationpermutations)]
public class InvoiceConfigurationPermutationsService : ServiceBase<InvoiceConfigurationPermutationsService, InvoiceConfigurationPermutationsIndex, Guid>, IInvoiceConfigurationPermutationsService
{
    public InvoiceConfigurationPermutationsService(ILogger<InvoiceConfigurationPermutationsService> logger,
                                                   IHttpClientFactory httpClientFactory)
        : base(logger, httpClientFactory)
    {
    }
}
