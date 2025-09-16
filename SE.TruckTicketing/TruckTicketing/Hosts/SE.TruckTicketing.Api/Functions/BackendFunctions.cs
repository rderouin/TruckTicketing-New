using System;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

using SE.Shared.Common;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Routes;

using Trident.Azure.Functions;
using Trident.Contracts.Configuration;
using Trident.Logging;

namespace SE.TruckTicketing.Api.Functions;

public class BackendFunctions : IFunctionController
{
    private readonly Lazy<FeatureToggles> _lazyToggles;

    private readonly ILog _log;

    public BackendFunctions(IAppSettings appSettings, ILog log)
    {
        _log = log;
        _lazyToggles = FeatureToggles.Init(appSettings);
    }

    [Function(nameof(GetToggles))]
    public async Task<HttpResponseData> GetToggles([HttpTrigger(AuthorizationLevel.Anonymous,
                                                                nameof(HttpMethod.Get),
                                                                Route = Routes.Backend_Toggles)]
                                                   HttpRequestData httpRequestData)
    {
        HttpResponseData httpResponseData;

        try
        {
            httpResponseData = httpRequestData.CreateResponse(HttpStatusCode.OK);
            await httpResponseData.WriteAsJsonAsync(new FeatureTogglesModel { FeatureToggles = _lazyToggles.Value });
        }
        catch (Exception e)
        {
            _log.Error(exception: e);
            httpResponseData = httpRequestData.CreateResponse(HttpStatusCode.InternalServerError);
        }

        return httpResponseData;
    }
}
