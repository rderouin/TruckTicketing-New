using System;
using System.ComponentModel.DataAnnotations.Schema;

using Trident.Domain;

namespace SE.Shared.Domain.Entities.EDIFieldValue;

public class EDIFieldValueEntity : OwnedEntityBase<Guid>
{
    public Guid EDIFieldDefinitionId { get; set; }

    public string EDIFieldName { get; set; }

    public string EDIFieldValueContent { get; set; }

    [NotMapped]
    public bool IsValid { get; set; } = true;
}
