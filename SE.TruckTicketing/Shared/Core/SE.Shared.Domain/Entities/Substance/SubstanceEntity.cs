using System.Security.Cryptography;
using System.Text;

using Newtonsoft.Json;

using Org.BouncyCastle.Utilities;

using Trident.Data;
using Trident.SourceGeneration.Attributes;

namespace SE.Shared.Domain.Entities.Substance;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Products, nameof(DocumentType), Databases.DocumentTypes.Substances, PartitionKeyType.WellKnown)]
[Discriminator(nameof(EntityType), Databases.Discriminators.Substance)]
[GenerateManager]
[GenerateProvider]
public class SubstanceEntity : TTEntityBase, ITTSearchableIdBase
{
    public string SubstanceName { get; set; }

    public string WasteCode { get; set; }

    public string SearchableId { get; set; }

    public void ComputeSubstanceId()
    {
        var substance = new SubstanceEntity
        {
            SubstanceName = SubstanceName,
            WasteCode = WasteCode,
        };

        using var sHa256 = SHA256.Create();
        Id = new(Arrays.CopyOf(sHa256.ComputeHash(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(substance))), 16));
    }
}
