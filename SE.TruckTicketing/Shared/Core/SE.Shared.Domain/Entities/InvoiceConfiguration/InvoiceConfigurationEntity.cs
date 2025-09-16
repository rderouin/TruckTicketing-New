using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

using Newtonsoft.Json;

using Trident.Data;
using Trident.Domain;
using Trident.SourceGeneration.Attributes;

namespace SE.Shared.Domain.Entities.InvoiceConfiguration;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Billing, nameof(DocumentType), Databases.DocumentTypes.InvoiceConfiguration, PartitionKeyType.WellKnown)]
[Discriminator(nameof(EntityType), Databases.Discriminators.InvoiceConfiguration)]
[GenerateProvider]
public class InvoiceConfigurationEntity : TTAuditableEntityBase
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

    [OwnedHierarchy]
    public PrimitiveCollection<Guid> SourceLocations { get; set; }

    [OwnedHierarchy]
    public PrimitiveCollection<string> SourceLocationIdentifier { get; set; }

    public bool IsSplitBySourceLocation { get; set; } = true;

    [OwnedHierarchy]
    public PrimitiveCollection<Guid> ServiceTypes { get; set; }

    [OwnedHierarchy]
    public PrimitiveCollection<string> ServiceTypesName { get; set; }

    public bool IsSplitByServiceType { get; set; }

    [OwnedHierarchy]
    public PrimitiveCollection<Guid> Substances { get; set; }

    [OwnedHierarchy]
    public PrimitiveCollection<string> SubstancesName { get; set; }

    public bool IsSplitBySubstance { get; set; }

    [OwnedHierarchy]
    public PrimitiveCollection<Guid> Facilities { get; set; }

    [OwnedHierarchy]
    public PrimitiveCollection<string> FacilityCode { get; set; }

    public bool IsSplitByFacility { get; set; } = true;

    [OwnedHierarchy]
    public PrimitiveCollection<string> WellClassifications { get; set; }

    public bool IsSplitByWellClassification { get; set; } = true;

    [OwnedHierarchy]
    public PrimitiveCollection<string> SplittingCategories { get; set; }

    [OwnedHierarchy]
    public PrimitiveCollection<Guid> SplitEdiFieldDefinitions { get; set; }

    public List<string> SplitEdiFieldDefinitionNames { get; set; }

    [OwnedHierarchy]
    public List<InvoiceConfigurationPermutationsEntity> Permutations { get; set; }

    public string PermutationsHash { get; set; }

    public bool AllSourceLocations { get; set; }

    public bool AllServiceTypes { get; set; }

    public bool AllWellClassifications { get; set; }

    public bool AllSubstances { get; set; }

    public bool AllFacilities { get; set; }

    public bool CatchAll { get; set; }

    public bool? IsMaximumDollarValueThresholdEnabled { get; set; }

    public double? ThresholdDollarValue { get; set; }

    public bool? IsMaximumTicketsThresholdEnabled { get; set; }

    public int? ThresholdTicketCount { get; set; }

    public void ComputeHash()
    {
        var invoiceConfiguration = new InvoiceConfigurationEntity
        {
            WellClassifications = AllWellClassifications ? null : WellClassifications,
            SourceLocations = AllSourceLocations ? null : SourceLocations,
            Facilities = AllFacilities ? null : Facilities,
            Substances = AllSubstances ? null : Substances,
            ServiceTypes = AllServiceTypes ? null : ServiceTypes,
            SplitEdiFieldDefinitions = SplitEdiFieldDefinitions,
            SplittingCategories = SplittingCategories,
            AllSourceLocations = AllSourceLocations,
            AllFacilities = AllFacilities,
            AllServiceTypes = AllServiceTypes,
            AllSubstances = AllSubstances,
            AllWellClassifications = AllWellClassifications,
            IsSplitByFacility = IsSplitByFacility,
            IsSplitByServiceType = IsSplitByServiceType,
            IsSplitBySourceLocation = IsSplitBySourceLocation,
            IsSplitBySubstance = IsSplitBySubstance,
            IsSplitByWellClassification = IsSplitByWellClassification,
            CatchAll = CatchAll,
            CustomerId = CustomerId,
        };

        using var sHa256 = SHA256.Create();
        PermutationsHash = Convert.ToHexString(sHa256.ComputeHash(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(invoiceConfiguration))));
    }
}

public class InvoiceConfigurationPermutationsEntity : OwnedEntityBase<Guid>
{
    public string Name { get; set; }

    public string Number { get; set; }

    public string SourceLocation { get; set; }

    public string ServiceType { get; set; }

    public string WellClassification { get; set; }

    public string Substance { get; set; }

    public string Facility { get; set; }
}
