using SE.TruckTicketing.Contracts;

using Trident.Data;
using Trident.SourceGeneration.Attributes;

namespace SE.Shared.Domain.Entities.Note;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Operations, nameof(DocumentType), nameof(NoteEntity), PartitionKeyType.Composite)]
[Discriminator(nameof(EntityType), Containers.Discriminators.Note)]
[GenerateManager]
[GenerateProvider]
[GenerateRepository]
public class NoteEntity : TTAuditableEntityBase, IHaveCompositePartitionKey
{
    public string Comment { get; set; }

    public string ThreadId { get; set; }

    public bool NotEditable { get; set; }

    public void InitPartitionKey(string customPartitionKey = null)
    {
        DocumentType ??= customPartitionKey ?? $"{nameof(NoteEntity)}|{ThreadId}";
    }
}
