using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Entities.SourceLocationType;
using SE.TruckTicketing.Contracts.Constants.SourceLocations;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Data;
using Trident.Domain;
using Trident.SourceGeneration.Attributes;

namespace SE.Shared.Domain.Entities.SourceLocation;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Operations, nameof(DocumentType), Databases.DocumentTypes.SourceLocation, PartitionKeyType.WellKnown)]
[Discriminator(nameof(EntityType), Databases.Discriminators.SourceLocation)]
[GenerateProvider]
public class SourceLocationEntity : TTAuditableEntityBase, ITTSearchableIdBase
{
    public string ApiNumber { get; set; }

    public string AssociatedSourceLocationFormattedIdentifier { get; set; }

    public Guid AssociatedSourceLocationId { get; set; }

    public string AssociatedSourceLocationIdentifier { get; set; }

    public string AssociatedSourceLocationCode { get; set; }

    public Guid ContractOperatorId { get; set; }

    public string ContractOperatorName { get; set; }

    public Guid? ContractOperatorProductionAccountContactId { get; set; }

    public CountryCode CountryCode { get; set; }

    public string CtbNumber { get; set; }

    public DeliveryMethod DeliveryMethod { get; set; }

    public DownHoleType DownHoleType { get; set; }

    public string FieldName { get; set; }

    public string FormattedIdentifier { get; set; }

    public string GeneratorAccountNumber { get; set; }

    public Guid GeneratorId { get; set; }

    public string GeneratorName { get; set; }

    public Guid? GeneratorProductionAccountContactId { get; set; }

    public string GeneratorProductionAccountContactName { get; set; }

    public DateTimeOffset GeneratorStartDate { get; set; }

    public string Identifier { get; set; }

    public bool IsActive { get; set; }

    [OwnedHierarchy]
    public List<SourceLocationOwnerHistoryEntity> OwnershipHistory { get; set; } = new();

    public string PlsNumber { get; set; }

    public StateProvince ProvinceOrState { get; set; }

    public string ProvinceOrStateString { get; set; }

    public string SourceLocationName { get; set; }

    public string SourceLocationCode { get; set; }

    public string SourceLocationTypeName { get; set; }

    public SourceLocationTypeCategory SourceLocationTypeCategory { get; set; }

    public Guid SourceLocationTypeId { get; set; }

    public string WellFileNumber { get; set; }

    public bool SourceLocationVerified { get; set; }

    public string LicenseNumber { get; set; }

    [NotMapped]
    public SourceLocationTypeEntity SourceLocationType { get; set; }

    [NotMapped]
    public bool IsUnique { get; set; }

    [NotMapped]
    public string FormattedIdentifierPattern { get; set; }

    public bool IsDeleted { get; set; }

    public string SearchableId { get; set; }
    public string GetCountryAgnosticIdentifier()
    {
        return FormattedIdentifier.HasText() ? FormattedIdentifier : SourceLocationName;
    }
}

public class SourceLocationOwnerHistoryEntity : OwnedEntityBase<Guid>
{
    public DateTimeOffset? EndDate { get; set; }

    public string GeneratorAccountNumber { get; set; }

    public Guid GeneratorId { get; set; }

    public string GeneratorName { get; set; }

    public Guid? ProductionAccountContactId { get; set; }

    public string ProductionAccountContactName { get; set; }

    public DateTimeOffset StartDate { get; set; }
}
