using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;

using SE.Integrations.Api.Configuration;
using SE.Integrations.Domain.Processors.EntityProcessors;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.LegalEntity;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Contracts;
using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Mapper;

namespace SE.Integrations.Domain.Tests.Processors;

[TestClass]
public class CustomerProcessorTests
{
    private readonly Mock<IProvider<Guid, LegalEntityEntity>> _legalEntityprovider = new();

    private Mock<ILog> _log = null!;

    private Mock<IManager<Guid, AccountEntity>> _manager = null!;

    private CustomerProcessor _processor = null!;

    private ServiceMapperRegistry _serviceMapperRegistry = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _serviceMapperRegistry = new(new[] { new ApiMapperProfile() });
        _manager = new();
        _log = new();

        _processor = new(_serviceMapperRegistry, _manager.Object!, _log.Object!, _legalEntityprovider.Object!);
    }

    [TestMethod("CustomerProcessor should be able to process a normal message.")]
    public async Task CustomerProcessor_Process_NormalMessage()
    {
        // arrange
        AccountEntity resultingEntity = null!;
        var id = Guid.Parse("ba248238-f375-4e78-b57e-b327227ea24d");
        var legalEntityId = Guid.Parse("104ed370-66da-4c20-b149-823d0d4e7738");
        var existingEntity = new AccountEntity
        {
            LastIntegrationDateTime = new DateTime(2022, 07, 19, 14, 41, 11),
            AccountNumber = "000001",
            Name = "Alberta Oilsands Inc!",
            Email = "old-email!",
            IsBlocked = false,
            LegalEntityId = legalEntityId,
            Id = id,
            Attachments = new()
            {
                new()
                {
                    ContainerName = "attachments",
                    FileName = "test1.pdf",
                    Blob = "be855f88-4dd9-4f92-86ad-f0afdd6bf3a1/",
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

        var expectedEntity = new AccountEntity
        {
            LastIntegrationDateTime = new DateTime(2022, 07, 20, 14, 41, 11),
            AccountNumber = "000001",
            Name = "Alberta Oilsands Inc",
            Email = "",
            LegalEntityId = legalEntityId,
            Id = id,
            Attachments = new()
            {
                new()
                {
                    ContainerName = "attachments",
                    FileName = "test1.pdf",
                    Blob = "be855f88-4dd9-4f92-86ad-f0afdd6bf3a1/",
                },
            },
        };

        var message = typeof(CustomerProcessorTests).Assembly.GetResourceAsString("EntityFunctions-Customer-Sample.json", "Resources")!;
        _manager.Setup(m => m.Save(It.IsAny<AccountEntity>(), It.IsAny<bool>()))!.Callback((AccountEntity c, bool d) => resultingEntity = c);
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
        resultingEntity.AccountNumber.Should().BeEquivalentTo(expectedEntity.AccountNumber);
        resultingEntity.Name.Should().BeEquivalentTo(expectedEntity.Name);
        resultingEntity.LegalEntityId.Should().Be(expectedEntity.LegalEntityId);
        resultingEntity.Email.Should().BeEquivalentTo(expectedEntity.Email);
        resultingEntity.Attachments.Should().HaveCount(1);
        resultingEntity.Attachments.FirstOrDefault().Should().BeEquivalentTo(expectedEntity.Attachments.FirstOrDefault());
        resultingEntity.Attachments.FirstOrDefault()!.ContainerName.Should().BeEquivalentTo(expectedEntity.Attachments.FirstOrDefault()!.ContainerName);
    }

    [TestMethod("CustomerProcessor should be able to process a normal message for a new entity.")]
    public async Task CustomerProcessor_Process_NormalMessageNew()
    {
        // arrange
        AccountEntity resultingEntity = null!;
        var id = Guid.Parse("ba248238-f375-4e78-b57e-b327227ea24d");
        var legalEntityId = Guid.Parse("104ed370-66da-4c20-b149-823d0d4e7738");
        var expectedEntity = new AccountEntity
        {
            LastIntegrationDateTime = new DateTime(2022, 07, 20, 14, 41, 11),
            AccountNumber = "000001",
            Name = "Alberta Oilsands Inc",
            Email = "",
            LegalEntityId = legalEntityId,
            Id = id,
            Attachments = new()
            {
                new()
                {
                    ContainerName = "attachments",
                    FileName = "test1.pdf",
                    Blob = "be855f88-4dd9-4f92-86ad-f0afdd6bf3a1/",
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

        var message = typeof(CustomerProcessorTests).Assembly.GetResourceAsString("EntityFunctions-Customer-Sample.json", "Resources")!;
        _manager.Setup(m => m.Save(It.IsAny<AccountEntity>(), It.IsAny<bool>()))!.Callback((AccountEntity c, bool d) => resultingEntity = c);
        _manager.Setup(m => m.GetById(It.Is<Guid>(g => g == id), It.IsAny<bool>()))!.Returns(Task.FromResult((AccountEntity)null));
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
        resultingEntity.AccountNumber.Should().BeEquivalentTo(expectedEntity.AccountNumber);
        resultingEntity.Name.Should().BeEquivalentTo(expectedEntity.Name);
        resultingEntity.LegalEntityId.Should().Be(expectedEntity.LegalEntityId);
        resultingEntity.Email.Should().BeEquivalentTo(expectedEntity.Email);
        resultingEntity.Attachments.Should().HaveCount(1);
        resultingEntity.Attachments.FirstOrDefault().Should().BeEquivalentTo(expectedEntity.Attachments.FirstOrDefault());
        resultingEntity.Attachments.FirstOrDefault()!.ContainerName.Should().BeEquivalentTo(expectedEntity.Attachments.FirstOrDefault()!.ContainerName);
    }

    [TestMethod("CustomerProcessor should be able to skip outdated messages.")]
    public async Task CustomerProcessor_Process_SkipOutdated()
    {
        // arrange
        var id = Guid.Parse("ba248238-f375-4e78-b57e-b327227ea24d");
        var legalEntityId = Guid.Parse("104ed370-66da-4c20-b149-823d0d4e7738");
        var existingEntity = new AccountEntity
        {
            LastIntegrationDateTime = new DateTime(2022, 12, 21, 14, 41, 11),
            AccountNumber = "000002",
            Name = "Alberta Oilsands Inc",
            Email = "old-email",
            IsBlocked = false,
            LegalEntityId = legalEntityId,
            Id = id,
        };

        var message = typeof(CustomerProcessorTests).Assembly.GetResourceAsString("EntityFunctions-Customer-Sample.json", "Resources")!;
        _manager.Setup(m => m.GetById(It.IsAny<Guid>(), It.IsAny<bool>()))!.Returns(Task.FromResult(existingEntity));

        // act
        await _processor.Process(message);

        // assert
        _log.Verify(l => l.Warning(It.IsAny<Type>()!, It.IsAny<Exception>()!, It.Is<string>(m => m.Contains("Message is outdated"))!, It.IsAny<object[]>()!));
    }
}
