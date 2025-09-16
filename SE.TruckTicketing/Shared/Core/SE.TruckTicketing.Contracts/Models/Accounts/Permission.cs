using System;
using System.Collections.Generic;

using Trident.Contracts.Api;

namespace SE.TruckTicketing.Contracts.Models.Accounts;

public class Permission : GuidApiModelBase
{
    public string Name { get; set; }

    public string Display { get; set; }

    public string Code { get; set; }

    public IEnumerable<Operation> AllowedOperations { get; set; }
}

public class Operation : ApiModelBase<Guid>
{
    public string Name { get; set; }

    public string Value { get; set; }

    public string Display { get; set; }
}
