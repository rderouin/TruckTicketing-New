using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

using SE.Shared.Domain;

using Trident.Data;
using Trident.Domain;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Domain.Entities.FacilityService;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Operations, nameof(DocumentType), Databases.DocumentTypes.FacilityService, PartitionKeyType.WellKnown)]
[Discriminator(Databases.Discriminators.FacilityService, Property = nameof(EntityType))]
[GenerateManager]
[GenerateProvider]
public class FacilityServiceEntity : TTEntityBase
{
    public Guid FacilityId { get; set; }

    public string SiteId { get; set; }

    public Guid ServiceTypeId { get; set; }

    public string OilItem { get; set; }

    public string SolidsItem { get; set; }

    public string WaterItem { get; set; }

    public string TotalItem { get; set; }

    public Guid? TotalItemProductId { get; set; }

    public string ServiceTypeName { get; set; }

    [OwnedHierarchy]
    public List<FacilityServiceSpartanProductParameterEntity> SpartanProductParameters { get; set; } = new();

    public string Description { get; set; }

    public int ServiceNumber { get; set; }

    public string FacilityServiceNumber { get; set; }

    public bool IsActive { get; set; }

    [NotMapped]
    public bool? IsUnique { get; set; }

    public PrimitiveCollection<Guid> AuthorizedSubstances { get; set; }
}

public class FacilityServiceSpartanProductParameterEntity : OwnedEntityBase<Guid>
{
    public Guid SpartanProductParameterId { get; set; }

    public string SpartanProductParameterName { get; set; }

    public string SpartanProductParameterDisplay { get; set; }
}
