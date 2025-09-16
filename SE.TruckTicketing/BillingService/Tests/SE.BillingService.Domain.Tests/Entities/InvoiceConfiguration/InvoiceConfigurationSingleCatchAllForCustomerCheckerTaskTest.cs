using System;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.InvoiceConfiguration;
using SE.Shared.Domain.Entities.InvoiceConfiguration.Tasks;
using SE.Shared.Domain.Tests.TestUtilities;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Testing.TestScopes;
using Trident.Workflow;

namespace SE.BillingService.Domain.Tests.Entities.InvoiceConfiguration;

[TestClass]
public class InvoiceConfigurationSingleCatchAllForCustomerCheckerTaskTest : TestScope<InvoiceConfigurationSingleCatchAllForCustomerCheckerTask>
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldRun_OnlyWhenInvoiceConfigurationIsCatchAllConfiguration_AndCustomerIsSelected()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = scope.DefaultInvoiceConfiguration.Clone();
        entity.CatchAll = true;
        var context = scope.CreateInvoiceConfigurationContext(entity);

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);
        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.BeforeUpdate | OperationStage.BeforeInsert);
        shouldRun.Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldNotRun_IfInvoiceConfigurationIsNotCatchAll()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = scope.DefaultInvoiceConfiguration.Clone();
        entity.CatchAll = false;
        var context = scope.CreateInvoiceConfigurationContext(entity);
        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);
        // assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldNotRun_IfInvoiceConfigurationCustomerIsNotSelected()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = scope.DefaultInvoiceConfiguration.Clone();
        entity.CatchAll = true;
        entity.CustomerId = default;
        var context = scope.CreateInvoiceConfigurationContext(entity);
        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);
        // assert
        shouldRun.Should().BeFalse();
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_ExistingInvoiceConfiguration_CatchAllForSameCustomer()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = scope.DefaultInvoiceConfiguration.Clone();
        entity.CatchAll = true;
        entity.Id = Guid.NewGuid();
        var context = scope.CreateInvoiceConfigurationContext(entity);
        var operationStage = scope.InstanceUnderTest.Stage;

        //Existing CatchAll Invoice Configuration for same Customer
        var catchAllInvoiceConfigurationForSameCustomer = context.Target.Clone();
        catchAllInvoiceConfigurationForSameCustomer.CatchAll = true;
        catchAllInvoiceConfigurationForSameCustomer.Id = Guid.NewGuid();

        InvoiceConfigurationEntity[] entities = { catchAllInvoiceConfigurationForSameCustomer };
        scope.SetupExistingInvoiceConfiguration(entities);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsTrue(context.GetContextBagItemOrDefault(InvoiceConfigurationSingleCatchAllForCustomerCheckerTask.ResultKey, false));
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_ExistingInvoiceConfiguration_CatchAllForDifferentCustomer()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = scope.DefaultInvoiceConfiguration.Clone();
        entity.CatchAll = true;
        entity.Id = Guid.NewGuid();
        var context = scope.CreateInvoiceConfigurationContext(entity);
        var operationStage = scope.InstanceUnderTest.Stage;

        //Existing Default Billing Configuration for different customer
        var catchAllInvoiceConfigurationForDifferentCustomer = context.Target.Clone();
        catchAllInvoiceConfigurationForDifferentCustomer.CatchAll = true;
        catchAllInvoiceConfigurationForDifferentCustomer.CustomerId = Guid.NewGuid();
        catchAllInvoiceConfigurationForDifferentCustomer.Id = Guid.NewGuid();

        InvoiceConfigurationEntity[] entities = { catchAllInvoiceConfigurationForDifferentCustomer };
        scope.SetupExistingInvoiceConfiguration(entities);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsFalse(context.GetContextBagItemOrDefault(InvoiceConfigurationSingleCatchAllForCustomerCheckerTask.ResultKey, false));
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    private class DefaultScope : TestScope<InvoiceConfigurationSingleCatchAllForCustomerCheckerTask>
    {
        public readonly InvoiceConfigurationEntity DefaultInvoiceConfiguration =
            new()
            {
                Id = Guid.NewGuid(),
                BusinessUnitId = "AA-100050",
                AllFacilities = false,
                AllServiceTypes = false,
                AllSourceLocations = false,
                AllSubstances = false,
                AllWellClassifications = false,
                CatchAll = false,
                CustomerId = Guid.NewGuid(),
                CustomerName = "QQ Generator/Customer 01",
                Description = "This is test invoice configuration",
                IncludeInternalDocumentAttachment = true,
                IncludeExternalDocumentAttachment = true,
                IsSplitByFacility = false,
                IsSplitByServiceType = false,
                IsSplitBySourceLocation = false,
                IsSplitBySubstance = false,
                IsSplitByWellClassification = false,
                Name = "TT Petro Canada",
                Facilities = new()
                {
                    Key = Guid.NewGuid(),
                    List = new()
                    {
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                    },
                },
                ServiceTypes = new()
                {
                    Key = Guid.NewGuid(),
                    List = new()
                    {
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                    },
                },
                SourceLocationIdentifier = new()
                {
                    Key = Guid.NewGuid(),
                    List = new()
                    {
                        "Id-001",
                        "Id-002",
                    },
                },
                SourceLocations = new()
                {
                    Key = Guid.NewGuid(),
                    List = new()
                    {
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                    },
                },
                SplitEdiFieldDefinitions = null,
                SplittingCategories = null,
                WellClassifications = new()
                {
                    Key = Guid.NewGuid(),
                    List = new() { "Drilling" },
                },
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
                SubstancesName = new()
                {
                    Key = Guid.NewGuid(),
                    List = new()
                    {
                        "Substance01",
                        "Substance02",
                    },
                },
                ServiceTypesName = new()
                {
                    Key = Guid.NewGuid(),
                    List = new()
                    {
                        "Service01",
                        "Service02",
                    },
                },
                Substances = new()
                {
                    Key = Guid.NewGuid(),
                    List = new()
                    {
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                    },
                },
                CreatedAt = new DateTime(2025, 1, 17, 18, 11, 30),
                UpdatedAt = new DateTime(2025, 1, 17, 18, 11, 30),
                CreatedBy = "Panth Shah",
                UpdatedBy = "Panth Shah",
                CreatedById = Guid.NewGuid().ToString(),
                UpdatedById = Guid.NewGuid().ToString(),
            };

        public readonly Mock<IProvider<Guid, InvoiceConfigurationEntity>> InvoiceConfigurationProviderMock = new();

        public DefaultScope()
        {
            InstanceUnderTest = new(InvoiceConfigurationProviderMock.Object);
        }

        public void SetupExistingInvoiceConfiguration(params InvoiceConfigurationEntity[] entities)
        {
            InvoiceConfigurationProviderMock.SetupEntities(entities);
        }

        public BusinessContext<InvoiceConfigurationEntity> CreateInvoiceConfigurationContext(InvoiceConfigurationEntity target)
        {
            return new(target);
        }
    }
}
