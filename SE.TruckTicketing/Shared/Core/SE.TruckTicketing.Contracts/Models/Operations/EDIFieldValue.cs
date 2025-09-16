using System;

using SE.Shared.Common.Extensions;

namespace SE.TruckTicketing.Contracts.Models.Operations;

public class EDIFieldValue : GuidApiModelBase
{
    public string ReferenceId => Id.ToReferenceId();

    public Guid EDIFieldDefinitionId { get; set; }

    public string EDIFieldName { get; set; }

    public string EDIFieldValueContent { get; set; }

    public string DefaultValue { get; set; }

    public bool IsNew { get; set; }
}
