using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using SE.BillingService.Contracts.Api.Enums;
using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Contracts.Models;

using ObjectExtensions = Trident.Extensions.ObjectExtensions;

namespace SE.BillingService.Contracts.Api.Models;

public class InvoiceExchangeDto : GuidApiModelBase
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
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

    public bool SupportsFieldTickets { get; set; }

    public bool UseEmailDelivery { get; set; }

    public string Namespace { get; set; }

    public InvoiceExchangeDeliveryConfigurationDto InvoiceDeliveryConfiguration { get; set; } = new();

    public InvoiceExchangeDeliveryConfigurationDto FieldTicketsDeliveryConfiguration { get; set; } = new();

    public InvoiceExchangeDto InheritValuesFrom(InvoiceExchangeDto another, bool isCloning)
    {
        if (another == null)
        {
            return this;
        }

        // inheritance
        if (isCloning)
        {
            RootInvoiceExchangeId = another.RootInvoiceExchangeId;
        }
        else
        {
            RootInvoiceExchangeId = another.Type == InvoiceExchangeType.Global ? another.Id : another.RootInvoiceExchangeId;
        }

        // cascading without Type
        PlatformCode = another.PlatformCode;
        BusinessStreamId = another.BusinessStreamId;
        BusinessStreamName = another.BusinessStreamName;
        LegalEntityId = another.LegalEntityId;
        LegalEntityName = another.LegalEntityName;
        BillingAccountId = another.BillingAccountId;
        BillingAccountName = another.BillingAccountName;
        BillingAccountNumber = another.BillingAccountNumber;
        BillingAccountDunsNumber = another.BillingAccountDunsNumber;

        // basics
        SupportsFieldTickets = another.SupportsFieldTickets;
        UseEmailDelivery = another.UseEmailDelivery;
        Namespace = another.Namespace;
        InvoiceDeliveryConfiguration.InheritValuesFrom(another.InvoiceDeliveryConfiguration, isCloning);
        FieldTicketsDeliveryConfiguration.InheritValuesFrom(another.FieldTicketsDeliveryConfiguration, isCloning);

        // fluency
        return this;
    }
}

public class InvoiceExchangeDeliveryConfigurationDto : GuidApiModelBase
{
    public MessageAdapterType MessageAdapterType { get; set; }

    public decimal MessageAdapterVersion { get; set; }

    public bool SupportsStatusPolling { get; set; }

    public MessageAdapterPollingStrategy PollingStrategy { get; set; }

    public bool IsPreprocessorEnabled { get; set; }

    public string PreprocessorExpression { get; set; }

    public InvoiceExchangeMessageAdapterSettingsDto MessageAdapterSettings { get; set; } = new();

    public InvoiceExchangeTransportSettingsDto TransportSettings { get; set; } = new();

    public InvoiceExchangeTransportSettingsDto AttachmentSettings { get; set; } = new();

    public List<InvoiceExchangeMessageFieldMappingDto> Mappings { get; set; } = new();

    public InvoiceExchangeDeliveryConfigurationDto InheritValuesFrom(InvoiceExchangeDeliveryConfigurationDto another, bool includeMappings)
    {
        if (another == null)
        {
            return this;
        }

        // basics without mappings
        MessageAdapterType = another.MessageAdapterType;
        MessageAdapterVersion = another.MessageAdapterVersion;
        SupportsStatusPolling = another.SupportsStatusPolling;
        PollingStrategy = another.PollingStrategy;
        IsPreprocessorEnabled = another.IsPreprocessorEnabled;
        PreprocessorExpression = another.PreprocessorExpression;
        MessageAdapterSettings.InheritValuesFrom(another.MessageAdapterSettings);
        TransportSettings.InheritValuesFrom(another.TransportSettings);
        AttachmentSettings.InheritValuesFrom(another.AttachmentSettings);

        // clone mappings if requested
        if (includeMappings)
        {
            Mappings = ObjectExtensions.FromJson<List<InvoiceExchangeMessageFieldMappingDto>>(ObjectExtensions.ToJson(another.Mappings));
        }

        // fluency
        return this;
    }
}

public class InvoiceExchangeMessageAdapterSettingsDto : GuidApiModelBase
{
    public string DestinationApiDefinitionUri { get; set; }

    public bool IncludeHeaderRow { get; set; }

    public bool AcceptsAttachments { get; set; }

    public bool EmbedAttachments { get; set; }

    public bool SupportsSingleAttachmentOnly { get; set; }

    public int MaxAttachmentSizeInMegabytes { get; set; } = 5;

    public bool? AlwaysQuote { get; set; } = false;

    public InvoiceExchangeMessageAdapterSettingsDto InheritValuesFrom(InvoiceExchangeMessageAdapterSettingsDto another)
    {
        if (another == null)
        {
            return this;
        }

        // all properties
        DestinationApiDefinitionUri = another.DestinationApiDefinitionUri;
        IncludeHeaderRow = another.IncludeHeaderRow;
        AcceptsAttachments = another.AcceptsAttachments;
        EmbedAttachments = another.EmbedAttachments;
        SupportsSingleAttachmentOnly = another.SupportsSingleAttachmentOnly;
        MaxAttachmentSizeInMegabytes = another.MaxAttachmentSizeInMegabytes;
        AlwaysQuote = another.AlwaysQuote;

        return this;
    }
}

public class InvoiceExchangeTransportSettingsDto : GuidApiModelBase
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

    public InvoiceExchangeTransportSettingsDto InheritValuesFrom(InvoiceExchangeTransportSettingsDto another)
    {
        if (another == null)
        {
            return this;
        }

        // all properties
        TransportType = another.TransportType;
        DestinationEndpointUri = another.DestinationEndpointUri;
        HttpVerb = another.HttpVerb;
        HttpHeaders = another.HttpHeaders.Clone();
        ClientId = another.ClientId;
        ClientSecret = another.ClientSecret;
        Certificate = another.Certificate;
        IsCustomPayload = another.IsCustomPayload;
        ContentType = another.ContentType;
        PayloadTemplate = another.PayloadTemplate;

        return this;
    }
}

public class InvoiceExchangeMessageFieldMappingDto : GuidApiModelBase
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
