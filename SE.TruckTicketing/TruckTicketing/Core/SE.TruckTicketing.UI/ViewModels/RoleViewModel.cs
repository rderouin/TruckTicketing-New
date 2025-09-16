using System.Collections.Generic;
using System.Web;

using Newtonsoft.Json;

using SE.TruckTicketing.Contracts.Models;

namespace SE.TruckTicketing.UI.ViewModels;

public class RoleViewModel : GuidApiModelBase
{
    public string Name { get; set; }

    public bool Deleted { get; set; }

    public List<PermissionViewModel> Permissions { get; set; } = new();

    public string PermissionDisplay { get; set; }

    [JsonIgnore]
    public string UriPermissions
    {
        get => HttpUtility.UrlEncode(JsonConvert.SerializeObject(Permissions));
        set => Permissions = JsonConvert.DeserializeObject<List<PermissionViewModel>>(HttpUtility.UrlDecode(value));
    }

    [JsonIgnore]
    public string Status => Deleted ? "Inactive" : "Active";

    public bool SubmitButtonDisabled { get; set; }

    public void Update(RoleViewModel role)
    {
        Name = role.Name;
        Permissions = role.Permissions;
        Deleted = role.Deleted;
    }
}
