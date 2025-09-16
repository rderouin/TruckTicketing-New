using System;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Contracts.Api.Client;
using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Service.Resources.materialapproval)]
public class MaterialApprovalService : ServiceBase<MaterialApprovalService, MaterialApproval, Guid>, IMaterialApprovalService
{
    public MaterialApprovalService(ILogger<MaterialApprovalService> logger,
                                   IHttpClientFactory httpClientFactory)
        : base(logger, httpClientFactory)
    {
    }

    public async Task<Response<object>> DownloadMaterialApprovalPdf(Guid materialApprovalId)
    {
        var url = Routes.MaterialApprovalPdf.Replace("{id}", materialApprovalId.ToString());
        var response = await SendRequest<object>(HttpMethod.Post.ToString(), url);

        return response;
    }

    public async Task<Response<object>> DownloadMaterialApprovalScaleTicket(Guid materialApprovalId)
    {
        var url = Routes.MaterialApproval_DownloadScaleTicketStub.Replace("{id}", materialApprovalId.ToString());
        var response = await SendRequest<object>(HttpMethod.Post.ToString(), url);

        return response;
    }
}
