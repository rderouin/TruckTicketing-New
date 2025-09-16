using System;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Routes.TradeAgreementUploads.Base)]
public class TradeAgreementUploadService : ServiceBase<TradeAgreementUploadService, TradeAgreementUpload, Guid>, ITradeAgreementUploadService
{
    public TradeAgreementUploadService(ILogger<TradeAgreementUploadService> logger, IHttpClientFactory httpClientFactory) : base(logger, httpClientFactory)
    {
    }

    public async Task<TradeAgreementUpload> GetUploadUrl()
    {
        var url = Routes.TradeAgreementUploads.UploadUris;
        var response = await SendRequest<TradeAgreementUpload>(HttpMethod.Get.Method, url);
        return response.Model;
    }
}
