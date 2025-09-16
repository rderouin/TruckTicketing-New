using System;

namespace SE.TruckTicketing.Contracts.Models.Operations;

public class Note : GuidApiModelBase
{
    public string Comment { get; set; }

    public string CreatedBy { get; set; }

    public string CreatedById { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string UpdatedById { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string ThreadId { get; set; }

    public bool NotEditable { get; set; }
}
