using System;
using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.BillingService.Domain.Entities.InvoiceExchange;

using Trident.Data.Contracts;
using Trident.Search;
using Trident.Testing.TestScopes;

// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable PossibleNullReferenceException

namespace SE.BillingService.Domain.Tests.Entities.InvoiceExchange;

[TestClass]
public class InvoiceExchangeRepositoryTests
{
    [TestMethod("ApplyKeywordSearch should apply keywords.")]
    public void InvoiceExchangeRepository_ApplyKeywordSearch_WithKeyword()
    {
        // arrange
        var scope = new DefaultScope();
        var db = scope.GenerateList();

        // act
        var resultingQuery = scope.InstanceUnderTest.ApplyKeywordSearchImpl(db.AsQueryable(), "d57");
        var resultingCollection = resultingQuery.ToList();

        // assert
        resultingCollection.Count.Should().Be(1);
        resultingCollection[0].PlatformCode.Should().Be("OI");
    }

    [TestMethod("ApplyKeywordSearch should skip incorrect sources.")]
    public void InvoiceExchangeRepository_ApplyKeywordSearch_IncorrectSource()
    {
        // arrange
        var scope = new DefaultScope();
        var db = new List<string>
        {
            "test1",
            "test2",
        };

        // act
        var resultingQuery = scope.InstanceUnderTest.ApplyKeywordSearchImpl(db.AsQueryable(), "d57");
        var resultingCollection = resultingQuery.ToList();

        // assert
        resultingCollection.Count.Should().Be(2);
        resultingCollection[0].Should().Be("test1");
    }

    private class DefaultScope : TestScope<InvoiceExchangeRepository>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(ResultsBuilder.Object, QueryBuilder.Object, AbstractContextFactory.Object, QueryableHelper.Object);
        }

        public Mock<ISearchResultsBuilder> ResultsBuilder { get; } = new();

        public Mock<ISearchQueryBuilder> QueryBuilder { get; } = new();

        public Mock<IAbstractContextFactory> AbstractContextFactory { get; } = new();

        public Mock<IQueryableHelper> QueryableHelper { get; } = new();

        public List<InvoiceExchangeEntity> GenerateList()
        {
            return new()
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    PlatformCode = "ACTIAN",
                    BusinessStreamName = "MI",
                    LegalEntityName = "LE1",
                    BillingAccountName = "FB1",
                    BillingAccountNumber = "123",
                    BillingAccountDunsNumber = "D23",
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    PlatformCode = "CORTEX",
                    BusinessStreamName = "MX",
                    LegalEntityName = "LE1",
                    BillingAccountName = "II1",
                    BillingAccountNumber = "456",
                    BillingAccountDunsNumber = "D56",
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    PlatformCode = "JOBUTRAX",
                    BusinessStreamName = "BX",
                    LegalEntityName = "LE1",
                    BillingAccountName = "VP5",
                    BillingAccountNumber = "789",
                    BillingAccountDunsNumber = "D89",
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    PlatformCode = "OI",
                    BusinessStreamName = "MI",
                    LegalEntityName = "LE2",
                    BillingAccountName = "PG8",
                    BillingAccountNumber = "357",
                    BillingAccountDunsNumber = "D57",
                },
            };
        }
    }
}
