using System;
using System.Net.Http;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Api.Models;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Service.Resources.BusinessStream)]
public class BusinessStreamService : ServiceBase<BusinessStreamService, BusinessStream, Guid>, IBusinessStreamService
{
    public BusinessStreamService(ILogger<BusinessStreamService> logger, IHttpClientFactory httpClientFactory) : base(logger, httpClientFactory)
    {
    }
}
