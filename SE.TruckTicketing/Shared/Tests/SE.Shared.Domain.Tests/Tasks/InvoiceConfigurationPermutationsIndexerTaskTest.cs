using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;

using SE.Shared.Domain.Entities.InvoiceConfiguration;
using SE.Shared.Domain.Entities.InvoiceConfiguration.Tasks;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Testing.TestScopes;

namespace SE.Shared.Domain.Tests.Tasks;

[TestClass]
public class InvoiceConfigurationPermutationsIndexerTaskTest
{
    [TestMethod]
    public async Task Task_ShouldNotRun_WhenTargetIsNull()
    {
        ////arrange
        var scope = new DefaultScope();
        var context = new BusinessContext<InvoiceConfigurationEntity>(null);

        //act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        //assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldNotRun_WhenNoPermutationPresent()
    {
        ////arrange
        var scope = new DefaultScope();
        var entity = scope.GenerateInvoiceConfiguration().First().Clone();
        entity.Permutations = null;
        var context = new BusinessContext<InvoiceConfigurationEntity>(entity);

        //act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        //assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldCreateNewIndex_WhenNewInvoiceConfigurationCreated()
    {
        //arrange
        var scope = new DefaultScope();
        var entity = scope.GenerateInvoiceConfiguration().First().Clone();
        var context = new BusinessContext<InvoiceConfigurationEntity>(entity);

        //act
        var result = await scope.InstanceUnderTest.Run(context);

        //assert
        result.Should().BeTrue();
        scope.IndexProviderMock.Verify(p => p.Insert(It.Is<InvoiceConfigurationPermutationsIndexEntity>(index => IsInvoiceConfigurationMatch(entity, index)), It.IsAny<bool>()));
    }

    [TestMethod]
    public async Task Task_ShouldCreateNewIndex_WhenExistingInvoiceConfigurationUpdated()
    {
        //arrange
        var scope = new DefaultScope();
        var entity = scope.GenerateInvoiceConfiguration().First().Clone();
        var entityIndex = scope.GenerateInvoiceConfigurationPermutationsIndexEntities().First().Clone();
        entityIndex.InvoiceConfigurationId = entity.Id;
        entityIndex.CustomerId = entity.CustomerId;

        scope.ConfigureCustomerInvoiceConfigurationProviderMock(entityIndex);
        entity.PermutationsHash = "0037093D2EA52B432228E072824E0CE0DE63986C44638429A85B29411AF43810";

        var context = new BusinessContext<InvoiceConfigurationEntity>(entity, scope.GenerateInvoiceConfiguration().First().Clone());

        //act
        var result = await scope.InstanceUnderTest.Run(context);

        //assert
        result.Should().BeTrue();
        scope.IndexProviderMock.Verify(p => p.Delete(It.IsAny<InvoiceConfigurationPermutationsIndexEntity>(), It.IsAny<bool>()));
        scope.IndexProviderMock.Verify(p => p.Insert(It.Is<InvoiceConfigurationPermutationsIndexEntity>(index => IsInvoiceConfigurationMatch(entity, index)), It.IsAny<bool>()));
    }

    private bool IsInvoiceConfigurationMatch(InvoiceConfigurationEntity entity, InvoiceConfigurationPermutationsIndexEntity index)
    {
        return entity.Id == index.InvoiceConfigurationId &&
               entity.CustomerId == index.CustomerId &&
               entity.Name == index.Name &&
               entity.Permutations.First().Facility == index.Facility &&
               entity.Permutations.First().SourceLocation == index.SourceLocation &&
               entity.Permutations.First().ServiceType == index.ServiceType &&
               entity.Permutations.First().WellClassification == index.WellClassification &&
               entity.Permutations.First().Substance == index.Substance;
    }

    public class DefaultScope : TestScope<InvoiceConfigurationPermutationsIndexerTask>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(IndexProviderMock.Object);
        }

        public Mock<IProvider<Guid, InvoiceConfigurationPermutationsIndexEntity>> IndexProviderMock { get; } = new();

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
                    WellClassifications = null,
                    PermutationsHash = "5537093D2EA52B432228E072824E0CE0DE63986C44638429A85B29411AF43810",
                    Permutations = new()
                    {
                        new()
                        {
                            Name = "TT Petro Canada",
                            SourceLocation = "Test SourceLocation A",
                            ServiceType = "Test ServiceType A",
                            WellClassification = "All",
                            Substance = "All",
                            Facility = "Facility A",
                        },
                    },
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
                    WellClassifications = new()
                    {
                        Key = Guid.NewGuid(),
                        List = new() { "Drilling" },
                    },
                },
            };
        }

        public List<InvoiceConfigurationPermutationsIndexEntity> GenerateInvoiceConfigurationPermutationsIndexEntities()
        {
            return new()
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    InvoiceConfigurationId = Guid.NewGuid(),
                    CustomerId = Guid.NewGuid(),
                    Name = "TT Petro Canada",
                    SourceLocation = "Test SourceLocation A",
                    ServiceType = "Test ServiceType A",
                    WellClassification = "All",
                    Substance = "All",
                    Facility = "Facility A",
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    InvoiceConfigurationId = Guid.NewGuid(),
                    CustomerId = Guid.NewGuid(),
                    Name = "Invoice Configuration B",
                    SourceLocation = "Test SourceLocation B",
                    ServiceType = "Test ServiceType B",
                    WellClassification = "All",
                    Substance = "All",
                    Facility = "Facility B",
                },
            };
        }

        public void ConfigureCustomerInvoiceConfigurationProviderMock(params InvoiceConfigurationPermutationsIndexEntity[] invoiceConfigurationPermutationsIndexEntities)
        {
            IndexProviderMock.Setup(x => x.Get(It.IsAny<Expression<Func<InvoiceConfigurationPermutationsIndexEntity, bool>>>(),
                                               It.IsAny<string>(),
                                               It.IsAny<Func<IQueryable<InvoiceConfigurationPermutationsIndexEntity>,
                                                   IOrderedQueryable<InvoiceConfigurationPermutationsIndexEntity>>>(),
                                               It.IsAny<IEnumerable<string>>(),
                                               It.IsAny<bool>(),
                                               It.IsAny<bool>(),
                                               It.IsAny<bool>()))
                             .ReturnsAsync(invoiceConfigurationPermutationsIndexEntities);
        }
    }
}
