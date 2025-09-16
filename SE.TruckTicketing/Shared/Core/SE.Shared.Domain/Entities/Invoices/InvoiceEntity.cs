using System;
using System.Collections.Generic;
using System.Linq;

using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Data;
using Trident.Domain;
using Trident.SourceGeneration.Attributes;

namespace SE.Shared.Domain.Entities.Invoices;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Operations, nameof(DocumentType), Databases.DocumentTypes.Invoice, PartitionKeyType.Composite)]
[Discriminator(nameof(EntityType), Databases.Discriminators.Invoice)]
[GenerateProvider]
public class InvoiceEntity : TTAuditableEntityBase, IFacilityRelatedEntity, IHaveCompositePartitionKey
{
    public string ProformaInvoiceNumber { get; set; }

    public string GlInvoiceNumber { get; set; }

    public string LegalEntity { get; set; }

    public Guid CustomerId { get; set; }

    public string CustomerName { get; set; }

    public string FacilityName { get; set; }

    public string SiteId { get; set; }

    public string InvoicePermutationId { get; set; }

    public InvoiceStatus Status { get; set; }

    public bool IsReversed { get; set; }

    public bool IsReversal { get; set; }

    public string OriginalGlInvoiceNumber { get; set; }

    public string OriginalProformaInvoiceNumber { get; set; }

    public Guid? ReversedInvoiceId { get; set; }

    public Guid? ReversalInvoiceId { get; set; }

    public DateTimeOffset TicketDateRangeStart { get; set; }

    public DateTimeOffset TicketDateRangeEnd { get; set; }

    public DateTimeOffset InvoiceStartDate { get; set; }

    public DateTimeOffset? InvoiceEndDate { get; set; }

    public DateTimeOffset? LastDistributionDate { get; set; }

    public Guid LastSentById { get; set; }

    public string LastSentByName { get; set; }

    public bool Paid { get; set; }

    public int SalesLineCount { get; set; }

    public double InvoiceAmount { get; set; }

    [OwnedHierarchy]
    public List<InvoiceAttachmentEntity> Attachments { get; set; } = new();

    public string Currency { get; set; }

    [OwnedHierarchy]
    public List<InvoiceBillingConfigurationEntity> BillingConfigurations { get; set; } = new();

    [OwnedHierarchy]
    public PrimitiveCollection<string> Generators { get; set; }

    [OwnedHierarchy]
    public PrimitiveCollection<string> Signatories { get; set; }

    public string GeneratorNames { get => string.Join(",", Generators?.List ?? Enumerable.Empty<string>()); private set => _ = value; }

    public bool? HasAllLoadConfirmationApprovals { get; set; }

    public string BillingConfigurationNames
    {
        get => string.Join(", ", BillingConfigurations?.Where(x => x != null).Select(x => x.BillingConfigurationName).ToList() ?? Enumerable.Empty<string>());
        private set => _ = value;
    }

    public string CustomerNumber { get; set; }

    public string ManifestNumber { get; set; }

    public string BillofLading { get; set; }

    public List<TruckTicketing.Contracts.Models.Operations.EDIFieldValue> EDIFieldValues { get; set; } = new();

    public string EDIFieldsValueString { get; set; }

    public Guid SourceLocationTypeId { get; set; }

    public string SourceLocationTypeName { get; set; }

    public Guid SourceLocationId { get; set; }

    public string SourceLocationIdentifier { get; set; }

    public string SourceLocationFormattedIdentifier { get; set; }

    public DowNonDow DowNonDow { get; set; }

    public AttachmentIndicatorType AttachmentIndicatorType { get; set; }

    public InvoiceDistributionMethod DistributionMethod { get; set; }

    public InvoiceReversalReason InvoiceReversalReason { get; set; }

    public string InvoiceReversalDescription { get; set; }

    public InvoiceCollectionOwners CollectionOwner { get; set; }

    public InvoiceCollectionReason CollectionReason { get; set; }

    public bool TransactionComplete { get; set; }

    public bool RequiresPdfRegeneration { get; set; } = true;

    public CreditStatus? CustomerCreditStatus { get; set; }

    public WatchListStatus? CustomerWatchListStatus { get; set; }

    public HasAttachments HasAttachments
    {
        get
        {
            if (Attachments == null || !Attachments.Any())
            {
                return HasAttachments.None;
            }

            return AttachmentIndicatorType switch
                   {
                       AttachmentIndicatorType.Internal or AttachmentIndicatorType.External or AttachmentIndicatorType.Neither => HasAttachments.Any,
                       AttachmentIndicatorType.InternalExternal => HasAttachments.Both,
                       _ => HasAttachments.None,
                   };
        }
        set { }
    }

    public Guid InvoiceConfigurationId { get; set; }

    public string InvoiceConfigurationName { get; set; }

    public Guid? BillingConfigurationId { get; set; }

    public string BusinessUnit { get; set; }

    public string Division { get; set; }

    public string AccountNumber { get; set; }

    public string CollectionNotes { get; set; }

    public Guid? BillingContactId { get; set; }

    public string BillingContactName { get; set; }

    public bool? IsDeliveredToErp { get; set; }

    public int? TruckTicketCount { get; set; }

    public double? MaxInvoiceAmountThreshold { get; set; }

    public double? MaxTruckTicketCountThreshold { get; set; }

    public Guid FacilityId { get; set; }

    public void InitPartitionKey(string customPartitionKey = null)
    {
        DocumentType ??= customPartitionKey ?? $"{Databases.DocumentTypes.Invoice}|{DateTime.Today:MMyyyy}";
    }
}

public class InvoiceAttachmentEntity : OwnedEntityBase<Guid>
{
    public string ContainerName { get; set; }

    public string BlobPath { get; set; }

    public string FileName { get; set; }

    public string ContentType { get; set; }

    public DateTimeOffset AttachedOn { get; set; }

    public bool IsUploaded { get; set; }
}

public class InvoiceBillingConfigurationEntity : OwnedEntityBase<Guid>
{
    public int? AssociatedSalesLinesCount { get; set; }

    public Guid? BillingConfigurationId { get; set; }

    public string BillingConfigurationName { get; set; }
}
