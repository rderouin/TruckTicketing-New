using System;

namespace SE.Shared.Domain;

public class TTAuditableEntityBase : TTEntityBase
{
    public string CreatedBy { get; set; }

    public string CreatedById { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string UpdatedBy { get; set; }

    public string UpdatedById { get; set; }
}
