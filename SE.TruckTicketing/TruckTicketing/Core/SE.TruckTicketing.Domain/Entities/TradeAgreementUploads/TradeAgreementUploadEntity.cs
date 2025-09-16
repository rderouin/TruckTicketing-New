using System.ComponentModel.DataAnnotations.Schema;

using SE.Shared.Domain;
using SE.TruckTicketing.Contracts;

using Trident.Data;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Domain.Entities.TradeAgreementUploads;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Operations, nameof(DocumentType), Databases.DocumentTypes.TradeAgreementUpload, PartitionKeyType.WellKnown)]
[Discriminator(nameof(EntityType), Containers.Discriminators.TradeAgreementUpload)]
[GenerateProvider]
[GenerateRepository]
public class TradeAgreementUploadEntity : TTAuditableEntityBase
{
    public string OriginalFileName { get; set; }

    public string UploadFileName { get; set; }

    public string BlobPath { get; set; }

    public string EmailAddress { get; set; }

    [NotMapped]
    public string Uri { get; set; }
}
