using System;

using SE.TruckTicketing.Contracts;

using Trident.Data;
using Trident.SourceGeneration.Attributes;

namespace SE.Shared.Domain.Entities.EDIFieldDefinition;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Billing, nameof(DocumentType), nameof(EDIFieldDefinitionEntity), PartitionKeyType.WellKnown)]
[Discriminator(Containers.Discriminators.EDIFieldDefinition, Property = nameof(EntityType))]
[GenerateManager]
[GenerateProvider]
[GenerateRepository]
public class EDIFieldDefinitionEntity : TTAuditableEntityBase
{
    public Guid CustomerId { get; set; }

    public string EDIFieldLookupId { get; set; }

    public string EDIFieldName { get; set; }

    public string DefaultValue { get; set; }

    public bool IsRequired { get; set; }

    public bool IsPrinted { get; set; }

    public bool ValidationRequired { get; set; }

    public Guid ValidationPatternId { get; set; }

    public string ValidationPattern { get; set; }

    public string ValidationErrorMessage { get; set; }

    public string LegalEntity { get; set; }
}
