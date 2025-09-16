using System;
using System.Collections.Generic;
using System.Linq;

using Trident.Contracts.Api;

namespace SE.TruckTicketing.Contracts.Models.InvoiceConfigurations;

public class InvoiceConfiguration : GuidApiModelBase
{
    public string BusinessUnitId { get; set; }

    public string Name { get; set; }

    public string InvoiceNumber { get; set; }

    public string Description { get; set; }

    public Guid CustomerId { get; set; }

    public Guid? CustomerLegalEntityId { get; set; }

    public Guid? BillingContactId { get; set; }

    public string BillingContactName { get; set; }

    public string CustomerName { get; set; }

    public string InvoiceExchange { get; set; }

    public bool IncludeExternalDocumentAttachment { get; set; }

    public bool IncludeInternalDocumentAttachment { get; set; }

    public List<Guid> SourceLocations { get; set; } = new();

    public List<string> SourceLocationIdentifier { get; set; }

    public bool IsSplitBySourceLocation { get; set; } = true;

    public List<Guid> ServiceTypes { get; set; } = new();

    public List<string> ServiceTypesName { get; set; }

    public bool IsSplitByServiceType { get; set; }

    public List<Guid> Substances { get; set; } = new();

    public List<string> SubstancesName { get; set; }

    public bool IsSplitBySubstance { get; set; }

    public List<Guid> Facilities { get; set; }

    public List<string> FacilityCode { get; set; }

    public bool IsSplitByFacility { get; set; } = true;

    public List<string> WellClassifications { get; set; }

    public bool IsSplitByWellClassification { get; set; } = true;

    public List<string> SplittingCategories { get; set; }

    public List<Guid> SplitEdiFieldDefinitions { get; set; }

    public List<string> SplitEdiFieldDefinitionNames { get; set; }

    public bool IsMaximumDollarValueThresholdEnabled { get; set; }

    public double? ThresholdDollarValue { get; set; }

    public bool IsMaximumTicketsThresholdEnabled { get; set; }

    public int? ThresholdTicketCount { get; set; }

    public List<InvoiceConfigurationPermutations> Permutations { get; set; }

    public string PermutationsHash { get; set; }

    public bool AllSourceLocations { get; set; } = true;

    public bool AllServiceTypes { get; set; } = true;

    public bool AllWellClassifications { get; set; } = true;

    public bool AllSubstances { get; set; } = true;

    public bool AllFacilities { get; set; } = true;

    public bool CatchAll { get; set; } = true;

    public string CreatedBy { get; set; }

    public string CreatedById { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string UpdatedBy { get; set; }

    public string UpdatedById { get; set; }
}

public class InvoiceConfigurationPermutations : ApiModelBase<Guid>
{
    public string Name { get; set; }

    public string Number { get; set; }

    public string SourceLocation { get; set; }

    public string ServiceType { get; set; }

    public string WellClassification { get; set; }

    public string Substance { get; set; }

    public string Facility { get; set; }

    private InvoiceConfigurationPermutations Combine(InvoiceConfigurationPermutations other)
    {
        return new()
        {
            Name = !string.IsNullOrWhiteSpace(Name) ? Name : other.Name,
            SourceLocation = !string.IsNullOrWhiteSpace(SourceLocation) ? SourceLocation : other.SourceLocation,
            ServiceType = !string.IsNullOrWhiteSpace(ServiceType) ? ServiceType : other.ServiceType,
            Substance = !string.IsNullOrWhiteSpace(Substance) ? Substance : other.Substance,
            Facility = !string.IsNullOrWhiteSpace(Facility) ? Facility : other.Facility,
            WellClassification = !string.IsNullOrWhiteSpace(WellClassification) ? WellClassification : other.WellClassification,
        };
    }

    public List<InvoiceConfigurationPermutations> CrossApply(List<InvoiceConfigurationPermutations> items)
    {
        var clone = new InvoiceConfigurationPermutations
        {
            Name = Name,
            SourceLocation = SourceLocation,
            ServiceType = ServiceType,
            Substance = Substance,
            Facility = Facility,
            WellClassification = WellClassification,
        };

        return items.Count == 0 ? new() { clone } : items.Select(Combine).ToList();
    }
}
