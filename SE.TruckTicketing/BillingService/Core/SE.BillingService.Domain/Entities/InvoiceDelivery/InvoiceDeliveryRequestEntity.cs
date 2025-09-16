using Newtonsoft.Json;

using SE.Enterprise.Contracts.Models.InvoiceDelivery;
using SE.Shared.Domain;

using Trident.Data;
using Trident.SourceGeneration.Attributes;

namespace SE.BillingService.Domain.Entities.InvoiceDelivery;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Billing, nameof(DocumentType), Databases.DocumentTypes.InvoiceDeliveryRequest, PartitionKeyType.WellKnown)]
[Discriminator(nameof(EntityType), Databases.Discriminators.InvoiceDeliveryRequest)]
[GenerateProvider]
[GenerateRepository]
public class InvoiceDeliveryRequestEntity : TTEntityBase
{
    public bool IsProcessed { get; set; }

    public bool HasReachedFinalStatus { get; set; }

    public string OriginalMessage { get; set; }

    public DeliveryRequest GetInvoiceDeliveryRequest()
    {
        return JsonConvert.DeserializeObject<DeliveryRequest>(OriginalMessage);
    }
}
