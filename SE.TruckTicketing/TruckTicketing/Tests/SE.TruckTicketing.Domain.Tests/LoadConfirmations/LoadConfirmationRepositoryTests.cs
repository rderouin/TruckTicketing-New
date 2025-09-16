using System;
using System.Collections.Generic;
using System.Linq;

using Castle.Components.DictionaryAdapter;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Data.Contracts;
using Trident.Search;
using Trident.Testing.TestScopes;

using CompareOperators = Trident.Search.CompareOperators;

namespace SE.TruckTicketing.Domain.Tests.LoadConfirmations;

[TestClass]
public class LoadConfirmationRepositoryTests
{
    [TestMethod]
    public void BuildCustomQuery_OpenAndVoidStatusTicketsFound()
    {
        //setup
        var scope = new DefaultScope();
        var oneOpenTicketDatabaseList = new List<LoadConfirmationEntity>()
        {
            new(){Number = "DCFST10001163-LC", InvoiceNumber = "DCFST10000777-IP", Status = LoadConfirmationStatus.Open},
            new(){Number = "HUCAV10000674-LC", InvoiceNumber = "HUCAV10000446-IP", SignatoryNames = "Sam King", Status = LoadConfirmationStatus.Void},
            new(){Number = "FCFST10000538-LC", InvoiceNumber = "FCFST10000526-IP", SignatoryNames = "Tony Furger,Brett Smith", Status = LoadConfirmationStatus.Posted},
        };

        //mimic what the user select for filters. Below, they selected Status of Open and Posted
        var axiom1 = new Axiom() { Key = "InvoiceStatus1", Value = nameof(LoadConfirmationStatus.Open) };
        var axiom2 = new Axiom() { Key = "InvoiceStatus2", Value = nameof(LoadConfirmationStatus.Posted) };
        var axiomFilter = BuildAxiomEqualsFilter(new() { axiom1, axiom2 });

        SearchCriteria criteria = new SearchCriteria { Filters = new() { { nameof(LoadConfirmationEntity.Status), axiomFilter } } };

        //execute
        var resultingQuery = LoadConfirmationRepository.BuildCustomQuery<LoadConfirmationEntity>(criteria, oneOpenTicketDatabaseList.AsQueryable(), false, false, false, null, null);
        var resultingCollection = resultingQuery.ToList();

        //assert
        resultingCollection.Count().Should().Be(2);
        resultingCollection[0].Number.Should().Be("DCFST10001163-LC");
        resultingCollection[1].Number.Should().Be("FCFST10000538-LC");
    }

    [TestMethod]
    public void BuildCustomQuery_OneOpenStatusTicketFound()
    {
        //setup
        var scope = new DefaultScope();
        var oneOpenTicketDatabaseList = new List<LoadConfirmationEntity>()
        {
            new(){Number = "DCFST10001163-LC", InvoiceNumber = "DCFST10000777-IP", Status = LoadConfirmationStatus.Open},
            new(){Number = "HUCAV10000674-LC", InvoiceNumber = "HUCAV10000446-IP", SignatoryNames = "Sam King", Status = LoadConfirmationStatus.Void},
            new(){Number = "FCFST10000538-LC", InvoiceNumber = "FCFST10000526-IP", SignatoryNames = "Tony Furger,Brett Smith", Status = LoadConfirmationStatus.Posted},
        };

        //mimic what the user select for filters. Below, they selected Status of Open 
        AxiomFilter axiomFilter = new AxiomFilter { Axioms = new EditableList<Axiom>() };
        axiomFilter.Axioms.Add(new() { Key = "InvoiceStatus1", Value = nameof(LoadConfirmationStatus.Open), Operator = CompareOperators.eq });
        SearchCriteria criteria = new SearchCriteria { Filters = new() { { nameof(LoadConfirmationEntity.Status), axiomFilter } } };

        //execute
        var resultingQuery = LoadConfirmationRepository.BuildCustomQuery<LoadConfirmationEntity>(criteria, oneOpenTicketDatabaseList.AsQueryable(), false, false, false, null, null);
        var resultingCollection = resultingQuery.ToList();

        //assert
        resultingCollection.Count().Should().Be(1);
        resultingCollection[0].Number.Should().Be("DCFST10001163-LC");
    }



    [TestMethod("ApplyKeywordSearch should apply keywords.")]
    public void LoadConfirmationRepository_ApplyKeywordSearch_NameNotFoundDoesNotThrowError()
    {
        // arrange
        var scope = new DefaultScope();
        var databaseList = scope.GenerateThreeSignatories();

        // act
        var resultingQuery = scope.InstanceUnderTest.ApplyKeywordSearchImpl(databaseList.AsQueryable(), "XYZ");
        var resultingCollection = resultingQuery.ToList();

        // assert
        resultingCollection.Count().Should().Be(0);
    }

    [TestMethod("ApplyKeywordSearch should apply keywords.")]
    public void LoadConfirmationRepository_ApplyKeywordSearch_WithSignatoryNamesOneNameKeyword()
    {
        // arrange
        var scope = new DefaultScope();
        var databaseList = scope.GenerateThreeSignatories();

        // act
        var resultingQuery = scope.InstanceUnderTest.ApplyKeywordSearchImpl(databaseList.AsQueryable(), "Angie");
        var resultingCollection = resultingQuery.ToList();

        // assert
        resultingCollection.Count().Should().Be(1);
        resultingCollection[0].SignatoryNames.Should().Be("Angie Thomson");
    }

