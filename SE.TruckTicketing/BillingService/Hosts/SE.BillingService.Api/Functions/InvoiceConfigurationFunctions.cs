using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

using SE.Shared.Common;
using SE.Shared.Domain.Entities.InvoiceConfiguration;
using SE.TruckTicketing.Contracts.Models.InvoiceConfigurations;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.Contracts.Security;

using Trident.Azure.Functions;
using Trident.Azure.Security;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.BillingService.Api.Functions;

[UseHttpFunction(HttpFunctionApiMethod.GetById,
                 Route = Routes.InvoiceConfiguration_IdRouteTemplate,
                 ClaimsAuthorizeResource = Permissions.Resources.Account,
                 ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Search,
                 Route = Routes.InvoiceConfiguration_SearchRoute,
                 ClaimsAuthorizeResource = Permissions.Resources.Account,
                 ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Create,
                 Route = Routes.InvoiceConfiguration_BaseRoute,
                 ClaimsAuthorizeResource = Permissions.Resources.Account,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Update,
                 Route = Routes.InvoiceConfiguration_IdRouteTemplate,
                 ClaimsAuthorizeResource = Permissions.Resources.Account,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Patch,
                 Route = Routes.InvoiceConfiguration_IdRouteTemplate,
                 ClaimsAuthorizeResource = Permissions.Resources.Account,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
public partial class InvoiceConfigurationFunctions : HttpFunctionApiBase<InvoiceConfiguration, InvoiceConfigurationEntity, Guid>
{
    private readonly IInvoiceConfigurationManager _invoiceConfigurationManager;

    private readonly IMapperRegistry _mapper;

    public InvoiceConfigurationFunctions(ILog log,
                                         IMapperRegistry mapper,
                                         IInvoiceConfigurationManager manager)
        : base(log, mapper, manager)
    {
        _invoiceConfigurationManager = manager;
        _mapper = mapper;
    }

    [Function(nameof(GetInvalidBillingConfiguration))]
    [OpenApiOperation(nameof(GetInvalidBillingConfiguration), nameof(InvoiceConfigurationFunctions), Summary = nameof(Routes.InvoiceConfiguration_Invalid_BillingConfiguration))]
    [OpenApiRequestBody(ContentTypes.JSON, typeof(InvoiceConfiguration))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ContentTypes.JSON, typeof(InvoiceConfiguration))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.Account, Permissions.Operations.Read)]
    public async Task<HttpResponseData> GetInvalidBillingConfiguration(
        [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Get), nameof(HttpMethod.Post), Route = Routes.InvoiceConfiguration_Invalid_BillingConfiguration)] HttpRequestData req)
    {
        return await HandleRequest(req, nameof(GetInvalidBillingConfiguration), async response =>
                                                                                {
                                                                                    var request = await req.ReadFromJsonAsync<InvoiceConfiguration>();
                                                                                    var entity = _mapper.Map<InvoiceConfigurationEntity>(request);
                                                                                    var billingConfigurationEntities = await _invoiceConfigurationManager.ValidateBillingConfiguration(entity);
                                                                                    if (billingConfigurationEntities != null && billingConfigurationEntities.Any())
                                                                                    {
                                                                                        var models = Mapper.Map<IEnumerable<BillingConfiguration>>(billingConfigurationEntities);
                                                                                        await response.WriteAsJsonAsync(models);
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        response.StatusCode = HttpStatusCode.NotFound;
                                                                                    }
                                                                                });
    }

    [Function(nameof(CloneInvoiceConfiguration))]
    [OpenApiOperation(nameof(CloneInvoiceConfiguration), nameof(InvoiceConfigurationFunctions), Summary = nameof(RouteTypes.Create))]
    [OpenApiRequestBody(ContentTypes.JSON, typeof(CloneInvoiceConfigurationModel))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ContentTypes.JSON, typeof(CloneInvoiceConfigurationModel))]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.Account, Permissions.Operations.Write)]
    public async Task<HttpResponseData> CloneInvoiceConfiguration([HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.InvoiceConfiguration_Clone)] HttpRequestData req)
    {
        return await HandleRequest(req, nameof(CloneInvoiceConfiguration), async response =>
                                                                           {
                                                                               var request = await req.ReadFromJsonAsync<CloneInvoiceConfigurationModel>();
                                                                               await _invoiceConfigurationManager.CloneInvoiceConfiguration(request);
                                                                           });
    }
}
