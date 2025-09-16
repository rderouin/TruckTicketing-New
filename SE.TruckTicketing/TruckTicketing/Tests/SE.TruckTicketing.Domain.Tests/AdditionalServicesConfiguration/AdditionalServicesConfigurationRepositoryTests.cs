using System;
using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.AdditionalServicesConfiguration;

using Trident.Data.Contracts;
using Trident.Search;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Domain.Tests.AdditionalServicesConfiguration;

[TestClass]
public class AdditionalServicesConfigurationRepositoryTests
{
    [TestMethod("ApplyKeywordSearch should apply keywords.")]
    public void AdditionalServicesConfigurationRepository_ApplyKeywordSearch_WithKeyword()
    {
        // arrange
        var scope = new DefaultScope();
        var db = scope.GenerateList();

        // act
        var resultingQuery = scope.InstanceUnderTest.ApplyKeywordSearchImpl(db.AsQueryable(), $"{nameof(AdditionalServicesConfigurationEntity.Name)} - 1");
        var resultingCollection = resultingQuery.ToList();

        // assert
        resultingCollection.Count().Should().Be(1);
        resultingCollection[0].Name.Should().Be($"{nameof(AdditionalServicesConfigurationEntity.Name)} - 1");
    }

    [TestMethod("ApplyKeywordSearch should skip incorrect sources.")]
    public void AdditionalServicesConfigurationRepository_ApplyKeywordSearch_IncorrectSource()
    {
        // arrange
        var scope = new DefaultScope();
        var db = new List<string>
        {
            $"{nameof(AdditionalServicesConfigurationEntity.SiteId)} - 1",
            $"{nameof(AdditionalServicesConfigurationEntity.SiteId)} - 2",
        };

        // act
        var resultingQuery = scope.InstanceUnderTest.ApplyKeywordSearchImpl(db.AsQueryable(), $"{nameof(AdditionalServicesConfigurationEntity.SiteId)} - 2");
        var resultingCollection = resultingQuery.ToList();

        // assert
        resultingCollection.Count.Should().Be(2);
        resultingCollection[0].Should().Be($"{nameof(AdditionalServicesConfigurationEntity.SiteId)} - 1");
    }

    private class DefaultScope : TestScope<AdditionalServicesConfigurationRepository>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(ResultsBuilder.Object, QueryBuilder.Object, AbstractContextFactory.Object, QueryableHelper.Object);
        }

        public Mock<ISearchResultsBuilder> ResultsBuilder { get; } = new();

        public Mock<ISearchQueryBuilder> QueryBuilder { get; } = new();

        public Mock<IAbstractContextFactory> AbstractContextFactory { get; } = new();

        public Mock<IQueryableHelper> QueryableHelper { get; } = new();

        public List<AdditionalServicesConfigurationEntity> GenerateList()
        {
            return new()
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = $"{nameof(AdditionalServicesConfigurationEntity.Name)} - 1",
                    CustomerName = $"{nameof(AdditionalServicesConfigurationEntity.CustomerName)} - 1",
                    FacilityName = $"{nameof(AdditionalServicesConfigurationEntity.FacilityName)} - 1",
                    SiteId = $"{nameof(AdditionalServicesConfigurationEntity.SiteId)} - 1",
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = $"{nameof(AdditionalServicesConfigurationEntity.Name)} - 2",
                    CustomerName = $"{nameof(AdditionalServicesConfigurationEntity.CustomerName)} - 2",
                    FacilityName = $"{nameof(AdditionalServicesConfigurationEntity.FacilityName)} - 2",
                    SiteId = $"{nameof(AdditionalServicesConfigurationEntity.SiteId)} - 2",
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = $"{nameof(AdditionalServicesConfigurationEntity.Name)} - 3",
                    CustomerName = $"{nameof(AdditionalServicesConfigurationEntity.CustomerName)} - 3",
                    FacilityName = $"{nameof(AdditionalServicesConfigurationEntity.FacilityName)} - 3",
                    SiteId = $"{nameof(AdditionalServicesConfigurationEntity.SiteId)} - 3",
                },
            };
        }
    }
}
