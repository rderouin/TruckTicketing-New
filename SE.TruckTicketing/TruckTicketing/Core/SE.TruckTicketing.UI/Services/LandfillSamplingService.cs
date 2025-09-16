using System;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using SE.TruckTicketing.Contracts.Models.Sampling;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Routes.LandfillSampling.Base)]
public class LandfillSamplingService : ServiceBase<LandfillSamplingService, LandfillSamplingDto, Guid>, ILandfillSamplingService
{
    public LandfillSamplingService(ILogger<LandfillSamplingService> logger, IHttpClientFactory httpClientFactory) : base(logger, httpClientFactory)
    {
    }

    public async Task<LandfillSamplingStatusCheckDto> CheckStatus(LandfillSamplingStatusCheckRequestDto landfillSamplingStatusCheckRequestDto)
    {
        var response = await SendRequest<object>(HttpMethod.Post.ToString(), Routes.LandfillSampling.CheckStatus, landfillSamplingStatusCheckRequestDto);
        return JsonConvert.DeserializeObject<LandfillSamplingStatusCheckDto>(response.ResponseContent);
    }
}
