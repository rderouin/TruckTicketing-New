using System;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.Account.Tasks;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Extensions;
using Trident.Testing.TestScopes;
using Trident.Workflow;

namespace SE.Shared.Domain.Tests.Entities.Account;

[TestClass]
public class AccountPrimaryAddressCheckerTaskTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldRun_OnlyWhenAccountAddressExists()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext(null, scope.ValidAccountEntity);

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);
        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.BeforeUpdate | OperationStage.BeforeInsert);
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldNotRun_IfNoAccountAddressExists()
    {
        // arrange
        var scope = new DefaultScope();
        var noAccountAddressPresent = scope.ValidAccountEntity.Clone();
        noAccountAddressPresent.AccountAddresses.Clear();
        var context = scope.CreateValidAccountContext(scope.ValidAccountEntity, noAccountAddressPresent);

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);
        // assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_MultipleAccountAddressesMarkerPrimary_IfNoNewAccountAddressAdded_AnyOneExistingAddressSetToPrimary()
    {
        // arrange
        var scope = new DefaultScope();

        var originalAccount = scope.ValidAccountEntity.Clone();
        var targetAccount = scope.ValidAccountEntity.Clone();
        foreach (var accountAddresses in targetAccount.AccountAddresses)
        {
            accountAddresses.IsPrimaryAddress = true;
        }

        var context = scope.CreateValidAccountContext(originalAccount, targetAccount);

        // act
        var result = await scope.InstanceUnderTest.Run(context);
        var countOfPrimaryAddresses = context.Target.AccountAddresses.Count(x => x.IsPrimaryAddress);

        // assert
        result.Should().BeTrue();
        countOfPrimaryAddresses.Should().Be(1);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_MultipleAccountAddressesMarkerPrimary_IfNewAccountAddressAdded_AnyOneOfNewAddressSetToPrimary()
    {
        // arrange
        var scope = new DefaultScope();
        var originalAccount = scope.ValidAccountEntity.Clone();
        foreach (var accountAddresses in originalAccount.AccountAddresses)
        {
            accountAddresses.IsPrimaryAddress = true;
        }

        var accountWithNewAddresses = scope.ValidAccountEntity.Clone();
        accountWithNewAddresses.AccountAddresses.AddRange(scope.AddNewAccounts());

        var context = scope.CreateValidAccountContext(originalAccount, accountWithNewAddresses);

        // act
        var result = await scope.InstanceUnderTest.Run(context);
        var countOfPrimaryAddresses = context.Target.AccountAddresses.Count(x => x.IsPrimaryAddress);

        // assert
        result.Should().BeTrue();
        countOfPrimaryAddresses.Should().Be(1);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_NoAccountAddressesMarkerPrimary_IfNoNewAccountAddressAdded_AnyOneExistingAddressSetToPrimary()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext(scope.ValidAccountEntity, scope.ValidAccountEntity);

        // act
        var result = await scope.InstanceUnderTest.Run(context);
        var countOfPrimaryAddresses = context.Target.AccountAddresses.Count(x => x.IsPrimaryAddress);
        // assert
        result.Should().BeTrue();
        countOfPrimaryAddresses.Should().Be(1);
    }

    private class DefaultScope : TestScope<AccountPrimaryAddressCheckerTask>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new();
        }

        public AccountEntity ValidAccountEntity =>
            new()
            {
                Id = Guid.NewGuid(),
                AccountNumber = "59260670",
                AccountPrimaryContactEmail = "Teresa_Daugherty63@yahoo.com",
                AccountPrimaryContactName = "Bradford Lang",
                AccountPrimaryContactPhoneNumber = "534-732-2170",
                AccountStatus = AccountStatus.Open,
                BillingTransferRecipientId = null,
                BillingTransferRecipientName = null,
                BillingType = BillingType.CreditCard,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "Panth Shah",
                CreatedById = Guid.NewGuid().ToString(),
                CreditLimit = 5000,
                CreditStatus = CreditStatus.Approved,
                CustomerNumber = "33279271",
                EnableNewThirdPartyAnalytical = true,
                EnableNewTruckingCompany = true,
                LegalEntity = "Entity",
                Name = "Hayes - Koss",
                NickName = "Kuhlman Inc",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "Panth Shah",
                UpdatedById = Guid.NewGuid().ToString(),
                WatchListStatus = WatchListStatus.Red,
                AccountAddresses = new()
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        City = "Moon",
                        Street = "1210 Landing Ln.",
                        Country = CountryCode.US,
                        Province = StateProvince.PA,
                        ZipCode = "15108",
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        City = "Brampton",
                        Street = "66 Kalahari Rd",
                        Country = CountryCode.CA,
                        Province = StateProvince.ON,
                        ZipCode = "L6R2P2",
                    },
                },
                AccountTypes = new()
                {
                    Key = Guid.NewGuid(),
                },
                Contacts = new()
                {
                    new()
                    {
                        Email = "Salvador.Fahey0@yahoo.com",
                        PhoneNumber = "271-668-1312",
                        Name = "Jonathan Collins",
                    },
                    new()
                    {
                        Email = "Nelle.Legros@hotmail.com",
                        PhoneNumber = "213-632-4251",
                        Name = "Ted Abshire II",
                    },
                },
            };

        public AccountAddressEntity[] AddNewAccounts()
        {
            return new AccountAddressEntity[]
            {
                new()
                {
                    Id = default,
                    City = "McDonald",
                    Street = "5110 Forest Ridge Dr.",
                    Country = CountryCode.US,
                    Province = StateProvince.PA,
                    ZipCode = "15057",
                },
                new()
                {
                    Id = default,
                    City = "Markham",
                    Street = "32 Clegg Rd.",
                    Country = CountryCode.CA,
                    Province = StateProvince.ON,
                    ZipCode = "L7A5C1",
                },
            };
        }

        public BusinessContext<AccountEntity> CreateValidAccountContext(AccountEntity target, AccountEntity original)
        {
            return new(original, target);
        }
    }
}
