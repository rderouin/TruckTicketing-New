using System;
using System.Net.Http;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Service.Resources.products)]
public class ProductService : ServiceBase<ProductService, Product, Guid>, IProductService
{
    public ProductService(ILogger<ProductService> logger,
                          IHttpClientFactory httpClientFactory)
        : base(logger, httpClientFactory)
    {
    }
}
