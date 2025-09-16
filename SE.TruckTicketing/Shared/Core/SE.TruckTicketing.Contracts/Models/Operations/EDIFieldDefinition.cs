using System;

namespace SE.TruckTicketing.Contracts.Models.Operations;

public class EDIFieldDefinition : GuidApiModelBase
{
    public Guid CustomerId { get; set; }

    public Guid EDIFieldLookupId { get; set; }

    public string EDIFieldName { get; set; }

    public string DefaultValue { get; set; }

    public bool IsRequired { get; set; }

    public bool IsPrinted { get; set; }

    public bool ValidationRequired { get; set; }

    public bool IsNew { get; set; }

    public Guid ValidationPatternId { get; set; }

    public string ValidationPattern { get; set; }

    public string ValidationErrorMessage { get; set; }

    public string LegalEntity { get; set; }

    public string CreatedBy { get; set; }

    public string CreatedById { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string UpdatedById { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
