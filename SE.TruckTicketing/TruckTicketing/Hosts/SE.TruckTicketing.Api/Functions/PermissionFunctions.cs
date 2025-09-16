using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

using SE.Shared.Domain.Entities.Permission;
using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Models.Accounts;
using SE.TruckTicketing.Contracts.Routes;

using Trident.Azure.Functions;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;

namespace SE.TruckTicketing.Api.Functions;

public sealed class PermissionFunctions : HttpFunctionApiBase<Permission, PermissionEntity, Guid>
{
    public PermissionFunctions(ILog log,
                               IMapperRegistry mapper,
                               IManager<Guid, PermissionEntity> manager)
        : base(log, mapper, manager)
    {
    }

    [Function("GetPermissions")]
    [OpenApiOperation("GetPermissions", nameof(PermissionFunctions), Summary = nameof(RouteTypes.GetAll))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(IEnumerable<Permission>))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    public async Task<HttpResponseData> GetPermissions([HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Get), Route = Routes.Permission_BaseRoute)] HttpRequestData req)
    {
        return await Get(req, x => x.EntityType == Containers.Discriminators.Permission, "GetPermissions");
    }
}
