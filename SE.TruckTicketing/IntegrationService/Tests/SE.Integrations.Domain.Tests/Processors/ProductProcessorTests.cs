using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;

using SE.Integrations.Api.Configuration;
using SE.Integrations.Domain.Processors.EntityProcessors;
using SE.Shared.Domain.LegalEntity;
using SE.Shared.Domain.Product;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Contracts;
using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Mapper;

namespace SE.Integrations.Domain.Tests.Processors;

[TestClass]
public class ProductProcessorTests
{
    private readonly Mock<IProvider<Guid, LegalEntityEntity>> _legalEntityprovider = new();

    private Mock<ILog> _log = null!;

    private Mock<IManager<Guid, ProductEntity>> _manager = null!;

    private ProductMessageProcessor _processor = null!;

    private ServiceMapperRegistry _serviceMapperRegistry = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _serviceMapperRegistry = new(new[] { new ApiMapperProfile() });
        _manager = new();
        _log = new();
        _processor = new(_manager.Object!, _legalEntityprovider.Object!, _log.Object!);
    }

    [TestMethod("ProductProcessor should be able to process a normal message.")]
    public async Task ProductProcessor_Process_NormalMessage()
    {
        // arrange
        ProductEntity resultingEntity = null!;
        var id = Guid.Parse("ba248238-f375-4e78-b57e-b327227ea24d");
        var legalEntityId = Guid.Parse("104ed370-66da-4c20-b149-823d0d4e7738");
        var legalEntityCode = "LegalEntityCode";
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
                new()
                {
                    SubstanceName = "SubstanceName2",
                    WasteCode = "WasteCode2",
                },
            },
        };

        var existingLegalEntity = new List<LegalEntityEntity>
        {
            new()
            {
                Id = legalEntityId,
                CountryCode = CountryCode.CA,
                Code = legalEntityCode,
                Name = "Name",
                BusinessStreamId = Guid.NewGuid(),
                CreditExpirationThreshold = 120,
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
            LegalEntityCode = legalEntityCode,
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
            IsActive = true
        };

        var message = typeof(ProductProcessorTests).Assembly.GetResourceAsString("EntityFunctions-Product-Sample.json", "Resources")!;
        _manager.Setup(m => m.Save(It.IsAny<ProductEntity>(), It.IsAny<bool>()))!.Callback((ProductEntity c, bool d) => resultingEntity = c);
        _manager.Setup(m => m.GetById(It.Is<Guid>(g => g == id), It.IsAny<bool>()))!.Returns(Task.FromResult(existingEntity));
        _legalEntityprovider.Setup(m => m.Get(It.IsAny<Expression<Func<LegalEntityEntity, bool>>>(),
                                              It.IsAny<Func<IQueryable<LegalEntityEntity>, IOrderedQueryable<LegalEntityEntity>>>(),
                                              It.IsAny<IEnumerable<string>>(),
                                              It.IsAny<bool>(),
                                              It.IsAny<bool>(),
                                              It.IsAny<bool>()))
                            .ReturnsAsync(await Task.FromResult(existingLegalEntity));

        // act
        await _processor.Process(message);

        // assert
        resultingEntity.Number.Should().BeEquivalentTo(expectedEntity.Number);
        resultingEntity.Name.Should().BeEquivalentTo(expectedEntity.Name);
        resultingEntity.LegalEntityId.Should().Be(expectedEntity.LegalEntityId);
        resultingEntity.LegalEntityCode.Should().Be(expectedEntity.LegalEntityCode);
        resultingEntity.DisposalUnit.Should().Be(expectedEntity.DisposalUnit);
        resultingEntity.UnitOfMeasure.Should().BeEquivalentTo(expectedEntity.UnitOfMeasure);
        resultingEntity.LastIntegrationTimestamp.Should().Be(expectedEntity.LastIntegrationTimestamp);
        resultingEntity.Substances.Should().HaveCount(2);
        resultingEntity.Substances.FirstOrDefault().Should().BeEquivalentTo(expectedEntity.Substances.FirstOrDefault());
        resultingEntity.Substances.FirstOrDefault()!.SubstanceName.Should().BeEquivalentTo(expectedEntity.Substances.FirstOrDefault()!.SubstanceName);
        resultingEntity.IsActive.Should().Be(expectedEntity.IsActive);
    }

    [TestMethod("ProductProcessor should be able to process a normal message for a new entity.")]
    public async Task ProductProcessor_Process_NormalMessageNew()
    {
        // arrange
        ProductEntity resultingEntity = null!;
        var id = Guid.Parse("ba248238-f375-4e78-b57e-b327227ea24d");
        var legalEntityId = Guid.Parse("104ed370-66da-4c20-b149-823d0d4e7738");
        var legalEntityCode = "LegalEntityCode";
        var expectedEntity = new ProductEntity
        {
            LastIntegrationTimestamp = new DateTime(2022, 07, 19, 14, 41, 11),
            Number = "123456",
            Name = "Product Name",
            UnitOfMeasure = "DisposalUnit",
            DisposalUnit = "DisposalUnit",
            LegalEntityId = legalEntityId,
            LegalEntityCode = legalEntityCode,
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
            IsActive = true,
        };

        var existingLegalEntity = new List<LegalEntityEntity>
        {
            new()
            {
                Id = legalEntityId,
                CountryCode = CountryCode.CA,
                Code = legalEntityCode,
                Name = "Name",
                BusinessStreamId = Guid.NewGuid(),
                CreditExpirationThreshold = 120,
            },
        };

        var message = typeof(ProductProcessorTests).Assembly.GetResourceAsString("EntityFunctions-Product-Sample.json", "Resources")!;
        _manager.Setup(m => m.Save(It.IsAny<ProductEntity>(), It.IsAny<bool>()))!.Callback((ProductEntity c, bool d) => resultingEntity = c);
        _manager.Setup(m => m.GetById(It.Is<Guid>(g => g == id), It.IsAny<bool>()))!.Returns(Task.FromResult((ProductEntity)null));
        _legalEntityprovider.Setup(m => m.Get(It.IsAny<Expression<Func<LegalEntityEntity, bool>>>(),
                                              It.IsAny<Func<IQueryable<LegalEntityEntity>, IOrderedQueryable<LegalEntityEntity>>>(),
                                              It.IsAny<IEnumerable<string>>(),
                                              It.IsAny<bool>(),
                                              It.IsAny<bool>(),
                                              It.IsAny<bool>()))
                            .ReturnsAsync(await Task.FromResult(existingLegalEntity));

        // act
        await _processor.Process(message);

        // assert
        resultingEntity.Number.Should().BeEquivalentTo(expectedEntity.Number);
        resultingEntity.Name.Should().BeEquivalentTo(expectedEntity.Name);
        resultingEntity.LegalEntityId.Should().Be(expectedEntity.LegalEntityId);
        resultingEntity.LegalEntityCode.Should().Be(expectedEntity.LegalEntityCode);
        resultingEntity.UnitOfMeasure.Should().BeEquivalentTo(expectedEntity.UnitOfMeasure);
        resultingEntity.Substances.Should().HaveCount(2);
        resultingEntity.Substances.FirstOrDefault().Should().BeEquivalentTo(expectedEntity.Substances.FirstOrDefault());
        resultingEntity.Substances.FirstOrDefault()!.SubstanceName.Should().BeEquivalentTo(expectedEntity.Substances.FirstOrDefault()!.SubstanceName);
        resultingEntity.IsActive.Should().Be(expectedEntity.IsActive);
    }

    [TestMethod("ProductProcessor should be able to skip outdated messages.")]
    public async Task ProductProcessor_Process_SkipOutdated()
    {
        // arrange
        var id = Guid.Parse("ba248238-f375-4e78-b57e-b327227ea24d");
        var legalEntityId = Guid.Parse("104ed370-66da-4c20-b149-823d0d4e7738");
        var existingEntity = new ProductEntity
        {
            LastIntegrationTimestamp = new DateTime(2022, 07, 22, 14, 41, 11),
            Number = "123456",
            Name = "Product Name",
            UnitOfMeasure = "UOM",
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

        var message = typeof(ProductProcessorTests).Assembly.GetResourceAsString("EntityFunctions-Product-Sample.json", "Resources")!;
        _manager.Setup(m => m.GetById(It.Is<Guid>(g => g == id), It.IsAny<bool>()))!.Returns(Task.FromResult(existingEntity));

        // act
        await _processor.Process(message);

        // assert
        _log.Verify(l => l.Warning(It.IsAny<Type>()!, It.IsAny<Exception>()!, It.Is<string>(m => m.Contains("Message is outdated"))!, It.IsAny<object[]>()!));
    }
}
