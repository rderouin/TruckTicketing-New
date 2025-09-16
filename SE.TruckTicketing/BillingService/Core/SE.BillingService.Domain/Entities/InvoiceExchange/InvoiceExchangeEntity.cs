using System;
using System.Collections.Generic;

using SE.BillingService.Contracts.Api.Enums;
using SE.Shared.Domain;

using Trident.Data;
using Trident.Domain;
using Trident.SourceGeneration.Attributes;

namespace SE.BillingService.Domain.Entities.InvoiceExchange;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Billing, nameof(DocumentType), Databases.DocumentTypes.InvoiceExchange, PartitionKeyType.WellKnown)]
[Discriminator(nameof(EntityType), Databases.Discriminators.InvoiceExchange)]
[GenerateProvider]
public class InvoiceExchangeEntity : TTAuditableEntityBase
{
    public string PlatformCode { get; set; }

    public InvoiceExchangeType Type { get; set; }

    public bool IsDeleted { get; set; }

    public Guid? RootInvoiceExchangeId { get; set; }

    public Guid? BusinessStreamId { get; set; }

    public string BusinessStreamName { get; set; }

    public Guid? LegalEntityId { get; set; }

    public string LegalEntityName { get; set; }

    public Guid? BillingAccountId { get; set; }

    public string BillingAccountName { get; set; }

    public string BillingAccountNumber { get; set; }

    public string BillingAccountDunsNumber { get; set; }

    public bool IsCustomerDefault { get; set; }

    public bool SupportsFieldTickets { get; set; }

    public bool UseEmailDelivery { get; set; }

    public string Namespace { get; set; }

    public InvoiceExchangeDeliveryConfigurationEntity InvoiceDeliveryConfiguration { get; set; } = new();

    public InvoiceExchangeDeliveryConfigurationEntity FieldTicketsDeliveryConfiguration { get; set; } = new();
}

public class InvoiceExchangeDeliveryConfigurationEntity : OwnedEntityBase<Guid>
{
    public MessageAdapterType MessageAdapterType { get; set; }

    public decimal MessageAdapterVersion { get; set; }

    public bool SupportsStatusPolling { get; set; }

    public MessageAdapterPollingStrategy PollingStrategy { get; set; }

    public bool? IsPreprocessorEnabled { get; set; }

    public string PreprocessorExpression { get; set; }

    public InvoiceExchangeMessageAdapterSettingsEntity MessageAdapterSettings { get; set; } = new();

    public InvoiceExchangeTransportSettingsEntity TransportSettings { get; set; } = new();

    public InvoiceExchangeTransportSettingsEntity AttachmentSettings { get; set; } = new();

    public List<InvoiceExchangeMessageFieldMappingEntity> Mappings { get; set; } = new();
}

public class InvoiceExchangeMessageAdapterSettingsEntity : OwnedEntityBase<Guid>
{
    public string DestinationApiDefinitionUri { get; set; }

    public bool IncludeHeaderRow { get; set; }

    public bool AcceptsAttachments { get; set; }

    public bool EmbedAttachments { get; set; }

    public bool SupportsSingleAttachmentOnly { get; set; }

    public int MaxAttachmentSizeInMegabytes { get; set; }

    public bool? AlwaysQuote { get; set; }
}

public class InvoiceExchangeTransportSettingsEntity : OwnedEntityBase<Guid>
{
    public InvoiceDeliveryTransportType TransportType { get; set; }

    public string DestinationEndpointUri { get; set; }

    public HttpVerb HttpVerb { get; set; }

    public Dictionary<string, string> HttpHeaders { get; set; } = new();

    public string ClientId { get; set; }

    public string ClientSecret { get; set; }

    public string Certificate { get; set; }

    public bool IsCustomPayload { get; set; }

    public string ContentType { get; set; }

    public string PayloadTemplate { get; set; }
}

public class InvoiceExchangeMessageFieldMappingEntity : OwnedEntityBase<Guid>
{
    public bool IsDisabled { get; set; }

    public Guid? SourceModelFieldId { get; set; }

    public Guid? DestinationModelFieldId { get; set; }

    public Guid? DestinationFormatId { get; set; }

    public string DestinationConstantValue { get; set; }

    public bool DestinationUsesValueExpression { get; set; }

    public string DestinationValueExpression { get; set; }

    public string DestinationPlacementHint { get; set; }

    public string DestinationHeaderTitle { get; set; }

    public int DestinationFieldPosition { get; set; }

    public string DestinationClassName { get; set; }

    public string DestinationPropertyName { get; set; }
}
