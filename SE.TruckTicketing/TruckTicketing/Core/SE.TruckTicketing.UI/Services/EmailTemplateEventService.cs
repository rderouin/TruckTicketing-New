using System;
using System.Net.Http;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Models.ContentGeneration;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Routes.EmailTemplateEvents.Base)]
public class EmailTemplateEventService : ServiceBase<EmailTemplateEventService, EmailTemplateEvent, Guid>
{
    public EmailTemplateEventService(ILogger<EmailTemplateEventService> logger, IHttpClientFactory httpClientFactory) : base(logger, httpClientFactory)
    {
    }
}
