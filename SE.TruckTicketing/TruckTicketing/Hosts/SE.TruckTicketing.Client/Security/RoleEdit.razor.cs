using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Newtonsoft.Json;

using SE.TruckTicketing.Contracts.Models.Accounts;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels;

using Trident.Contracts.Api.Client;
using Trident.Mapper;
using Trident.UI.Blazor.Components;
using Trident.UI.Blazor.Components.Modals;

namespace SE.TruckTicketing.Client.Security;

public partial class RoleEdit : BaseRazorComponent
{
    private IEnumerable<string> selectedValues = new List<string>();

    private List<FlattenedPermission> FlatPermissions { get; set; } = new();

    [Inject]
    public IMapperRegistry Mapper { get; set; }

    [Inject]
    private IRoleService RoleService { get; set; }

    [Parameter]
    public RoleViewModel Role { get; set; }

    private bool Enabled { get => !Role.Deleted; set => Role.Deleted = !value; }

    protected override async Task OnInitializedAsync()
    {
        var permissionsList = await RoleService.GetPermissionList();
        FlatPermissions = permissionsList.SelectMany(x => x.Flattened).ToList();
        selectedValues = Role.Permissions.SelectMany(x => x.Flattened).Select(x => x.Id);
    }

    private void SelectionChanged(IEnumerable<FlattenedPermission> selectedPermissions)
    {
        var perms = new List<PermissionViewModel>();
        var rolePermsGroup = selectedPermissions.GroupBy(x => x.Permission);
        foreach (var item in rolePermsGroup)
        {
            var perm = Role.Permissions.FirstOrDefault(x => x.Id == item.Key.Id)
                    ?? JsonConvert.DeserializeObject<PermissionViewModel>(JsonConvert.SerializeObject(item.Key));

            perm.AllowedOperations = item.Select(x => x.Operation).ToList();
            perms.Add(perm);
        }

        Role.Permissions.Clear();
        Role.Permissions.AddRange(perms);
    }

    private async Task SaveButton_Clicked()
    {
        Response<Role> response;

        if (Role.Id == Guid.Empty)
        {
            response = await RoleService.Create(Mapper.Map<Role>(Role));
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Role = Mapper.Map<RoleViewModel>(response.Model);
            }
        }
        else
        {
            response = await RoleService.Update(Mapper.Map<Role>(Role));
        }

        if (response.StatusCode == HttpStatusCode.OK)
        {
            DialogService.Close(Role);
        }
        else
        {
            var json = JsonConvert.SerializeObject(response.ValidationErrors);
            DialogService.Open<ErrorAlert>("Error", new()
            {
                { nameof(ErrorAlert.Response), json },
                { nameof(Application), Application },
            });
        }
    }

    private Task CancelButton_Clicked()
    {
        DialogService.Close(Role);
        return Task.CompletedTask;
    }
}
