using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using SE.TruckTicketing.Contracts.Models.InvoiceConfigurations;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Contracts.Api.Client;
using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SEBillingServiceApi, Service.Resources.invoiceconfiguration)]
public class InvoiceConfigurationService : ServiceBase<InvoiceConfigurationService, InvoiceConfiguration, Guid>, IInvoiceConfigurationService
{
    public InvoiceConfigurationService(ILogger<InvoiceConfigurationService> logger,
                                       IHttpClientFactory httpClientFactory)
        : base(logger, httpClientFactory)
    {
    }

    public async Task<List<BillingConfiguration>> GetInvalidBillingConfiguration(InvoiceConfiguration invoiceConfiguration)
    {
        var url = Routes.InvoiceConfiguration_Invalid_BillingConfiguration;
        var response = await SendRequest<object>(HttpMethod.Post.ToString(), url, invoiceConfiguration);

        return JsonConvert.DeserializeObject<List<BillingConfiguration>>(response.ResponseContent);
    }

    public async Task<Response<CloneInvoiceConfigurationModel>> CloneInvoiceConfiguration(CloneInvoiceConfigurationModel model)
    {
        var response = await SendRequest<CloneInvoiceConfigurationModel>(HttpMethod.Post.Method, Routes.InvoiceConfiguration_Clone, model);
        return response;
    }
}
