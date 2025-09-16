using System.Collections.Generic;

using Newtonsoft.Json;

namespace SE.TruckTicketing.Contracts.Models.Accounts;

public class Role : GuidApiModelBase
{
    public string Name { get; set; }

    public bool Deleted { get; set; }

    public List<PermissionLookup> Permissions { get; set; }

    public string PermissionDisplay { get; set; }

    [JsonIgnore]
    public bool Enabled { get => !Deleted; set => Deleted = !value; }
}
