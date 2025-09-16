using System;

using SE.Shared.Common.Extensions;

namespace SE.TruckTicketing.UI.ViewModels;

public class EDIValueViewModel
{
    public Guid Id { get; set; }

    public string ReferenceId => Id.ToReferenceId();

    public Guid EDIFieldDefinitionId { get; set; }

    public Guid CustomerId { get; set; }

    public Guid? EDIFieldLookupId { get; set; }

    public string EDIFieldName { get; set; }

    public string DefaultValue { get; set; }

    public bool ValidationRequired { get; set; }

    public bool IsPrinted { get; set; }

    public bool IsRequired { get; set; }

    public string ValidationPattern { get; set; }

    public string ValidationErrorMessage { get; set; }

    public string EDIFieldValueContent { get; set; }

    public bool IsNew { get; set; }

    public string CreatedBy { get; set; }

    public string CreatedById { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string UpdatedById { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
