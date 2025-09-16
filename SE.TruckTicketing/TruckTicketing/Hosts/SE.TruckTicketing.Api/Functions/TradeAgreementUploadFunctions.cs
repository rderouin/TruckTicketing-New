using System;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.Contracts.Security;
using SE.TruckTicketing.Domain.Entities.TradeAgreementUploads;

using Trident.Azure.Functions;
using Trident.Azure.Security;
using Trident.Extensions.OpenApi.Attributes;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions;

[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.TradeAgreementUploads.Search,
                 ClaimsAuthorizeResource = Permissions.Resources.TradeAgreement,
                 ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Create, Route = Routes.TradeAgreementUploads.Base,
                 ClaimsAuthorizeResource = Permissions.Resources.TradeAgreement,
                 ClaimsAuthorizeOperation = Permissions.Operations.Upload)]
public partial class TradeAgreementUploadFunctions : HttpFunctionApiBase<TradeAgreementUpload, TradeAgreementUploadEntity, Guid>
{
    private readonly ITradeAgreementUploadManager _manager;

    public TradeAgreementUploadFunctions(ILog log, IMapperRegistry mapper, ITradeAgreementUploadManager manager) : base(log, mapper, manager)
    {
        _manager = manager;
    }

    [Function(nameof(GetTradeAgreementUploadUri))]
    [OpenApiOperation(nameof(GetTradeAgreementUploadUri), nameof(TradeAgreementUploadFunctions), Summary = Routes.TradeAgreementUploads.UploadUris)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(TradeAgreementUploadEntity))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.TradeAgreement, Permissions.Operations.Upload)]
    public async Task<HttpResponseData> GetTradeAgreementUploadUri([HttpTrigger(AuthorizationLevel.Anonymous,
                                                                                nameof(HttpMethod.Get),
                                                                                Route = Routes.TradeAgreementUploads.UploadUris)]
                                                                   HttpRequestData httpRequestData,
                                                                   string path)
    {
        return await HandleRequest(httpRequestData,
                                   nameof(GetTradeAgreementUploadUri),
                                   async response =>
                                   {
                                       var tradeAgreement = _manager.GetUploadUri();
                                       await response.WriteAsJsonAsync(tradeAgreement);
                                   });
    }
}
