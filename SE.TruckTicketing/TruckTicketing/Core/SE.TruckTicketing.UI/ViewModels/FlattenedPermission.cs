using Newtonsoft.Json;

using Trident.UI.Client.Contracts.Models;

namespace SE.TruckTicketing.UI.ViewModels;

public class FlattenedPermission : ModelBase<string>
{
    [JsonIgnore]
    public PermissionViewModel Permission { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public Operation Operation { get; set; }

    public override string Id => $"{Permission.Id}|{Operation.Id}";

    public string Display => $"{Operation.Display} {Permission.Display}";
}
