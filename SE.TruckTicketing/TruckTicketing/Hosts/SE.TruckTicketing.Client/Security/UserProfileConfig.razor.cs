using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Contracts.Models.Accounts;
using SE.TruckTicketing.Contracts.Security;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;
using Trident.UI.Blazor.Components.Grid;
using Trident.UI.Client.Contracts.Models;

namespace SE.TruckTicketing.Client.Security;

public partial class UserProfileConfig : BaseTruckTicketingComponent
{
    private PagableGridView<UserProfileFacilityAccess> _facilityAccessLevelGrid;

    private bool _isUpdating;

    private Response<UserProfile> _response;

    private dynamic _specificFacilityAccessLevelDialog;

    private bool _IsLoading { get; set; }

    [Inject]
    public IUserProfileService UserProfileService { get; set; }

    [Parameter]
    public Guid? UserProfileId { get; set; }

    private UserProfile UserProfile { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Inject]
    private IRoleService RoleService { get; set; }

    private ICollection<RoleModel> Roles { get; set; } = Array.Empty<RoleModel>();

    private SearchResultsModel<UserProfileFacilityAccess, SearchCriteriaModel> UserProfileFacilityAccessList =>
        UserProfile is null ? new() : new(UserProfile.SpecificFacilityAccessAssignments.OrderBy(access => access.FacilityDisplayName));

    protected override async Task OnParametersSetAsync()
    {
        _IsLoading = true;

        if ((UserProfileId is not null && UserProfileId == Guid.Empty) || UserProfile?.Id != UserProfileId)
        {
            UserProfile = await UserProfileService.GetById(UserProfileId.GetValueOrDefault());
            Roles = (await RoleService.GetAll()).Where(role => role.Enabled).Select(role => new RoleModel
            {
                Id = role.Id,
                Name = role.Name,
            }).ToArray();
        }

        _IsLoading = false;
    }

    private async Task HandleSubmit()
    {
        _isUpdating = true;
        StateHasChanged();

        _response = await UserProfileService.Update(UserProfile);
        if (_response.IsSuccessStatusCode)
        {
            NotificationService.Notify(NotificationSeverity.Success, "User update successful");
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, "User update unsuccessful");
        }

        _isUpdating = false;
        StateHasChanged();
    }

    private Task HandleRoleAssignmentChange(IEnumerable<RoleModel> roles)
    {
        UserProfile.Roles = roles.Select(role => new UserProfileRole
        {
            RoleId = role.Id,
            RoleName = role.Name,
        }).ToList();

        return Task.CompletedTask;
    }

    private async Task OpenSpecificFacilityAccessLevelDialog()
    {
        _specificFacilityAccessLevelDialog = await DialogService.OpenAsync<FacilityAccessLevelForm>("Add Specific Facility Access Level",
                                                                                                    new()
                                                                                                    {
                                                                                                        [nameof(FacilityAccessLevelForm.Model)] =
                                                                                                            new UserProfileFacilityAccess
                                                                                                            {
                                                                                                                Id = Guid.NewGuid(),
                                                                                                                IsAuthorized = true,
                                                                                                                Level = FacilityAccessLevels.InheritedRoles,
                                                                                                            },
                                                                                                        [nameof(FacilityAccessLevelForm.UserProfile)] = UserProfile,
                                                                                                        [nameof(FacilityAccessLevelForm.OnSubmit)] =
                                                                                                            new EventCallback<UserProfileFacilityAccess>(this, AddSpecificFacilityAccessLevel),
                                                                                                        [nameof(FacilityAccessLevelForm.OnCancel)] =
                                                                                                            new EventCallback(this, CloseFacilityAccessDialog),
                                                                                                    }, new());
    }

    private async Task AddSpecificFacilityAccessLevel(UserProfileFacilityAccess access)
    {
        UserProfile.SpecificFacilityAccessAssignments.Add(access);
        CloseFacilityAccessDialog();
        await _facilityAccessLevelGrid.ReloadGrid();
        StateHasChanged();
    }

    private void CloseFacilityAccessDialog()
    {
        DialogService.Close(_specificFacilityAccessLevelDialog);
    }

    private async Task RemoveSpecificFacilityAccessLevel(UserProfileFacilityAccess access)
    {
        UserProfile.SpecificFacilityAccessAssignments = UserProfile.SpecificFacilityAccessAssignments
                                                                   .Where(level => level.Id != access.Id)
                                                                   .ToList();

        await _facilityAccessLevelGrid.ReloadGrid();
        StateHasChanged();
    }
}

public class RoleModel : ModelBase<Guid>
{
    public string Name { get; set; }
}
