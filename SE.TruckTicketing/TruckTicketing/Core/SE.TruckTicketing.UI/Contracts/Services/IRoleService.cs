using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using SE.TruckTicketing.Contracts.Models.Accounts;
using SE.TruckTicketing.UI.ViewModels;

namespace SE.TruckTicketing.UI.Contracts.Services;

public interface IRoleService : IServiceBase<Role, Guid>
{
    Task<List<PermissionViewModel>> GetPermissionList();

    Task<List<RoleViewModel>> GetRoleList();
}
