using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SE.Shared.Domain.Entities.Account;

using Trident.EFCore;

namespace SE.Shared.Domain.Configuration;

public class AccountContactIndexEntityMapping : EntityMapper<AccountContactIndexEntity>, IEntityMapper<AccountContactIndexEntity>
{
    public override void Configure(EntityTypeBuilder<AccountContactIndexEntity> modelBinding)
    {
        modelBinding.Property(x => x.Country).HasConversion<string>();
        modelBinding.Property(x => x.Province).HasConversion<string>();
        modelBinding.Property(x => x.SignatoryType).HasConversion<string>();
    }
}
