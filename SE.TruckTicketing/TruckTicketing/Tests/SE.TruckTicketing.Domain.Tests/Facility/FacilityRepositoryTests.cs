using System;
using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.Facilities;

using Trident.Data.Contracts;
using Trident.Search;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Domain.Tests.Facility;

[TestClass]
public class FacilityRepositoryTests
{
    [TestMethod("ApplyKeywordSearch should apply keywords.")]
    public void FacilityRepository_ApplyKeywordSearch_WithKeyword()
    {
        // arrange
        var scope = new DefaultScope();
        var db = scope.GenerateList();

        // act
        var resultingQuery = scope.InstanceUnderTest.ApplyKeywordSearchImpl(db.AsQueryable(), "Amelia SWD");
        var resultingCollection = resultingQuery.ToList();

        // assert
        resultingCollection.Count().Should().Be(1);
        resultingCollection[0].Name.Should().Be("Amelia SWD");
    }

    [TestMethod("ApplyKeywordSearch should skip incorrect sources.")]
    public void FacilityRepository_ApplyKeywordSearch_IncorrectSource()
    {
        // arrange
        var scope = new DefaultScope();
        var db = new List<string>
        {
            "AMSWD",
            "ATSWD",
        };

        // act
        var resultingQuery = scope.InstanceUnderTest.ApplyKeywordSearchImpl(db.AsQueryable(), "ATSWD");
        var resultingCollection = resultingQuery.ToList();

        // assert
        resultingCollection.Count.Should().Be(2);
        resultingCollection[0].Should().Be("AMSWD");
    }

    private class DefaultScope : TestScope<FacilitiesRepository>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(ResultsBuilder.Object, QueryBuilder.Object, AbstractContextFactory.Object, QueryableHelper.Object);
        }

        public Mock<ISearchResultsBuilder> ResultsBuilder { get; } = new();

        public Mock<ISearchQueryBuilder> QueryBuilder { get; } = new();

        public Mock<IAbstractContextFactory> AbstractContextFactory { get; } = new();

        public Mock<IQueryableHelper> QueryableHelper { get; } = new();

        public List<FacilityEntity> GenerateList()
        {
            return new()
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Amelia SWD",
                    SiteId = "AMSWD",
                    Type = FacilityType.Swd,
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Athabasca SWD",
                    SiteId = "ATSWD",
                    Type = FacilityType.Swd,
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Brazeau West FST",
                    SiteId = "BSFST",
                    Type = FacilityType.Fst,
                },
            };
        }
    }
}
