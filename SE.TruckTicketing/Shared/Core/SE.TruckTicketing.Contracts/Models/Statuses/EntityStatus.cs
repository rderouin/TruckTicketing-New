using System;

using Trident.Contracts.Api;

namespace SE.TruckTicketing.Contracts.Models.Statuses;

public class EntityStatus : GuidApiModelBase
{
    public string ReferenceEntityType { get; set; }

    public CompositeKey<Guid> ReferenceEntityKey { get; set; }

    public string Status { get; set; }
}
