using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Client.Security;
using SE.TruckTicketing.Contracts.Models.Accounts;
using SE.TruckTicketing.Contracts.Security;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.UI.Blazor.Components;
using Trident.UI.Blazor.Components.Forms;

namespace SE.TruckTicketing.Client.Components.UserControls;

public partial class UserFacilityAccess : BaseRazorComponent
{
    private List<RadioButtonModel> _facilityAccessLevelsOptions;

    private SearchResultsModel<UserProfileFacilityAccess, SearchCriteriaModel> _userProfileFacilityAccessList = new()
    {
        Info = new() { PageSize = 10 },
        Results = new List<UserProfileFacilityAccess>(),
    };

    [Inject]
    public IUserProfileService UserProfileService { get; set; }

    [CascadingParameter]
    public UserProfile Person { get; set; }

    [Parameter]
    public EventCallback<UserProfile> OnPersonChange { get; set; }

    private string PersonName => $"{Person.FirstName} {Person.LastName}";

    private void LoadUserProfileFacilityAccessData(SearchCriteriaModel current)
    {
        //load results from server
        _userProfileFacilityAccessList = new(Person.SpecificFacilityAccessAssignments);
    }

    //private async Task OnAllFacilitiesChanged(bool? value, UserProfile request)
    private void OnAllFacilitiesChanged(ChangeEventArgs args)
    {
        if (args.Value != null)
        {
            Person.EnforceSpecificFacilityAccessLevels = bool.Parse(args.Value.ToString());
            StateHasChanged();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        SetFacilityAccessLevelOptions();
        await LoadData();
    }

    private void SetFacilityAccessLevelOptions()
    {
        _facilityAccessLevelsOptions = typeof(FacilityAccessLevels).GetFields(BindingFlags.Public | BindingFlags.Static)
                                                                   .Where(f => f.IsLiteral && !f.IsInitOnly)
                                                                   .Select(f => new RadioButtonModel
                                                                    {
                                                                        TextToDisplay = (string)f.GetRawConstantValue(),
                                                                        DataValue = (string)f.GetRawConstantValue(),
                                                                    }).ToList();
    }

    private async Task AddFacilityButton_Click()
    {
        await OpenEditDialog(new());
    }

    private async Task OpenEditDialog(UserProfileFacilityAccess model)
    {
        await DialogService.OpenAsync<UserProfileFacilityAccessEdit>("Facility Access",
                                                                     new()
                                                                     {
                                                                         { "UserProfileFacilityAccess", model },
                                                                         { "Person", Person },
                                                                     },
                                                                     new()
                                                                     {
                                                                         Width = "40%",
                                                                     });

        await LoadData();
    }

    public override void Dispose()
    {
        DialogService.OnClose -= Close;
    }

    private async Task LoadData()
    {
        //load results from server
        Person = await UserProfileService.GetById(Person.Id);
        _userProfileFacilityAccessList.Results = Person.SpecificFacilityAccessAssignments;
        _userProfileFacilityAccessList.Info.TotalRecords = Person.SpecificFacilityAccessAssignments.Count;
    }

    private void Close(dynamic result)
    {
        if (result is not UserProfileFacilityAccess facilityAccess)
        {
            return;
        }

        var resultList = _userProfileFacilityAccessList.Results.ToList();
        var index = resultList.FindIndex(x => x.FacilityId == facilityAccess.FacilityId);
        if (index >= 0)
        {
            resultList[index] = facilityAccess;
        }
        else
        {
            resultList.Add(facilityAccess);
            _userProfileFacilityAccessList.Info.TotalRecords++;
        }

        _userProfileFacilityAccessList.Results = resultList;
        Person.SpecificFacilityAccessAssignments = _userProfileFacilityAccessList.Results.ToList();
        OnPersonChange.InvokeAsync(Person);
    }

    private async Task DeleteButton_Click(UserProfileFacilityAccess userFacilityAccess)
    {
        const string msg = "Are you sure you want to delete this item?";
        const string title = "Delete Facility";
        var deleteConfirmed = await DialogService.Confirm(msg, title,
                                                          new()
                                                          {
                                                              OkButtonText = "Yes",
                                                              CancelButtonText = "No",
                                                          });

        if (deleteConfirmed.GetValueOrDefault())
        {
            var resultList = _userProfileFacilityAccessList.Results.ToList();
            var index = resultList.FindIndex(x => x.FacilityId == userFacilityAccess.FacilityId);
            resultList.RemoveAll(x => x.FacilityId == userFacilityAccess.FacilityId);
            _userProfileFacilityAccessList.Results = resultList;
            _userProfileFacilityAccessList.Info.TotalRecords++;
            Person.SpecificFacilityAccessAssignments = _userProfileFacilityAccessList.Results.ToList();
            await OnPersonChange.InvokeAsync(Person);
        }
    }
}
