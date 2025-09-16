using System;
using System.Collections.Generic;
using System.Linq;

using Humanizer;

using SE.Shared.Common.Extensions;
using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.EDIFieldValue;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Data;
using Trident.Domain;
using Trident.Extensions;
using Trident.SourceGeneration.Attributes;

namespace SE.Shared.Domain.Entities.SalesLine;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Operations, nameof(DocumentType), Databases.DocumentTypes.SalesLine, PartitionKeyType.Composite)]
[Discriminator(nameof(EntityType), Databases.Discriminators.SalesLine)]
[GenerateProvider]
public class SalesLineEntity : TTAuditableEntityBase, IFacilityRelatedEntity, IHaveCompositePartitionKey
{
    public string SalesLineNumber { get; set; }

    public Guid TruckTicketId { get; set; }

    public string TruckTicketNumber { get; set; }

    public Guid ProductId { get; set; }

    public string ProductName { get; set; }

    public string ProductNumber { get; set; }

    public double Quantity { get; set; }

    public double QuantityPercent { get; set; }

    public double TareWeight { get; set; }

    public double GrossWeight { get; set; }

    public string UnitOfMeasure { get; set; }

    public double? OriginalRate { get; set; }

    public double Rate { get; set; }

    public double TotalValue { get; set; }

    public bool IsRateOverridden { get; set; }

    public Guid? LoadConfirmationId { get; set; }

    public string LoadConfirmationNumber { get; set; }

    public Guid? InvoiceId { get; set; }

    public string ProformaInvoiceNumber { get; set; }

    public bool IsReversed { get; set; }

    public bool IsReversal { get; set; }

    public Guid? ReversedSalesLineId { get; set; }

    public Guid? ReversalSalesLineId { get; set; }

    public Guid SourceLocationId { get; set; }

    public string SourceLocationIdentifier { get; set; }

    public string SourceLocationFormattedIdentifier { get; set; }

    public Guid SourceLocationTypeId { get; set; }

    public string SourceLocationTypeName { get; set; }

    public WellClassifications WellClassification { get; set; }

    // aka TruckTicket.BillingCustomerId & Account.Id
    public Guid CustomerId { get; set; }

    public string CustomerName { get; set; }

    public Guid GeneratorId { get; set; }

    public string GeneratorName { get; set; }

    public Guid? BillingConfigurationId { get; set; }

    public string BillingConfigurationName { get; set; }

    public string FacilitySiteId { get; set; }

    public bool IsAdditionalService { get; set; }

    public bool? IsReadOnlyLine { get; set; }

    public bool? IsUserAddedAdditionalServices { get; set; }

    public bool IsCutLine { get; set; }

    public SalesLineCutType? CutType { get; set; }

    public Guid? MaterialApprovalId { get; set; }

    public string MaterialApprovalNumber { get; set; }

    public Guid? ServiceTypeId { get; set; }

    public string ServiceTypeName { get; set; }

    public Guid? TruckingCompanyId { get; set; }

    public string TruckingCompanyName { get; set; }

    public string UserName { get; set; }

    public string PriceChangeUserName { get; set; }

    public DateTimeOffset? PriceChangeDate { get; set; }

    public SalesLinePriceChangeReason ChangeReason { get; set; }

    public string ChangeComments { get; set; }

    public SalesLineStatus Status { get; set; }

    public string ManifestNumber { get; set; }

    public string BillOfLading { get; set; }

    public DowNonDow DowNonDow { get; set; }

    public DateTimeOffset TruckTicketDate { get; set; }

    public DateTime? TruckTicketEffectiveDate { get; set; }

    public bool IsEdiValid { get; set; }

    public Guid? PricingRuleId { get; set; }

    public bool? AwaitingRemovalAcknowledgment { get; set; }

    public Guid? HistoricalInvoiceId { get; set; }

    [OwnedHierarchy]
    public List<EDIFieldValueEntity> EdiFieldValues { get; set; } = new();

    public List<SalesLineAttachmentEntity> Attachments { get; set; } = new();

    public string EDIFieldsValueString
    {
        get => string.Join(", ", (EdiFieldValues ?? new()).Select(x => string.Concat(x.EDIFieldName, ": ", x.EDIFieldValueContent)));
        private set
        {
            // No Op, EF Core requires this.
        }
    }

    public string EDIFieldsValueStringHumanized
    {
        get =>
            string.Join(", ", (EdiFieldValues ?? new()).Where(x => x.EDIFieldValueContent.HasText())
                                                       .Select(x => string.Concat(x.EDIFieldName.Humanize(LetterCasing.Title), ": ", x.EDIFieldValueContent)));
        private set
        {
            // No Op, EF Core requires this.
        }
    }

    public AttachmentIndicatorType AttachmentIndicatorType { get; set; }

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

    public string Substance { get; set; }

    public string Division { get; set; }

    public string BusinessUnit { get; set; }

    public string LegalEntity { get; set; }

    public string AccountNumber { get; set; }

    public string CustomerNumber { get; set; }

    public bool? CanPriceBeRefreshed { get; set; }

    public Guid FacilityId { get; set; }

    public void InitPartitionKey(string customPartitionKey = null)
    {
        DocumentType ??= customPartitionKey ?? $"{Databases.DocumentTypes.SalesLine}|{DateTime.Today:MMyyyy}";
    }

    public void ApplyFoRounding()
    {
        if (Id == Guid.Empty)
        {
            Id = Guid.NewGuid();
        }

        // This is to match FO rounding mechanics which differs from the TT default
        Rate = (double)Math.Round((decimal)Rate, 2, MidpointRounding.AwayFromZero);
        Quantity = (double)Math.Round((decimal)Quantity, 2, MidpointRounding.AwayFromZero);
        TotalValue = (double)Math.Round((decimal)Rate * (decimal)Quantity, 2, MidpointRounding.AwayFromZero);
    }

    public SalesLineEntity CloneAsNew()
    {
        var newSalesLine = this.Clone();
        newSalesLine.Id = Guid.NewGuid();
        newSalesLine.Status = SalesLineStatus.Preview;
        newSalesLine.AwaitingRemovalAcknowledgment = false;
        newSalesLine.LoadConfirmationId = null;
        newSalesLine.LoadConfirmationNumber = null;
        newSalesLine.ProformaInvoiceNumber = null;
        newSalesLine.InvoiceId = null;
        newSalesLine.HistoricalInvoiceId = null;
        newSalesLine.SalesLineNumber = null;
        return newSalesLine;
    }
}

public class SalesLineAttachmentEntity : OwnedEntityBase<Guid>
{
    public string Container { get; set; }

    public string File { get; set; }

    public string Path { get; set; }

    public AttachmentType? AttachmentType { get; set; }

    public bool IsInternalAttachment()
    {
        return AttachmentType.HasValue
                   ? AttachmentType.Value == TruckTicketing.Contracts.Lookups.AttachmentType.Internal
                   : IsInternalAttachmentByFile();
    }

    public bool IsExternalAttachment()
    {
        return AttachmentType.HasValue
                   ? AttachmentType.Value == TruckTicketing.Contracts.Lookups.AttachmentType.External
                   : IsExternalAttachmentByFile();
    }

    public bool IsInternalAttachmentByFile()
    {
        return File.ToLower().Contains("-int", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsExternalAttachmentByFile()
    {
        return File.ToLower().Contains("-ext", StringComparison.OrdinalIgnoreCase);
    }
}
