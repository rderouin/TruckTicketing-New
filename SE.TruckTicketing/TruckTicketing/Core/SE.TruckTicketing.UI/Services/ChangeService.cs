using System.Net.Http;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Service.Resources.changes)]
public class ChangeService : ServiceBase<ChangeService, Change, string>, IChangeService
{
    public ChangeService(ILogger<ChangeService> logger,
                         IHttpClientFactory httpClientFactory)
        : base(logger, httpClientFactory)
    {
    }
}
