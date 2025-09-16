using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Models.Accounts;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels;

using Trident.Mapper;
using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Service.Resources.roles)]
public class RoleService : ServiceBase<RoleService, Role, Guid>, IRoleService
{
    private readonly IMapperRegistry _mapper;

    public RoleService(ILogger<RoleService> logger,
                       IHttpClientFactory httpClientFactory,
                       IMapperRegistry mapper)
        : base(logger, httpClientFactory)
    {
        _mapper = mapper;
    }

    public async Task<List<PermissionViewModel>> GetPermissionList()
    {
        Logger.LogInformation("Get Permission List - Calling API");

        var response = await SendRequest<List<PermissionViewModel>>(nameof(HttpMethod.Get), Routes.Permission_BaseRoute);
        Logger.LogInformation("Get Permission List - API Reponse");

        return response?.Model ?? new List<PermissionViewModel>();
    }

    public async Task<List<RoleViewModel>> GetRoleList()
    {
        var response = await SendRequest<List<Role>>(nameof(HttpMethod.Get), Routes.Role_BaseRoute);

        return _mapper.Map<List<RoleViewModel>>(response?.Model) ?? new List<RoleViewModel>();
    }
}
