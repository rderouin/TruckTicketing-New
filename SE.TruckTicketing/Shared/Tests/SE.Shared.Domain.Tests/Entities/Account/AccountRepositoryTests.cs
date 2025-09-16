using System;
using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using Moq;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.Account;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Data.Contracts;
using Trident.Search;
using Trident.Testing.TestScopes;

namespace SE.Shared.Domain.Tests.Entities.Account;

[TestClass]
public class AccountRepositoryTests
{
    [TestMethod("ApplyKeywordSearch should apply keywords.")]
    public void AccountRepository_ApplyKeywordSearch_WithKeyword()
    {
        // arrange
        var scope = new DefaultScope();
        var db = scope.GenerateList();

        // act
        var resultingQuery = scope.InstanceUnderTest.ApplyKeywordSearchImpl(db.AsQueryable(), "kuhlman");
        var resultingCollection = resultingQuery.ToList();

        // assert
        resultingCollection.Count.Should().Be(1);
        resultingCollection[0].Name.Should().Be("Hayes - Koss");
    }

    [TestMethod("ApplyKeywordSearch should skip incorrect sources.")]
    public void AccountRepository__ApplyKeywordSearch_IncorrectSource()
    {
        // arrange
        var scope = new DefaultScope();
        var db = new List<string>
        {
            "test1",
            "test2",
        };

        // act
        var resultingQuery = scope.InstanceUnderTest.ApplyKeywordSearchImpl(db.AsQueryable(), "kuhlman");
        var resultingCollection = resultingQuery.ToList();

        // assert
        resultingCollection.Count.Should().Be(2);
        resultingCollection[0].Should().Be("test1");
    }

    private class DefaultScope : TestScope<AccountRepository>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(ResultsBuilder.Object, QueryBuilder.Object, AbstractContextFactory.Object, QueryableHelper.Object);
        }

        public Mock<ISearchResultsBuilder> ResultsBuilder { get; } = new();

        public Mock<ISearchQueryBuilder> QueryBuilder { get; } = new();

        public Mock<IAbstractContextFactory> AbstractContextFactory { get; } = new();

        public Mock<IQueryableHelper> QueryableHelper { get; } = new();

        public List<AccountEntity> GenerateList()
        {
            return new()
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    IsShowAccount = true,
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
                        },
                        new()
                        {
                            Email = "Nelle.Legros@hotmail.com",
                            PhoneNumber = "213-632-4251",
                            Name = "Ted Abshire II",
                        },
                    },
                },
            };
        }
    }
}
