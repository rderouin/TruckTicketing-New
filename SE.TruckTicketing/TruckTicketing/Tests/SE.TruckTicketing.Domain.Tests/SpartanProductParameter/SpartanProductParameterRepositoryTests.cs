using System;
using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.TruckTicketing.Contracts.Constants.SpartanProductParameters;
using SE.TruckTicketing.Domain.Entities.SpartanProductParameters;

using Trident.Data.Contracts;
using Trident.Search;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Domain.Tests.SpartanProductParameter;

[TestClass]
public class SpartanProductParameterRepositoryTests
{
    [TestMethod("ApplyKeywordSearch should apply keywords.")]
    public void SpartanProductParameterRepository_ApplyKeywordSearch_WithKeyword()
    {
        // arrange
        var scope = new DefaultScope();
        var db = scope.GenerateList();

        // act
        var resultingQuery = scope.InstanceUnderTest.ApplyKeywordSearchImpl(db.AsQueryable(), "Product 1");
        var resultingCollection = resultingQuery.ToList();

        // assert
        resultingCollection.Count().Should().Be(1);
        resultingCollection[0].ProductName.Should().Be("Product 1");
    }

    [TestMethod("ApplyKeywordSearch should skip incorrect sources.")]
    public void InvoiceExchangeRepository_ApplyKeywordSearch_IncorrectSource()
    {
        // arrange
        var scope = new DefaultScope();
        var db = new List<string>
        {
            "MI",
            "PI",
        };

        // act
        var resultingQuery = scope.InstanceUnderTest.ApplyKeywordSearchImpl(db.AsQueryable(), "PI");
        var resultingCollection = resultingQuery.ToList();

        // assert
        resultingCollection.Count.Should().Be(2);
        resultingCollection[0].Should().Be("MI");
    }

    private class DefaultScope : TestScope<SpartanProductParameterRepository>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(ResultsBuilder.Object, QueryBuilder.Object, AbstractContextFactory.Object, QueryableHelper.Object);
        }

        public Mock<ISearchResultsBuilder> ResultsBuilder { get; } = new();

        public Mock<ISearchQueryBuilder> QueryBuilder { get; } = new();

        public Mock<IAbstractContextFactory> AbstractContextFactory { get; } = new();

        public Mock<IQueryableHelper> QueryableHelper { get; } = new();

        public List<SpartanProductParameterEntity> GenerateList()
        {
            return new()
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductName = "Product 1",
                    FluidIdentity = FluidIdentity.Sweet,
                    MinFluidDensity = 12.21,
                    MaxFluidDensity = 72.56,
                    MinWaterPercentage = 12.3456,
                    MaxWaterPercentage = 82.9876,
                    ShowDensity = false,
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductName = "Product 2",
                    FluidIdentity = FluidIdentity.Sour,
                    MinFluidDensity = 12.21,
                    MaxFluidDensity = 72.56,
                    MinWaterPercentage = 12.3456,
                    MaxWaterPercentage = 82.9876,
                    ShowDensity = false,
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductName = "Product 3",
                    FluidIdentity = FluidIdentity.Sweet,
                    MinFluidDensity = 12.21,
                    MaxFluidDensity = 72.56,
                    MinWaterPercentage = 12.3456,
                    MaxWaterPercentage = 82.9876,
                    ShowDensity = false,
                },
            };
        }
    }
}
