using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Newtonsoft.Json;

using SE.TruckTicketing.Contracts.Models.Accounts;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Security;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;
using Trident.Contracts.Enums;
using Trident.Mapper;
using Trident.UI.Blazor.Components;
using Trident.UI.Blazor.Components.Forms;
using Trident.UI.Blazor.Components.Modals;

namespace SE.TruckTicketing.Client.Security;

public partial class UserProfileFacilityAccessEdit : BaseRazorComponent
{
    private List<RadioButtonModel> _facilityAccessLevelsOptions;

    private List<SelectOption> _facilityList = new();

    [Inject]
    public IMapperRegistry Mapper { get; set; }

    [Inject]
    private IFacilityService FacilityService { get; set; }

    [Inject]
    private IUserProfileService UserProfileService { get; set; }

    [Parameter]
    public UserProfileFacilityAccess userProfileFacilityAccess { get; set; }

    [Parameter]
    public UserProfile Person { get; set; }

    private List<Facility> AllFacilities { get; set; }

    public void OnFacilityChanged(string facilityId)
    {
        try
        {
            var facilityGuid = new Guid(facilityId);
            var _facility = AllFacilities.FirstOrDefault(facility => facility.Id == facilityGuid);
            userProfileFacilityAccess.FacilityId = facilityId;
            userProfileFacilityAccess.FacilityDisplayName = String.Format("{0}-{1}-{2}-{3}", _facility.Name, _facility.SiteId, _facility.Type, _facility.LegalEntity);
            StateHasChanged();
        }
        catch (Exception e)
        {
            Console.WriteLine($"An exception occurred getting a facility that matches this id={facilityId}. The exception was {e}");
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        var criteria = new SearchCriteriaModel
        {
            PageSize = 10,
            CurrentPage = 0,
            Keywords = "",
            OrderBy = "",
            Filters = new() { { "EntityType", "Facility" } },
            SortOrder = SortOrder.Asc,
        };

        var facilities = await FacilityService.Search(criteria);

        AllFacilities = facilities.Results.ToList();

        _facilityList = facilities.Results.Where(x => !Person.SpecificFacilityAccessAssignments.Select(c => c.FacilityId).Contains(x.Id.ToString())).Select(f => new SelectOption
        {
            Text = f.Name,
            Id = f.Id.ToString(),
        }).ToList();

        _facilityAccessLevelsOptions = typeof(FacilityAccessLevels).GetFields(BindingFlags.Public | BindingFlags.Static)
                                                                   .Where(f => f.IsLiteral && !f.IsInitOnly)
                                                                   .Select(f => new RadioButtonModel
                                                                    {
                                                                        TextToDisplay = (string)f.GetRawConstantValue(),
                                                                        DataValue = (string)f.GetRawConstantValue(),
                                                                    }).ToList();
    }

    private async Task SaveButton_Clicked()
    {
        Response<UserProfile> response;

        Person.SpecificFacilityAccessAssignments.Add(userProfileFacilityAccess);
        response = await UserProfileService.Update(Person);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            Person = response.Model;
            DialogService.Close(userProfileFacilityAccess);
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
        DialogService.Close(userProfileFacilityAccess);
        return Task.CompletedTask;
    }
}
