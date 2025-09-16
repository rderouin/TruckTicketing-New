using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

using SE.Shared.Common;
using SE.TruckTicketing.Contracts.Models.Navigation;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.Domain.Entities.Configuration;

using Trident.Api.Search;
using Trident.Azure.Functions;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;

namespace SE.TruckTicketing.Api.Functions;

public sealed class NavigationConfigurationFunctions : HttpFunctionApiBase<NavigationModel, NavigationConfigurationEntity, Guid>
{
    public NavigationConfigurationFunctions(ILog log,
                                            IMapperRegistry mapper,
                                            IManager<Guid, NavigationConfigurationEntity> manager)
        : base(log, mapper, manager)
    {
    }

    [Function(nameof(NavigationConfigurationSearch))]
    [OpenApiOperation(nameof(NavigationConfigurationSearch))]
    [OpenApiRequestBody(ContentTypes.JSON, typeof(SearchCriteriaModel))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ContentTypes.JSON, typeof(SearchResultsModel<NavigationModel, SearchCriteriaModel>))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    public async Task<HttpResponseData> NavigationConfigurationSearch(
        [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.NavigationConfig_SearchRoute)] HttpRequestData req)
    {
        return await Search(req, "Search Programs");
    }
}
