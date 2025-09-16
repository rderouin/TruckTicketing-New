using System;
using System.Collections.Generic;

using SE.TruckTicketing.Contracts;

using Trident.Data;
using Trident.Domain;
using Trident.SourceGeneration.Attributes;

namespace SE.Shared.Domain.Entities.Permission;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Accounts, nameof(DocumentType), Containers.Partitions.Accounts, PartitionKeyType.WellKnown)]
[Discriminator(Containers.Discriminators.Permission, Property = nameof(EntityType))]
[GenerateManager]
[GenerateProvider]
[GenerateRepository]
public class PermissionEntity : TTEntityBase
{
    public string Name { get; set; }

    public string Code { get; set; }

    public string Display { get; set; }

    public IEnumerable<OperationEntity> AllowedOperations { get; set; }
}

public class OperationEntity : OwnedLookupEntityBase<Guid>
{
    public string Value { get; set; }

    public string Display { get; set; }
}
