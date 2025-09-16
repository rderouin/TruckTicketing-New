using System.Net.Http;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Service.Resources.backend)]
public class BackendService : ServiceBase<BackendService, FeatureTogglesModel, string>, IBackendService
{
    public BackendService(ILogger<BackendService> logger, IHttpClientFactory httpClientFactory) : base(logger, httpClientFactory)
    {
    }
}
