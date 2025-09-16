using System;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Models.SourceLocations;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Contracts.Api.Client;
using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Routes.SourceLocation_BaseRoute)]
public class SourceLocationService : ServiceBase<SourceLocationService, SourceLocation, Guid>, ISourceLocationService
{
    public SourceLocationService(ILogger<SourceLocationService> logger, IHttpClientFactory httpClientFactory)
        : base(logger, httpClientFactory)
    {
    }

    public async Task<Response<SourceLocation>> MarkSourceLocationDeleted(Guid sourceLocationId)
    {
        var url = Routes.SourceLocation_MarkDelete.Replace("{id}", sourceLocationId.ToString());
        return await SendRequest<SourceLocation>(HttpMethod.Patch.Method, url);
    }
}
