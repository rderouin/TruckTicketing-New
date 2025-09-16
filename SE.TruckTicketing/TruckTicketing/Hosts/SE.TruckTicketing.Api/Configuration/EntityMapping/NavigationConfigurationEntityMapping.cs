using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SE.TruckTicketing.Domain.Entities.Configuration;

using Trident.EFCore;

namespace SE.TruckTicketing.Api.Configuration.EntityMapping;

public class NavigationConfigurationEntityMapping : EntityMapper<NavigationConfigurationEntity>, IEntityMapper<NavigationConfigurationEntity>
{
    public override void Configure(EntityTypeBuilder<NavigationConfigurationEntity> modelBinding)
    {
        modelBinding.OwnsMany(x => x.NavigationItems, x =>
                                                      {
                                                          x.WithOwner();
                                                          x.OwnsMany(y => y.NavigationItems);
                                                      });
    }
}
