using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SE.Shared.Domain.Entities.Account;

using Trident.EFCore;

namespace SE.Shared.Domain.Configuration;

public class AccountEntityMapping : EntityMapper<AccountEntity>, IEntityMapper<AccountEntity>
{
    public override void Configure(EntityTypeBuilder<AccountEntity> modelBinding)
    {
        modelBinding.Property(x => x.BillingType).HasConversion<string>();
        modelBinding.Property(x => x.AccountStatus).HasConversion<string>();
        modelBinding.Property(x => x.WatchListStatus).HasConversion<string>();
        modelBinding.Property(x => x.CreditStatus).HasConversion<string>();
        modelBinding.OwnsMany(x => x.AccountAddresses, AccountAddresses =>
                                                       {
                                                           AccountAddresses.WithOwner();
                                                           AccountAddresses.Property(x => x.AddressType).HasConversion<string>();
                                                           AccountAddresses.Property(x => x.Country).HasConversion<string>();
                                                           AccountAddresses.Property(x => x.Province).HasConversion<string>();
                                                       });

        modelBinding.OwnsMany(x => x.Contacts, AccountContacts =>
                                               {
                                                   AccountContacts.WithOwner();
                                                   AccountContacts.Property(x => x.SignatoryType).HasConversion<string>();
                                                   AccountContacts.OwnsOne(x => x.AccountContactAddress, AccountContactAddressBuilder =>
                                                                                                         {
                                                                                                             AccountContactAddressBuilder.WithOwner();
                                                                                                             AccountContactAddressBuilder.Property(x => x.Country).HasConversion<string>();
                                                                                                             AccountContactAddressBuilder.Property(x => x.Province).HasConversion<string>();
                                                                                                         });
                                               });
    }
}

public class AccountAddressEntityMap : EntityMapper<AccountAddressEntity>, IEntityMapper<AccountAddressEntity>
{
    public override void Configure(EntityTypeBuilder<AccountAddressEntity> modelBinding)
    {
    }
}

public class AccountContactEntityMap : EntityMapper<AccountContactEntity>, IEntityMapper<AccountContactEntity>
{
    public override void Configure(EntityTypeBuilder<AccountContactEntity> modelBinding)
    {
    }
}
