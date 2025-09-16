using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SE.Shared.Domain.Entities.Permission;
using SE.Shared.Domain.Entities.Role;

using Trident.EFCore;

namespace SE.TruckTicketing.Api.Configuration.EntityMapping;

public class RoleEntityMap : EntityMapper<RoleEntity>, IEntityMapper<RoleEntity>
{
    public override void Configure(EntityTypeBuilder<RoleEntity> modelBinding)
    {
        modelBinding.OwnsMany(x => x.Permissions, PermissionBuilder =>
                                                  {
                                                      PermissionBuilder.WithOwner();
                                                      PermissionBuilder.OwnsMany(x => x.AssignedOperations, y => y.WithOwner());
                                                  });
    }
}

public class PermissionEntityMap : EntityMapper<PermissionEntity>, IEntityMapper<PermissionEntity>
{
    public override void Configure(EntityTypeBuilder<PermissionEntity> modelBinding)
    {
        modelBinding.OwnsMany(x => x.AllowedOperations, x => x.WithOwner());
    }
}
