using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Product;
using SE.Shared.Domain.Tests.TestUtilities;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Domain.Entities.FacilityService;
using SE.TruckTicketing.Domain.Entities.Product.Tasks;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Testing.TestScopes;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Tests.Product;

[TestClass]
public class ProductFacilityServiceSubstanceIndexUpdateTaskTests
{
    [TestMethod]
    public void Task_RunParameters_AreValid()
    {
        // arrange / act
        var scope = new DefaultScope();

        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.PostValidation);
    }

    [TestMethod]
    [DataRow(Operation.Insert)]
    [DataRow(Operation.Delete)]
    [DataRow(Operation.Custom)]
    [DataRow(Operation.Undefined)]
    public async Task Task_ShouldNotRun_IfNotUpdate(Operation operation)
    {
        // arrange
        var scope = new DefaultScope();
        var original = GenFu.GenFu.New<ProductEntity>();
        var target = original.Clone();
        target.Name = original.Name + "-Updated";
        var context = new BusinessContext<ProductEntity>(target, original) { Operation = operation };

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldRun_ProductNameUpdated()
    {
        // arrange
        var scope = new DefaultScope();
        var original = GenFu.GenFu.New<ProductEntity>();
        var target = original.Clone();
        target.Name = original.Name + "-Updated";
        var context = new BusinessContext<ProductEntity>(target, original) { Operation = Operation.Update };

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldRun_ProductUnitOfMeasureUpdated()
    {
        // arrange
        var scope = new DefaultScope();
        var original = GenFu.GenFu.New<ProductEntity>();
        var target = original.Clone();
        target.UnitOfMeasure = original.UnitOfMeasure + "-Updated";
        var context = new BusinessContext<ProductEntity>(target, original) { Operation = Operation.Update };

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldRun_ProductCategoriesUpdated()
    {
        // arrange
        var scope = new DefaultScope();
        var original = GenFu.GenFu.New<ProductEntity>();
        original.Categories = new()
        {
            Key = Guid.NewGuid(),
            List = new()
            {
                ProductCategories.AdditionalServices.AltUnitOfMeasureClass1,
                ProductCategories.AdditionalServices.Lf,
            },
        };

        var target = original.Clone();
        target.Categories = new()
        {
            Key = Guid.NewGuid(),
            List = new() { ProductCategories.AdditionalServices.Fst },
        };

        var context = new BusinessContext<ProductEntity>(target, original) { Operation = Operation.Update };

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldNotRun_NoUpdate_Name_UnitOfMeasure_Categories()
    {
        // arrange
        var scope = new DefaultScope();
        var original = GenFu.GenFu.New<ProductEntity>();
        original.Categories = new()
        {
            Key = Guid.NewGuid(),
            List = new() { ProductCategories.AdditionalServices.Fst },
        };

        var target = original.Clone();
        var context = new BusinessContext<ProductEntity>(target, original) { Operation = Operation.Update };

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_Run_Update_FacilityServiceSubstanceIndex_TotalProductCategories()
    {
        var scope = new DefaultScope();
        var originalEntity = GenFu.GenFu.New<ProductEntity>();
        originalEntity.Categories = new()
        {
            Key = Guid.NewGuid(),
            List = new() { ProductCategories.AdditionalServices.Fst },
        };

        var targetEntity = originalEntity.Clone();
        targetEntity.Categories = new()
        {
            Key = Guid.NewGuid(),
            List = new() { ProductCategories.AdditionalServices.Lf },
        };

        var context = new BusinessContext<ProductEntity>(targetEntity, originalEntity);

        scope.SetUpExistingIndexesWithExistingProduct(originalEntity);
        var existingIndex = scope.ExistingIndices[0].Clone();
        // act
        var result = await scope.InstanceUnderTest.Run(context);
        var updatedIndex = scope.ExistingIndices[0];
        // assert
        result.Should().BeTrue();
        updatedIndex.TotalProductCategories.List.Should().BeEquivalentTo(context.Target.Categories.List);
        existingIndex.TotalProductCategories.List.Should().NotBeEquivalentTo(updatedIndex.TotalProductCategories.List);
        scope.FacilityServiceSubstanceIndexProviderMock.Verify(p => p.Update(It.IsAny<FacilityServiceSubstanceIndexEntity>(), true), Times.AtLeastOnce);
    }

    [TestMethod]
    public async Task Task_Run_Update_FacilityServiceSubstanceIndex_TotalProductName()
    {
        var scope = new DefaultScope();
        var originalEntity = GenFu.GenFu.New<ProductEntity>();
        originalEntity.Categories = new()
        {
            Key = Guid.NewGuid(),
            List = new() { ProductCategories.AdditionalServices.Fst },
        };

        var targetEntity = originalEntity.Clone();
        targetEntity.Name += "-Updated";

        var context = new BusinessContext<ProductEntity>(targetEntity, originalEntity);

        scope.SetUpExistingIndexesWithExistingProduct(originalEntity);
        var existingIndex = scope.ExistingIndices[0].Clone();
        // act
        var result = await scope.InstanceUnderTest.Run(context);
        var updatedIndex = scope.ExistingIndices[0];
        // assert
        result.Should().BeTrue();
        updatedIndex.TotalProductName.Should().BeEquivalentTo(context.Target.Name);
        existingIndex.TotalProductName.Should().NotBeEquivalentTo(updatedIndex.TotalProductName);
        scope.FacilityServiceSubstanceIndexProviderMock.Verify(p => p.Update(It.IsAny<FacilityServiceSubstanceIndexEntity>(), true), Times.AtLeastOnce);
    }

    [TestMethod]
    public async Task Task_Run_Update_FacilityServiceSubstanceIndex_UnitOfMeasure()
    {
        var scope = new DefaultScope();
        var originalEntity = GenFu.GenFu.New<ProductEntity>();
        originalEntity.Categories = new()
        {
            Key = Guid.NewGuid(),
            List = new() { ProductCategories.AdditionalServices.Fst },
        };

        var targetEntity = originalEntity.Clone();
        targetEntity.UnitOfMeasure += "-Updated";

        var context = new BusinessContext<ProductEntity>(targetEntity, originalEntity);

        scope.SetUpExistingIndexesWithExistingProduct(originalEntity);
        var existingIndex = scope.ExistingIndices[0].Clone();
        // act
        var result = await scope.InstanceUnderTest.Run(context);
        var updatedIndex = scope.ExistingIndices[0];
        // assert
        result.Should().BeTrue();
        updatedIndex.UnitOfMeasure.Should().BeEquivalentTo(context.Target.UnitOfMeasure);
        existingIndex.UnitOfMeasure.Should().NotBeEquivalentTo(updatedIndex.UnitOfMeasure);
        scope.FacilityServiceSubstanceIndexProviderMock.Verify(p => p.Update(It.IsAny<FacilityServiceSubstanceIndexEntity>(), true), Times.AtLeastOnce);
    }

    [TestMethod]
    public async Task Task_Run_NoExistingFacilityServiceSubstanceIndex_NoUpdate()
    {
        var scope = new DefaultScope();
        var originalEntity = GenFu.GenFu.New<ProductEntity>();
        originalEntity.Categories = new()
        {
            Key = Guid.NewGuid(),
            List = new() { ProductCategories.AdditionalServices.Fst },
        };

        var targetEntity = originalEntity.Clone();
        targetEntity.Categories = new()
        {
            Key = Guid.NewGuid(),
            List = new() { ProductCategories.AdditionalServices.Lf },
        };

        var context = new BusinessContext<ProductEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.Run(context);
        // assert
        result.Should().BeTrue();
        scope.FacilityServiceSubstanceIndexProviderMock.Verify(p => p.Update(It.IsAny<FacilityServiceSubstanceIndexEntity>(), true), Times.Never);
    }

    private class DefaultScope : TestScope<ProductFacilityServiceSubstanceIndexUpdateTask>
    {
        public readonly Mock<IProvider<Guid, FacilityServiceSubstanceIndexEntity>> FacilityServiceSubstanceIndexProviderMock = new();

        public DefaultScope()
        {
            InstanceUnderTest = new(FacilityServiceSubstanceIndexProviderMock.Object);
        }

        public List<FacilityServiceSubstanceIndexEntity> ExistingIndices { get; set; } = new();

        public void SetUpExistingIndexesWithExistingProduct(ProductEntity product)
        {
            ExistingIndices = GenFu.GenFu.ListOf<FacilityServiceSubstanceIndexEntity>(5);

            ExistingIndices.ForEach(x =>
                                    {
                                        x.TotalProductId = product.Id;
                                        x.TotalProductCategories = product.Categories;
                                        x.TotalProductName = product.Name;
                                        x.UnitOfMeasure = product.UnitOfMeasure;
                                    });

            FacilityServiceSubstanceIndexProviderMock.SetupEntities(ExistingIndices);
        }
    }
}
