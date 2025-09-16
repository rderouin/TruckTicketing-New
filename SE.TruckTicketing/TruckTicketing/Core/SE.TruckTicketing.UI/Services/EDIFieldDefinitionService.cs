using System;
using System.Net.Http;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Service.Resources.edifielddefinition)]
public class EDIFieldDefinitionService : ServiceBase<EDIFieldDefinitionService, EDIFieldDefinition, Guid>, IEDIFieldDefinitionService
{
    public EDIFieldDefinitionService(ILogger<EDIFieldDefinitionService> logger,
                                     IHttpClientFactory httpClientFactory)
        : base(logger, httpClientFactory)
    {
    }
}
