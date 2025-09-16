using System;
using System.Collections.Generic;
using System.Linq;

using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Data;
using Trident.Domain;
using Trident.SourceGeneration.Attributes;

namespace SE.Shared.Domain.Product;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Products, nameof(DocumentType), Databases.DocumentTypes.Products, PartitionKeyType.WellKnown)]
[Discriminator(nameof(EntityType), Containers.Discriminators.Product)]
[GenerateManager]
[GenerateProvider]
public class ProductEntity : TTEntityBase
{
    public string Name { get; set; }

    public string Number { get; set; }

    [OwnedHierarchy]
    public List<ProductSubstanceEntity> Substances { get; set; } = new();

    [OwnedHierarchy]
    public PrimitiveCollection<string> AllowedSites { get; set; } = new();

    [OwnedHierarchy]
    public PrimitiveCollection<string> Categories { get; set; } = new();

    public bool IsActive { get; set; }

    public Guid LegalEntityId { get; set; }

    public string LegalEntityCode { get; set; }

    public string UnitOfMeasure { get; set; }

    public string DisposalUnit { get; set; }

    public DateTime? LastIntegrationTimestamp { get; set; }
    
    public bool IsServiceOnlyProduct()
    {
        return Categories?.List.Any(c => c.StartsWith(ProductCategories.AdditionalServices.Lf) ||
                                         c.StartsWith(ProductCategories.AdditionalServices.Fst)) == true;
    }
}

public class ProductSubstanceEntity : OwnedEntityBase<Guid>
{
    public string SubstanceName { get; set; }

    public string WasteCode { get; set; }
}
