using System;
using System.Net.Http;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Service.Resources.edivalidationpatternlookup)]
public class EDIValidationPatternLookupService : ServiceBase<EDIValidationPatternLookupService, EDIValidationPatternLookup, Guid>, IEDIValidationPatternLookupService
{
    public EDIValidationPatternLookupService(ILogger<EDIValidationPatternLookupService> logger,
                                             IHttpClientFactory httpClientFactory)
        : base(logger, httpClientFactory)
    {
    }
}
