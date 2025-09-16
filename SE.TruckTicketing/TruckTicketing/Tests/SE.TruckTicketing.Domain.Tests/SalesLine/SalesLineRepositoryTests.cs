using System;
using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.SalesLine;

using Trident.Data.Contracts;
using Trident.Search;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Domain.Tests.SalesLine;

[TestClass]
public class SalesLineRepositoryTests
{
    [TestMethod("ApplyKeywordSearch should apply keywords.")]
    public void SalesLineRepository_ApplyKeywordSearch_WithKeyword()
    {
        // arrange
        var scope = new DefaultScope();
        var db = scope.GenerateList();

        // act
        var resultingQuery = scope.InstanceUnderTest.ApplyKeywordSearchImpl(db.AsQueryable(), $"{nameof(SalesLineEntity.SalesLineNumber)} - 123");
        var resultingCollection = resultingQuery.ToList();

        // assert
        resultingCollection.Count().Should().Be(1);
        resultingCollection[0].SalesLineNumber.Should().Be($"{nameof(SalesLineEntity.SalesLineNumber)} - 123");
    }

    [TestMethod("ApplyKeywordSearch should skip incorrect sources.")]
    public void SalesLineRepository_ApplyKeywordSearch_IncorrectSource()
    {
        // arrange
        var scope = new DefaultScope();
        var db = new List<string>
        {
            $"{nameof(SalesLineEntity.CustomerName)} - 123",
            $"{nameof(SalesLineEntity.CustomerName)} - 456",
        };

        // act
        var resultingQuery = scope.InstanceUnderTest.ApplyKeywordSearchImpl(db.AsQueryable(), $"{nameof(SalesLineEntity.CustomerName)} - 456");
        var resultingCollection = resultingQuery.ToList();

        // assert
        resultingCollection.Count.Should().Be(2);
        resultingCollection[0].Should().Be($"{nameof(SalesLineEntity.CustomerName)} - 123");
    }

    private class DefaultScope : TestScope<SalesLineRepository>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(ResultsBuilder.Object, QueryBuilder.Object, AbstractContextFactory.Object, QueryableHelper.Object);
        }

        public Mock<ISearchResultsBuilder> ResultsBuilder { get; } = new();

        public Mock<ISearchQueryBuilder> QueryBuilder { get; } = new();

        public Mock<IAbstractContextFactory> AbstractContextFactory { get; } = new();

        public Mock<IQueryableHelper> QueryableHelper { get; } = new();

        public List<SalesLineEntity> GenerateList()
        {
            return new()
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    SalesLineNumber = $"{nameof(SalesLineEntity.SalesLineNumber)} - 123",
                    TruckTicketNumber = $"{nameof(SalesLineEntity.SalesLineNumber)} - 123",
                    CustomerName = $"{nameof(SalesLineEntity.CustomerName)} - 123",
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    SalesLineNumber = $"{nameof(SalesLineEntity.SalesLineNumber)} - 456",
                    TruckTicketNumber = $"{nameof(SalesLineEntity.SalesLineNumber)} - 456",
                    CustomerName = $"{nameof(SalesLineEntity.CustomerName)} - 456",
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    SalesLineNumber = $"{nameof(SalesLineEntity.SalesLineNumber)} - 789",
                    TruckTicketNumber = $"{nameof(SalesLineEntity.SalesLineNumber)} - 789",
                    CustomerName = $"{nameof(SalesLineEntity.CustomerName)} - 789",
                },
            };
        }
    }
}
