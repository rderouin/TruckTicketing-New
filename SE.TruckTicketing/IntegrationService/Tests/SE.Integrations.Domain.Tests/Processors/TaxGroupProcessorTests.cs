using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;

using SE.Integrations.Api.Configuration;
using SE.Integrations.Domain.Processors.EntityProcessors;
using SE.Shared.Domain.Entities.TaxGroups;
using SE.Shared.Domain.LegalEntity;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Contracts;
using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Mapper;

namespace SE.Integrations.Domain.Tests.Processors;

[TestClass]
public class TaxGroupProcessorTests
{
    private readonly Mock<IProvider<Guid, LegalEntityEntity>> _legalEntityprovider = new();

    private Mock<ILog> _log = null!;

    private Mock<IManager<Guid, TaxGroupEntity>> _manager = null!;

    private TaxGroupMessageProcessor _processor = null!;

    private ServiceMapperRegistry _serviceMapperRegistry = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _serviceMapperRegistry = new(new[] { new ApiMapperProfile() });
        _manager = new();
        _log = new();

        _processor = new(_manager.Object!, _legalEntityprovider.Object!, _log.Object!);
    }

    [TestMethod("TaxGroupProcessor should be able to process a normal message.")]
    public async Task TaxGroupProcessor_Process_NormalMessage()
    {
        // arrange
        TaxGroupEntity resultingEntity = null!;
        var id = Guid.Parse("ba248238-f375-4e78-b57e-b327227ea24d");
        var legalEntityId = Guid.Parse("104ed370-66da-4c20-b149-823d0d4e7738");
        var existingEntity = new TaxGroupEntity
        {
            LastIntegrationDateTime = new DateTime(2022, 07, 19, 14, 41, 11),
            Group = "BC_ALL",
            Name = "BC ALL",
            LegalEntityName = "old-email!",
            LegalEntityId = legalEntityId,
            Id = id,
            TaxCodes = new()
            {
                new()
                {
                    Code = "GST",
                    CurrencyCode = "CAD",
                    ExemptTax = false,
                    TaxName = "GST",
                    TaxValuePercentage = 5,
                    UseTax = false,
                },
            },
        };

        var existingLegalEntity = new List<LegalEntityEntity>
        {
            new()
            {
                Id = legalEntityId,
                CountryCode = CountryCode.CA,
                Code = "Code",
                Name = "Name",
                BusinessStreamId = Guid.NewGuid(),
                CreditExpirationThreshold = 120,
            },
        };

        var expectedEntity = new TaxGroupEntity
        {
            LastIntegrationDateTime = new DateTime(2022, 07, 20, 14, 41, 11),
            Group = "BC_ALL",
            Name = "BC ALL",
            LegalEntityName = "old-email!",
            LegalEntityId = legalEntityId,
            Id = id,
            TaxCodes = new()
            {
                new()
                {
                    Code = "GST",
                    CurrencyCode = "CAD",
                    ExemptTax = false,
                    TaxName = "GST",
                    TaxValuePercentage = 5,
                    UseTax = false,
                },
            },
        };

        var message = typeof(TaxGroupProcessorTests).Assembly.GetResourceAsString("EntityFunctions-TaxGroup-Sample.json", "Resources")!;
        _manager.Setup(m => m.Save(It.IsAny<TaxGroupEntity>(), It.IsAny<bool>()))!.Callback((TaxGroupEntity c, bool d) => resultingEntity = c);
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
        resultingEntity.Group.Should().BeEquivalentTo(expectedEntity.Group);
        resultingEntity.Name.Should().BeEquivalentTo(expectedEntity.Name);
        resultingEntity.LegalEntityId.Should().Be(expectedEntity.LegalEntityId);
        resultingEntity.TaxCodes.Should().HaveCount(1);
        resultingEntity.TaxCodes.FirstOrDefault().Should().BeEquivalentTo(expectedEntity.TaxCodes.FirstOrDefault());
        resultingEntity.TaxCodes.FirstOrDefault()!.Code.Should().BeEquivalentTo(expectedEntity.TaxCodes.FirstOrDefault()!.Code);
    }

    [TestMethod("TaxGroupProcessor should be able to skip outdated messages.")]
    public async Task TaxGroupProcessor_Process_SkipOutdated()
    {
        // arrange
        var id = Guid.Parse("ba248238-f375-4e78-b57e-b327227ea24d");
        var legalEntityId = Guid.Parse("104ed370-66da-4c20-b149-823d0d4e7738");
        var existingEntity = new TaxGroupEntity
        {
            LastIntegrationDateTime = new DateTime(2022, 07, 19, 14, 41, 11),
            Group = "BC_ALL",
            Name = "BC ALL",
            LegalEntityName = "old-email!",
            LegalEntityId = legalEntityId,
            Id = id,
            TaxCodes = new()
            {
                new()
                {
                    Code = "GST",
                    CurrencyCode = "CAD",
                    ExemptTax = false,
                    TaxName = "GST",
                    TaxValuePercentage = 5,
                    UseTax = false,
                },
            },
        };

        var existingLegalEntity = new List<LegalEntityEntity>
        {
            new()
            {
                Id = legalEntityId,
                CountryCode = CountryCode.CA,
                Code = "Code",
                Name = "Name",
                BusinessStreamId = Guid.NewGuid(),
                CreditExpirationThreshold = 120,
            },
        };

        var message = typeof(CustomerProcessorTests).Assembly.GetResourceAsString("EntityFunctions-TaxGroup-Sample.json", "Resources")!;
        _manager.Setup(m => m.GetById(It.IsAny<Guid>(), It.IsAny<bool>()))!.Returns(Task.FromResult(existingEntity));
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
        _log.Verify(l => l.Warning(It.IsAny<Type>()!, It.IsAny<Exception>()!, It.Is<string>(m => m.Contains("Message is outdated"))!, It.IsAny<object[]>()!));
    }
}
