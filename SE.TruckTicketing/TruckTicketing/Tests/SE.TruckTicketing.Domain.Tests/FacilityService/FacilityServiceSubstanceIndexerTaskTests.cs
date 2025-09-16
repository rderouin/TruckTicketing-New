using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.ServiceType;
using SE.Shared.Domain.Product;
using SE.Shared.Domain.Tests.TestUtilities;
using SE.TruckTicketing.Domain.Entities.FacilityService;
using SE.TruckTicketing.Domain.Entities.FacilityService.Tasks;

using Trident.Business;
using Trident.Contracts;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Testing.TestScopes;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Tests.FacilityService;

[TestClass]
public class FacilityServiceSubstanceIndexerTaskTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldRun_WhenOperationIsInsert()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<FacilityServiceEntity>();
        var context = new BusinessContext<FacilityServiceEntity>(entity) { Operation = Operation.Insert };

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.PostValidation);
        shouldRun.Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldRun_WhenOperationIsUpdateAndIsActiveFlagHasChanged()
    {
        // arrange
        var scope = new DefaultScope();
        var original = GenFu.GenFu.New<FacilityServiceEntity>();
        var entity = original.Clone();
        entity.IsActive = !original.IsActive;
        var context = new BusinessContext<FacilityServiceEntity>(entity, original) { Operation = Operation.Update };

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.PostValidation);
        shouldRun.Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldRun_WhenOperationIsUpdateAndAuthorizedSubstancesHaveChanged()
    {
        // arrange
        var scope = new DefaultScope();
        var original = GenFu.GenFu.New<FacilityServiceEntity>();
        var entity = original.Clone();
        entity.AuthorizedSubstances = new() { List = new() { Guid.NewGuid() } };
        var context = new BusinessContext<FacilityServiceEntity>(entity, original) { Operation = Operation.Update };

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.PostValidation);
        shouldRun.Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldNotRun_WhenOperationIsUpdateAndIsActiveFlagOrAuthorizedSubstancesHaveNotChanged()
    {
        // arrange
        var scope = new DefaultScope();
        var original = GenFu.GenFu.New<FacilityServiceEntity>();
        var entity = original.Clone();
        var context = new BusinessContext<FacilityServiceEntity>(entity, original) { Operation = Operation.Update };

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.PostValidation);
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldFail_When_TotalProductIdCannotBeFound()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<FacilityServiceEntity>();
        var context = new BusinessContext<FacilityServiceEntity>(entity);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldCreateNewIndices_WhenFacilityServiceIsNewAndTotalProductContainsSubstances()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<FacilityServiceEntity>();
        var context = new BusinessContext<FacilityServiceEntity>(entity);

        entity.TotalItemProductId = scope.Product.Id;
        entity.ServiceTypeId = scope.ServiceType.Id;
        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        var persistedSubstanceIds = scope.PersistedIndices.Select(index => index.SubstanceId);
        var actualSubstanceIds = scope.Product.Substances.Select(substance => substance.Id);
        persistedSubstanceIds.Should().BeEquivalentTo(actualSubstanceIds);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldMergeIndexes_WhenFacilityServiceHasExistingIndexes()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<FacilityServiceEntity>();
        var context = new BusinessContext<FacilityServiceEntity>(entity, entity);

        var existingIndexes = GenFu.GenFu.ListOf<FacilityServiceSubstanceIndexEntity>();
        var existingIndexForFacilityService = existingIndexes[0];
        existingIndexes[0].FacilityServiceId = entity.Id;

        scope.IndexManagerMock.SetupEntities(existingIndexes);
        entity.TotalItemProductId = scope.Product.Id;
        entity.ServiceTypeId = scope.ServiceType.Id;
        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        var persistedSubstanceIds = scope.PersistedIndices.Select(index => index.SubstanceId);
        var actualSubstanceIds = scope.Product.Substances.Select(substance => substance.Id).Concat(new[] { existingIndexForFacilityService.SubstanceId });
        persistedSubstanceIds.Should().BeEquivalentTo(actualSubstanceIds);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldNotDuplicatedIndex_WhenMergingNewWithExistingIndexes()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<FacilityServiceEntity>();
        var context = new BusinessContext<FacilityServiceEntity>(entity, entity);

        var existingSubstanceId = scope.Product.Substances[0].Id;
        var existingIndexes = GenFu.GenFu.ListOf<FacilityServiceSubstanceIndexEntity>();
        var existingIndexForFacilityService = existingIndexes[0];
        existingIndexes[0].FacilityServiceId = entity.Id;
        existingIndexes[0].SubstanceId = existingSubstanceId;

        scope.IndexManagerMock.SetupEntities(existingIndexes);
        entity.TotalItemProductId = scope.Product.Id;
        entity.ServiceTypeId = scope.ServiceType.Id;

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        var persistedSubstanceIds = scope.PersistedIndices.Select(index => index.SubstanceId);
        var actualSubstanceIds = scope.Product.Substances.Select(substance => substance.Id);
        persistedSubstanceIds.Should().BeEquivalentTo(actualSubstanceIds);
        scope.PersistedIndices.Should().Contain(index => index.Id == existingIndexForFacilityService.Id);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldAuthorizeIndex_WhenFacilityServiceIsActiveAndSubstanceIsAuthorized()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<FacilityServiceEntity>();
        var context = new BusinessContext<FacilityServiceEntity>(entity);

        entity.IsActive = true;
        var authorizedSubstanceId = scope.Product.Substances[0].Id;
        entity.AuthorizedSubstances = new() { List = new() { authorizedSubstanceId } };

        entity.TotalItemProductId = scope.Product.Id;
        entity.ServiceTypeId = scope.ServiceType.Id;
        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        var persistedSubstanceIds = scope.PersistedIndices.Select(index => index.SubstanceId);
        var actualSubstanceIds = scope.Product.Substances.Select(substance => substance.Id);
        persistedSubstanceIds.Should().BeEquivalentTo(actualSubstanceIds);
        scope.PersistedIndices.Should().ContainSingle(index => index.SubstanceId == authorizedSubstanceId);
    }

    private class DefaultScope : TestScope<FacilityServiceSubstanceIndexer>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(IndexManagerMock.Object,
                                    ProductProviderMock.Object,
                                    ServiceTypeProviderMock.Object);

            IndexManagerMock.Setup(manager => manager.BulkSave(It.IsAny<IEnumerable<FacilityServiceSubstanceIndexEntity>>()))
                            .Callback((IEnumerable<FacilityServiceSubstanceIndexEntity> entities) => PersistedIndices = entities);

            Product = GenFu.GenFu.New<ProductEntity>();
            Product.Substances = GenFu.GenFu.ListOf<ProductSubstanceEntity>();

            ProductProviderMock.SetupEntities(new[] { Product });

            ServiceType = GenFu.GenFu.New<ServiceTypeEntity>();
            ServiceTypeProviderMock.SetupEntities(new[] { ServiceType });
        }

        public IEnumerable<FacilityServiceSubstanceIndexEntity> PersistedIndices { get; set; }

        public ProductEntity Product { get; }

        public ServiceTypeEntity ServiceType { get; }

        public Mock<IManager<Guid, FacilityServiceSubstanceIndexEntity>> IndexManagerMock { get; } = new();

        public Mock<IProvider<Guid, ProductEntity>> ProductProviderMock { get; } = new();

        public Mock<IProvider<Guid, ServiceTypeEntity>> ServiceTypeProviderMock { get; } = new();
    }
}
