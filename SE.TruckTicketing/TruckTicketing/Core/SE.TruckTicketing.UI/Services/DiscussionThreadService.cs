using System;
using System.Net.Http;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Service.Resources.note)]
public class DiscussionThreadService : ServiceBase<DiscussionThreadService, Note, Guid>, IDiscussionThreadService
{
    public DiscussionThreadService(ILogger<DiscussionThreadService> logger,
                                   IHttpClientFactory httpClientFactory)
        : base(logger, httpClientFactory)
    {
    }
}
