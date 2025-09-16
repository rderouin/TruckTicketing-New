using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.Account.Tasks;
using SE.Shared.Domain.Entities.Sequences;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Testing.TestScopes;
using Trident.Workflow;

namespace SE.Shared.Domain.Tests.Entities.Account;

[TestClass]
public class AccountNumberGeneratorTaskTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldRun_OnlyWhenAccountBusinessContext_Insert()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext(scope.ValidAccountEntity, new());
        context.Operation = Operation.Insert;
        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);
        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.BeforeInsert);
        shouldRun.Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldNotRun_IfAccountBusinessContext_NotInsert()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext(scope.ValidAccountEntity, new());
        context.Operation = Operation.Update;

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);
        // assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldNotRun_IfAccountNumber_AlreadyExist()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext(scope.ValidAccountEntity, new());
        context.Operation = Operation.Insert;
        context.Target.AccountNumber = "10000089";

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);
        // assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldRun_IfAccountNumber_IsNullOrEmpty()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidAccountContext(scope.ValidAccountEntity, new());
        context.Operation = Operation.Insert;

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);
        // assert
        shouldRun.Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldRun_ValidLegalEntity_AccountNumberGenerated()
    {
        // arrange
        var scope = new DefaultScope();
        scope.SetSequenceGeneratorManager();
        var context = scope.CreateValidAccountContext(scope.ValidAccountEntity, new());

        // act
        var result = await scope.InstanceUnderTest.Run(context);
        // assert
        result.Should().BeTrue();
        context.Target.AccountNumber.Should().NotBeNullOrEmpty();
    }

    private class DefaultScope : TestScope<AccountNumberGeneratorTask>
    {
        public readonly AccountEntity ValidAccountEntity = new()
        {
            Id = Guid.NewGuid(),
            AccountNumber = string.Empty,
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
            LegalEntityId = Guid.Parse("ae62f28e-fd0b-4594-b235-0e254bc4771a"),
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
                    City = "Moon",
                    Street = "1210 Landing Ln.",
                    Country = CountryCode.US,
                    Province = StateProvince.PA,
                    ZipCode = "15108",
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
                    IsActive = true,
                    ContactFunctions = new()
                    {
                        Key = Guid.NewGuid(),
                        List = new() { AccountContactFunctions.BillingContact.ToString() },
                    },
                    AccountContactAddress = new()
                    {
                        City = "Moon",
                        Street = "1210 Landing Ln.",
                        Country = CountryCode.US,
                        Province = StateProvince.PA,
                        ZipCode = "15108",
                    },
                },
                new()
                {
                    Email = "Nelle.Legros@hotmail.com",
                    PhoneNumber = "213-632-4251",
                    Name = "Ted Abshire II",
                    IsActive = true,
                    ContactFunctions = new()
                    {
                        Key = Guid.NewGuid(),
                        List = new() { AccountContactFunctions.General.ToString() },
                    },
                    AccountContactAddress = new()
                    {
                        City = "Moon",
                        Street = "1210 Landing Ln.",
                        Country = CountryCode.US,
                        Province = StateProvince.PA,
                        ZipCode = "15108",
                    },
                },
            },
        };

        public DefaultScope()
        {
            InstanceUnderTest = new(SequenceNumberGeneratorMock.Object);
        }

        public Mock<ISequenceNumberGenerator> SequenceNumberGeneratorMock { get; } = new();

        public BusinessContext<AccountEntity> CreateValidAccountContext(AccountEntity target, AccountEntity original)
        {
            return new(target, original);
        }

        public void SetSequenceGeneratorManager()
        {
            ConfigureSequenceGeneratorManagerMock(SequenceNumberGeneratorMock);
        }

        private void ConfigureSequenceGeneratorManagerMock(Mock<ISequenceNumberGenerator> mock)
        {
            mock.Setup(sequence => sequence.GenerateSequenceNumbers(It.IsAny<string>(),
                                                                    It.IsAny<string>(),
                                                                    It.IsAny<int>(),
                                                                    It.IsAny<string>(),
                                                                    It.IsAny<string>())).Returns(GenerateSequenceNumber());
        }

        private IAsyncEnumerable<string> GenerateSequenceNumber()
        {
            return new List<string>
            {
                "TAFA-10001-LF",
            }.ToAsyncEnumerable();
        }
    }
}
