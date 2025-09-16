using System;
using System.Net.Http;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Service.Resources.edifieldlookup)]
public class EDIFieldLookupService : ServiceBase<EDIFieldLookupService, EDIFieldLookup, Guid>, IEDIFieldLookupService
{
    public EDIFieldLookupService(ILogger<EDIFieldLookupService> logger,
                                 IHttpClientFactory httpClientFactory)
        : base(logger, httpClientFactory)
    {
    }
}
