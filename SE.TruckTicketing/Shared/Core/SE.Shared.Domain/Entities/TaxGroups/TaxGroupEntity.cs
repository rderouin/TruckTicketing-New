using System;
using System.Collections.Generic;

using Trident.Data;
using Trident.Domain;
using Trident.SourceGeneration.Attributes;

namespace SE.Shared.Domain.Entities.TaxGroups;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Operations, nameof(DocumentType), nameof(TaxGroupEntity), PartitionKeyType.WellKnown)]
[Discriminator(nameof(EntityType), Databases.Discriminators.TaxGroup)]
[GenerateManager]
[GenerateProvider]
[GenerateRepository]
public class TaxGroupEntity : TTAuditableEntityBase
{
    public string Name { get; set; }

    public string Group { get; set; }

    public string LegalEntityName { get; set; }

    public Guid LegalEntityId { get; set; }

    public DateTime? LastIntegrationDateTime { get; set; }

    [OwnedHierarchy]
    public List<TaxCodeEntity> TaxCodes { get; set; }
}

public class TaxCodeEntity : OwnedEntityBase<Guid>
{
    public string Code { get; set; }

    public string TaxName { get; set; }

    public string CurrencyCode { get; set; }

    public bool ExemptTax { get; set; }

    public bool UseTax { get; set; }

    public double TaxValuePercentage { get; set; }
}
