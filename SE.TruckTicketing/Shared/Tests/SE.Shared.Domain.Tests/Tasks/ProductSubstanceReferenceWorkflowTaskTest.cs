using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

using SE.Shared.Domain.Entities.Substance;
using SE.Shared.Domain.Product;
using SE.Shared.Domain.Product.Tasks;
using SE.Shared.Domain.Tests.TestUtilities;

using Trident.Business;
using Trident.Contracts;
using Trident.Testing.TestScopes;

namespace SE.Shared.Domain.Tests.Tasks;

[TestClass]
public class ProductSubstanceReferenceWorkflowTaskTest
{
    [TestMethod]
    public async Task Task_ShouldRun_WhenTargetSubstanceRecords()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<ProductEntity>();
        var substances = GenFu.GenFu.ListOf<ProductSubstanceEntity>(5);
        entity.Substances = new(substances);
        var context = new BusinessContext<ProductEntity>(entity) { Operation = Operation.Insert };
        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeTrue();
    }

    [TestMethod("Product should be able to create new Substance Entities.")]
    public async Task ProductSubstanceReference_ProductSubstances_NewSubstances_Create()
    {
        // arrange
        var scope = new DefaultScope();

        var id = Guid.Parse("ba248238-f375-4e78-b57e-b327227ea24d");
        var legalEntityId = Guid.Parse("104ed370-66da-4c20-b149-823d0d4e7738");
        var substanceEntities = GenFu.GenFu.ListOf<SubstanceEntity>(4);

        var expectedEntity = new ProductEntity
        {
            LastIntegrationTimestamp = new DateTime(2022, 07, 20, 14, 41, 11),
            Number = "123456",
            Name = "Product Name",
            UnitOfMeasure = "DisposalUnit",
            DisposalUnit = "DisposalUnit",
            LegalEntityId = legalEntityId,
            Id = id,
            Substances = new()
            {
                new()
                {
                    SubstanceName = "SubstanceName1",
                    WasteCode = "WasteCode1",
                },
                new()
                {
                    SubstanceName = "SubstanceName2",
                    WasteCode = "WasteCode2",
                },
            },
        };

        var context = new BusinessContext<ProductEntity>(expectedEntity);

        scope.SubstanceManagerMock.SetupEntities(substanceEntities);

        // act
        await scope.InstanceUnderTest.Run(context);

        // assert
        var resultingEntity = context.Target;
        resultingEntity.Substances.FirstOrDefault()!.WasteCode.Should().BeEquivalentTo(expectedEntity.Substances.FirstOrDefault()!.WasteCode);
        resultingEntity.Substances.FirstOrDefault()!.SubstanceName.Should().BeEquivalentTo(expectedEntity.Substances.FirstOrDefault()!.SubstanceName);
        scope.SubstanceManagerMock.Verify(num => num.Insert(It.IsAny<SubstanceEntity>(), It.IsAny<bool>()));
    }

    [TestMethod("Product - no new Substance Entities created")]
    public async Task ProductSubstanceReference_ProductSubstances_Existing_Substances()
    {
        // arrange
        var scope = new DefaultScope();
        var id = Guid.Parse("ba248238-f375-4e78-b57e-b327227ea24d");
        var legalEntityId = Guid.Parse("104ed370-66da-4c20-b149-823d0d4e7738");
        var expectedEntity = new ProductEntity
        {
            LastIntegrationTimestamp = new DateTime(2022, 07, 20, 14, 41, 11),
            Number = "123456",
            Name = "Product Name",
            UnitOfMeasure = "DisposalUnit",
            DisposalUnit = "DisposalUnit",
            LegalEntityId = legalEntityId,
            Id = id,
            Substances = new()
            {
                new()
                {
                    SubstanceName = "SubstanceName1",
                    WasteCode = "WasteCode1",
                },
                new()
                {
                    SubstanceName = "SubstanceName2",
                    WasteCode = "WasteCode2",
                },
            },
        };

        var context = new BusinessContext<ProductEntity>(expectedEntity);

        var substanceEntities = expectedEntity.Substances.Select(s => new SubstanceEntity
        {
            Id = s.Id,
            SubstanceName = s.SubstanceName,
            WasteCode = s.WasteCode,
        }).ToList();

        scope.SubstanceManagerMock.SetupEntities(substanceEntities);

        // act
        await scope.InstanceUnderTest.Run(context);

        // assert
        var resultingEntity = context.Target;
        resultingEntity.Substances.Should().HaveCount(2);
        resultingEntity.Substances.FirstOrDefault()!.WasteCode.Should().BeEquivalentTo(expectedEntity.Substances.FirstOrDefault()!.WasteCode);
        resultingEntity.Substances.FirstOrDefault()!.SubstanceName.Should().BeEquivalentTo(expectedEntity.Substances.FirstOrDefault()!.SubstanceName);
        scope.SubstanceManagerMock.Verify(num => num.Insert(It.IsAny<SubstanceEntity>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod("ProductProcessor - new Substance entry for existing product")]
    public async Task ProductProcessor_ProductVariance_NewSubstance_ExistingProduct()
    {
        // arrange
        var scope = new DefaultScope();
        var id = Guid.Parse("ba248238-f375-4e78-b57e-b327227ea24d");
        var legalEntityId = Guid.Parse("104ed370-66da-4c20-b149-823d0d4e7738");
        var existingEntity = new ProductEntity
        {
            LastIntegrationTimestamp = new DateTime(2022, 07, 20, 14, 41, 11),
            Number = "123456",
            Name = "Product Name",
            UnitOfMeasure = "DisposalUnit",
            DisposalUnit = "DisposalUnit",
            LegalEntityId = legalEntityId,
            Id = id,
            Substances = new()
            {
                new()
                {
                    SubstanceName = "SubstanceName1",
                    WasteCode = "WasteCode1",
                },
            },
        };

        var expectedEntity = new ProductEntity
        {
            LastIntegrationTimestamp = new DateTime(2022, 07, 20, 14, 41, 11),
            Number = "123456",
            Name = "Product Name",
            UnitOfMeasure = "DisposalUnit",
            DisposalUnit = "DisposalUnit",
            LegalEntityId = legalEntityId,
            Id = id,
            Substances = new()
            {
                new()
                {
                    SubstanceName = "SubstanceName1",
                    WasteCode = "WasteCode1",
                },
                new()
                {
                    SubstanceName = "SubstanceName2",
                    WasteCode = "WasteCode2",
                },
            },
        };

        var context = new BusinessContext<ProductEntity>(expectedEntity);

        var substanceEntities = existingEntity.Substances.Select(s => new SubstanceEntity
        {
            Id = s.Id,
            SubstanceName = s.SubstanceName,
            WasteCode = s.WasteCode,
        }).ToList();

        scope.SubstanceManagerMock.SetupEntities(substanceEntities);

        // act
        await scope.InstanceUnderTest.Run(context);

        // assert
        var resultingEntity = context.Target;
        resultingEntity.Substances.Should().HaveCount(2);
        resultingEntity.Substances.FirstOrDefault()!.WasteCode.Should().BeEquivalentTo(expectedEntity.Substances.FirstOrDefault()!.WasteCode);
        resultingEntity.Substances.FirstOrDefault()!.SubstanceName.Should().BeEquivalentTo(expectedEntity.Substances.FirstOrDefault()!.SubstanceName);
        scope.SubstanceManagerMock.Verify(num => num.Insert(It.IsAny<SubstanceEntity>(), It.IsAny<bool>()), Times.Once);
    }

    private class DefaultScope : TestScope<ProductSubstanceReferenceWorkflowTask>
    {
        public readonly List<SubstanceEntity> SubstanceEntities = new();

        public DefaultScope()
        {
            InstanceUnderTest = new(SubstanceManagerMock.Object,
                                    LoggerMock.Object);

            SubstanceManagerMock.Setup(manager => manager.Save(It.IsAny<SubstanceEntity>(), false))
                                .Callback((SubstanceEntity entity, bool d) => SubstanceEntities.Add(entity));
        }

        public Mock<IManager<Guid, SubstanceEntity>> SubstanceManagerMock { get; } = new();

        public Mock<ILogger<ProductSubstanceReferenceWorkflowTask>> LoggerMock { get; } = new();
    }
}
