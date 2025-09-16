using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

using SE.Shared.Domain.Entities.Account;

using Trident.Data.Contracts;
using Trident.Search;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Domain.Tests.Account;

[TestClass]
public class AccountRepositoryTests
{
    private const string TwinFallsName = "Twin Falls Oil Services, LLC";

    [TestMethod]
    public void Bug13145_IsShowAccountFalse_NotFoundInResults()
    {
        //setup
        var scope = new DefaultScope();
        var databaseList = scope.GenerateOneNonIsShowAccount();

        //execute
        var resultingQuery = scope.InstanceUnderTest.ApplyKeywordSearchImpl(databaseList.AsQueryable(), "twin");
        var resultingCollection = resultingQuery.ToList();

        //assert
        resultingCollection.Count().Should().Be(0);
    }

    [TestMethod]
    public void BUG13145_ApplyKeywordSearchImpl_TwinFallsIsFoundInResults()
    {
        //setup
        var scope = new DefaultScope();
        var databaseList = scope.GenerateThreeAccounts();

        //execute
        var resultingQuery = scope.InstanceUnderTest.ApplyKeywordSearchImpl(databaseList.AsQueryable(), "twin");
        var resultingCollection = resultingQuery.ToList();

        //assert
        resultingCollection.Count().Should().Be(1);
        resultingCollection[0].Name.Should().Be(TwinFallsName);
    }

    [TestMethod]
    public void BUG13145_ApplyKeywordSearchImpl_NoResultsDoesNotThrowError()
    {
        //setup
        var scope = new DefaultScope();
        var databaseList = scope.GenerateThreeAccounts();

        //execute
        var resultingQuery = scope.InstanceUnderTest.ApplyKeywordSearchImpl(databaseList.AsQueryable(), "yxz");
        var resultingCollection = resultingQuery.ToList();

        //assert
        resultingCollection.Count().Should().Be(0);
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

        public List<AccountEntity> GenerateOneNonIsShowAccount()
        {
            var list = new List<AccountEntity>();

            var cnrl = new AccountEntity()
            {
                Name = TwinFallsName,
                NickName = null,
                AccountNumber = "A20000273",
                LegalEntity = "SESU",
                AccountPrimaryContactName = null,
                AccountPrimaryContactPhoneNumber = null,
                CustomerNumber = null,
                IsShowAccount = null
            };

            list.Add(cnrl);

            return list;
        }

        public List<AccountEntity> GenerateThreeAccounts()
        {
            var list = new List<AccountEntity>();

            var twinFalls = new AccountEntity
            {
                Name = TwinFallsName,
                NickName = null,
                AccountNumber = "A20000273",
                LegalEntity = "SESU",
                AccountPrimaryContactName = null,
                AccountPrimaryContactPhoneNumber = null,
                CustomerNumber = null,
                IsShowAccount = true
            };

            list.Add(twinFalls);

            var harvest = new AccountEntity
            {
                Name = "Harvest Operations Corp.",
                NickName = "haro",
                AccountNumber = "A1001697",
                LegalEntity = "SESC",
                AccountPrimaryContactName = "Accounts Payable",
                AccountPrimaryContactPhoneNumber = "780-833-4037",
                CustomerNumber = "C0020",
                IsShowAccount = true
            };

            list.Add(harvest);

            var abs = new AccountEntity
            {
                Name = "ABS Excavating Ltd.",
                NickName = null,
                AccountNumber = "A1005977",
                LegalEntity = "SESC",
                AccountPrimaryContactName = null,
                AccountPrimaryContactPhoneNumber = null,
                CustomerNumber = null,
                IsShowAccount = true
            };

            list.Add(abs);


            return list;
        }
    }
}
