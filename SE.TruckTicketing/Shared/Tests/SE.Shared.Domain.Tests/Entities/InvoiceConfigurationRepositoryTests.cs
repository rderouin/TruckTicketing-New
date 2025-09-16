using System;
using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using Moq;

using SE.Shared.Domain.Entities.InvoiceConfiguration;

using Trident.Data.Contracts;
using Trident.Domain;
using Trident.Search;
using Trident.Testing.TestScopes;

namespace SE.Shared.Domain.Tests.Entities;

[TestClass]
public class InvoiceConfigurationRepositoryTests
{
    [TestMethod("ApplyKeywordSearch should apply keywords.")]
    public void AccountRepository_ApplyKeywordSearch_WithCustomerNameAsKeyword()
    {
        // arrange
        var scope = new DefaultScope();
        var db = scope.GenerateInvoiceConfiguration();

        // act
        var resultingQuery = scope.InstanceUnderTest.ApplyKeywordSearchImpl(db.AsQueryable(), "Generator/Customer");
        var resultingCollection = resultingQuery.ToList();

        // assert
        resultingCollection.Count.Should().Be(1);
        resultingCollection[0].Name.Should().Be("TT Petro Canada");
    }
    [TestMethod("ApplyKeywordSearch should apply keywords.")]
    public void AccountRepository_ApplyKeywordSearch_WithInvoiceConfigurationNameAsKeyword()
    {
        // arrange
        var scope = new DefaultScope();
        var db = scope.GenerateInvoiceConfiguration();

        // act
        var resultingQuery = scope.InstanceUnderTest.ApplyKeywordSearchImpl(db.AsQueryable(), "SHELL");
        var resultingCollection = resultingQuery.ToList();

        // assert
        resultingCollection.Count.Should().Be(1);
        resultingCollection[0].Name.Should().Be("TT Shell Corp");
    }
    [TestMethod("ApplyKeywordSearch should apply keywords.")]
    public void AccountRepository_ApplyKeywordSearch_WithDescriptionAsKeyword()
    {
        // arrange
        var scope = new DefaultScope();
        var db = scope.GenerateInvoiceConfiguration();

        // act
        var resultingQuery = scope.InstanceUnderTest.ApplyKeywordSearchImpl(db.AsQueryable(), "new customer");
        var resultingCollection = resultingQuery.ToList();

        // assert
        resultingCollection.Count.Should().Be(1);
        resultingCollection[0].Name.Should().Be("TT Shell Corp");
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
        var resultingQuery = scope.InstanceUnderTest.ApplyKeywordSearchImpl(db.AsQueryable(), "Generator/Customer");
        var resultingCollection = resultingQuery.ToList();

        // assert
        resultingCollection.Count.Should().Be(2);
        resultingCollection[0].Should().Be("test1");
    }

    private class DefaultScope : TestScope<InvoiceConfigurationRepository>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(ResultsBuilder.Object, QueryBuilder.Object, AbstractContextFactory.Object, QueryableHelper.Object);
        }

        public Mock<ISearchResultsBuilder> ResultsBuilder { get; } = new();

        public Mock<ISearchQueryBuilder> QueryBuilder { get; } = new();

        public Mock<IAbstractContextFactory> AbstractContextFactory { get; } = new();

        public Mock<IQueryableHelper> QueryableHelper { get; } = new();

        public List<InvoiceConfigurationEntity> GenerateInvoiceConfiguration()
        {
            return new()
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    AllFacilities = true,
                    AllServiceTypes = true,
                    AllSourceLocations = true,
                    AllSubstances = true,
                    AllWellClassifications = true,
                    CatchAll = false,
                    CustomerId = Guid.NewGuid(),
                    CustomerName = "QQ Generator/Customer 01",
                    Description = "This is test invoice configuration",
                    IncludeInternalDocumentAttachment = false,
                    IncludeExternalDocumentAttachment = false,
                    IsSplitByFacility = false,
                    IsSplitByServiceType = false,
                    IsSplitBySourceLocation = false,
                    IsSplitBySubstance = false,
                    IsSplitByWellClassification = false,
                    Name = "TT Petro Canada",
                    Facilities = null,
                    ServiceTypes = null,
                    SourceLocationIdentifier = null,
                    SourceLocations = null,
                    SplitEdiFieldDefinitions = null,
                    SplittingCategories = null,
                    WellClassifications = new PrimitiveCollection<string>()
                    {
                        Key = Guid.NewGuid(),
                        List = new List<string>(){"Drilling"}
                    }
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    AllFacilities = true,
                    AllServiceTypes = true,
                    AllSourceLocations = true,
                    AllSubstances = true,
                    AllWellClassifications = true,
                    CatchAll = false,
                    CustomerId = Guid.NewGuid(),
                    CustomerName = "QQ Customer 555",
                    Description = "New Invoice Configuration added with new customer",
                    IncludeInternalDocumentAttachment = false,
                    IncludeExternalDocumentAttachment = false,
                    IsSplitByFacility = false,
                    IsSplitByServiceType = false,
                    IsSplitBySourceLocation = false,
                    IsSplitBySubstance = false,
                    IsSplitByWellClassification = false,
                    Name = "TT Shell Corp",
                    Facilities = null,
                    ServiceTypes = null,
                    SourceLocationIdentifier = null,
                    SourceLocations = null,
                    SplitEdiFieldDefinitions = null,
                    SplittingCategories = null,
                    WellClassifications = new PrimitiveCollection<string>()
                    {
                        Key = Guid.NewGuid(),
                        List = new List<string>(){"Drilling"}
                    }
                },
            };
        }
    }
}
