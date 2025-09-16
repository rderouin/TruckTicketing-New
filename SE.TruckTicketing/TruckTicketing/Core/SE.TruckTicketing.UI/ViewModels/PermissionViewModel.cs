using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

using Trident.UI.Client.Contracts.Models;

namespace SE.TruckTicketing.UI.ViewModels;

public class PermissionViewModel : GuidModelBase
{
    public string Name { get; set; }

    public string Display { get; set; }

    public string Code { get; set; }

    public IEnumerable<Operation> AllowedOperations { get; set; }

    [JsonIgnore]
    public IEnumerable<FlattenedPermission> Flattened =>
        AllowedOperations
           .Select(x => new FlattenedPermission
            {
                Permission = this,
                Operation = x,
            }).ToList();
}

public class Operation
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public string Value { get; set; }

    public string Display { get; set; }
}
