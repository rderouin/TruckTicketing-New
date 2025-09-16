using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SE.Shared.Domain.Entities.UserProfile;

using Trident.EFCore;

namespace SE.TruckTicketing.Api.Configuration.EntityMapping;

public class UserProfileEntityMap : EntityMapper<UserProfileEntity>, IEntityMapper<UserProfileEntity>
{
    public override void Configure(EntityTypeBuilder<UserProfileEntity> modelBinding)
    {
        modelBinding.OwnsMany(x => x.Roles, RolesBuilder =>
                                            {
                                                RolesBuilder.WithOwner();
                                            });

        modelBinding.OwnsMany(x => x.SpecificFacilityAccessAssignments, facBuilder =>
                                                                        {
                                                                            facBuilder.WithOwner();
                                                                        });
    }
}

public class UserProfileRoleEntityMap : EntityMapper<UserProfileRoleEntity>, IEntityMapper<UserProfileRoleEntity>
{
    public override void Configure(EntityTypeBuilder<UserProfileRoleEntity> modelBinding)
    {
    }
}

public class UserProfileFacilityAccessEntityMap : EntityMapper<UserProfileFacilityAccessEntity>, IEntityMapper<UserProfileFacilityAccessEntity>
{
    public override void Configure(EntityTypeBuilder<UserProfileFacilityAccessEntity> modelBinding)
    {
    }
}
