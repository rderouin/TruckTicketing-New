using System;
using System.Collections.Generic;

using Trident.Contracts.Api;

namespace SE.TruckTicketing.Contracts.Models.Accounts;

public class PermissionLookup : ApiModelBase<Guid>
{
    public string Code { get; set; }

    public string Name { get; set; }

    public string Display { get; set; }

    public List<Operation> AllowedOperations { get; set; } = new();
}
