using System;

namespace SE.TruckTicketing.Contracts.Models.Operations;

public class AccountContactReferenceIndex : GuidApiModelBase
{
    public Guid ReferenceEntityId { get; set; }

    public string ReferenceEntityName { get; set; }

    public Guid? AccountContactId { get; set; }

    public Guid AccountId { get; set; }
}
