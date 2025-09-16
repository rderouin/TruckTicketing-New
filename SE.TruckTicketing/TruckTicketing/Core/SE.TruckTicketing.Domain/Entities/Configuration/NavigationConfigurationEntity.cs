using System;
using System.Collections.Generic;

using SE.Shared.Domain;
using SE.TruckTicketing.Contracts;

using Trident.Data;
using Trident.Domain;

namespace SE.TruckTicketing.Domain.Entities.Configuration;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Accounts, nameof(DocumentType), Containers.Partitions.Accounts, PartitionKeyType.WellKnown)]
[Discriminator(Containers.Discriminators.NavigationConfiguration, Property = nameof(EntityType))]
public class NavigationConfigurationEntity : TTEntityBase
{
    public string ProfileName { get; set; }

    public List<NavigationItemEntity> NavigationItems { get; set; }
}

public class NavigationItemEntity : OwnedEntityBase<Guid>
{
    public string RelativeUrl { get; set; }

    public string ClaimType { get; set; }

    public string ClaimValue { get; set; }

    public string Icon { get; set; }

    public string Text { get; set; }

    public int Order { get; set; }

    public List<NavigationItemEntity> NavigationItems { get; set; }
}
