using System;
using System.Net.Http;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Service.Resources.volumechange)]
public class VolumeChangeService : ServiceBase<VolumeChangeService, VolumeChange, Guid>, IVolumeChangeService
{
    public VolumeChangeService(ILogger<VolumeChangeService> logger, IHttpClientFactory httpClientFactory)
        : base(logger, httpClientFactory)
    {
    }
}