    [TestMethod("ApplyKeywordSearch should apply keywords.")]
    public void LoadConfirmationRepository_ApplyKeywordSearch_WithTwoSignatoryNamesKeyword()
    {
        // arrange
        var scope = new DefaultScope();
        var databaseList = scope.GenerateListWithTwoSignatories();

        // act
        var resultingQuery = scope.InstanceUnderTest.ApplyKeywordSearchImpl(databaseList.AsQueryable(), "Angie");
        var resultingCollection = resultingQuery.ToList();

        // assert
        resultingCollection.Count().Should().Be(2);
        resultingCollection[0].SignatoryNames.Should().Be("Angie Thomson");
        resultingCollection[1].SignatoryNames.Should().Be("Sam King,Angie Thomson");
    }

    [TestMethod("ApplyKeywordSearch should apply keywords.")]
    public void LoadConfirmationRepository_ApplyKeywordSearch_WithInvoiceNumberKeyword()
    {
        // arrange
        var scope = new DefaultScope();
        var databaseList = scope.GenerateThreeSignatories();

        // act
        var resultingQuery = scope.InstanceUnderTest.ApplyKeywordSearchImpl(databaseList.AsQueryable(), "HUCAV10000446-IP");
        var resultingCollection = resultingQuery.ToList();

        // assert
        resultingCollection.Count().Should().Be(1);
        resultingCollection[0].InvoiceNumber.Should().Be("HUCAV10000446-IP");
    }

    [TestMethod("ApplyKeywordSearch should apply keywords.")]
    public void LoadConfirmationRepository_ApplyKeywordSearch_WithNumberKeyword()
    {
        // arrange
        var scope = new DefaultScope();
        var databaseList = scope.GenerateThreeSignatories();

        // act
        var resultingQuery = scope.InstanceUnderTest.ApplyKeywordSearchImpl(databaseList.AsQueryable(), "DCFST10001163-LC");
        var resultingCollection = resultingQuery.ToList();

        // assert
        resultingCollection.Count().Should().Be(1);
        resultingCollection[0].Number.Should().Be("DCFST10001163-LC");
    }


    private static AxiomFilter BuildAxiomEqualsFilter(List<Axiom> axioms)
    {
        AxiomFilter axiomFilter = new AxiomFilter { Axioms = new EditableList<Axiom>() };

        foreach (var axiom in axioms)
        {
            axiom.Operator = CompareOperators.eq;
            axiomFilter.Axioms.Add(axiom);
        }

        return axiomFilter;
    }

    private static AxiomFilter BuildAxiomEqualsFilter(Axiom axiom1, Axiom axiom2)
    {
        AxiomFilter axiomFilter = new AxiomFilter { Axioms = new EditableList<Axiom>() };
        axiomFilter.Axioms.Add(axiom1);

        axiomFilter.Axioms.Add(axiom2);

        return axiomFilter;
    }


    private class DefaultScope : TestScope<LoadConfirmationRepository>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(ResultsBuilder.Object, QueryBuilder.Object, AbstractContextFactory.Object, QueryableHelper.Object);
        }

        public Mock<ISearchResultsBuilder> ResultsBuilder { get; } = new();

        public Mock<ISearchQueryBuilder> QueryBuilder { get; } = new();

        public Mock<IAbstractContextFactory> AbstractContextFactory { get; } = new();

        public Mock<IQueryableHelper> QueryableHelper { get; } = new();

        public List<LoadConfirmationEntity> GenerateListWithTwoSignatories()
        {
            var angieThomson = new SignatoryContactEntity() { FirstName = "Angie", LastName = "Thomson" };
            var angieOnlySignatories = new List<SignatoryContactEntity> { angieThomson };
            var twoSignatories = new List<SignatoryContactEntity> { new() { FirstName = "Sam", LastName = "King" }, angieThomson };

            return new()
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Number = "DCFST10001163-LC",
                    InvoiceNumber = "DCFST10000777-IP",
                    Signatories = angieOnlySignatories
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Number = "HUCAV10000674-LC",
                    InvoiceNumber = "HUCAV10000446-IP",
                    Signatories = twoSignatories
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Number = "FCFST10000538-LC",
                    InvoiceNumber = "FCFST10000526-IP",
                    SignatoryNames = "Tony Furger,Brett Smith"
                },
            };
        }

        public List<LoadConfirmationEntity> GenerateThreeSignatories()
        {
            var angieOnlySignatories = new List<SignatoryContactEntity> { new() { FirstName = "Angie", LastName = "Thomson" } };

            return new()
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Number = "DCFST10001163-LC",
                    InvoiceNumber = "DCFST10000777-IP",
                    Signatories = angieOnlySignatories
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Number = "HUCAV10000674-LC",
                    InvoiceNumber = "HUCAV10000446-IP",
                    SignatoryNames = "Sam King"
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Number = "FCFST10000538-LC",
                    InvoiceNumber = "FCFST10000526-IP",
                    SignatoryNames = "Tony Furger,Brett Smith"
                },
            };
        }
    }
}
