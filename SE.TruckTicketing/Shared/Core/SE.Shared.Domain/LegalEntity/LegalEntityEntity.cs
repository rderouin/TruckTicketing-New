using System;

using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Data;
using Trident.SourceGeneration.Attributes;

namespace SE.Shared.Domain.LegalEntity;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Operations, nameof(DocumentType), nameof(LegalEntityEntity), PartitionKeyType.WellKnown)]
[Discriminator(nameof(EntityType), Containers.Discriminators.LegalEntity)]
[GenerateManager]
[GenerateProvider]
public class LegalEntityEntity : TTEntityBase
{
    public Guid? BusinessStreamId { get; set; }

    public string Name { get; set; }

    public string Code { get; set; }

    public CountryCode CountryCode { get; set; }

    public int CreditExpirationThreshold { get; set; }

    public bool IsCustomerPrimaryContactRequired { get; set; }

    public bool? ShowAccountsInTruckTicketing { get; set; }

    public string RemitToDuns { get; set; }
}
