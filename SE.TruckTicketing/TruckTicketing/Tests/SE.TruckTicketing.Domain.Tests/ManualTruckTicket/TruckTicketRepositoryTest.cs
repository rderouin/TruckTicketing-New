using System;
using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.TruckTicket;

using Trident.Data.Contracts;
using Trident.EFCore;
using Trident.Search;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Domain.Tests.ManualTruckTicket;

[TestClass]
public class TruckTicketRepositoryTest
{
    [TestMethod]
    [TestCategory("Unit")]
    public void FacilityRepository_ScopeInvoke_ValidInstanceType()
    {
        // arrange
        var scope = new DefaultScope();
        // act / assert
        Assert.IsInstanceOfType(scope.InstanceUnderTest, typeof(CosmosEFCoreSearchRepositoryBase<TruckTicketEntity>));
    }

    [TestMethod("ApplyKeywordSearch should apply keywords.")]
    public void FacilityRepository_ApplyKeywordSearch_WithKeyword()
    {
        // arrange
        var scope = new DefaultScope();
        var db = scope.GenerateList();

        // act
        var resultingQuery = scope.InstanceUnderTest.ApplyKeywordSearchImpl(db.AsQueryable(), "kuhlman");
        var resultingCollection = resultingQuery.ToList();

        // assert
        resultingCollection.Count().Should().Be(1);
        resultingCollection[0].TruckingCompanyName.Should().Be("kuhlman");
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

    private class DefaultScope : TestScope<TruckTicketRepository>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(SearchResultBuilderMock.Object,
                                    SearchQueryBuilderMock.Object,
                                    AbstractContextFactoryMock.Object,
                                    QueryableHelper.Object);
        }

        public Mock<ISearchResultsBuilder> SearchResultBuilderMock { get; } = new();

        public Mock<ISearchQueryBuilder> SearchQueryBuilderMock { get; } = new();

        public Mock<IAbstractContextFactory> AbstractContextFactoryMock { get; } = new();

        public Mock<IQueryableHelper> QueryableHelper { get; } = new();

        public List<TruckTicketEntity> GenerateList()
        {
            return new()
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    TruckingCompanyName = "kuhlman",
                },
            };
        }
    }
}
