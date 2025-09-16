using SE.TruckTicketing.Contracts;

using Trident.Data;

namespace SE.Shared.Domain.Entities.Sequences;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Operations, nameof(DocumentType), nameof(SequenceEntity), PartitionKeyType.WellKnown)]
[Discriminator(nameof(EntityType), Containers.Discriminators.Sequence)]
public class SequenceEntity : TTEntityBase
{
    public string Type { get; set; }

    public string Prefix { get; set; }

    public long LastNumber { get; set; }
}
