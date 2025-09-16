using System;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;

using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.SourceLocation;
using SE.Shared.Domain.Entities.SourceLocation.Tasks;
using SE.TruckTicketing.Contracts.Constants.SourceLocations;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Testing.TestScopes;

namespace SE.Shared.Domain.Tests.Tasks;

[TestClass]
public class SourceLocationGeneratorContactIndexerTest
{
    [TestMethod]
    public async Task Task_ShouldNotRun_WhenTargetIsNull()
    {
        // arrange
        var scope = new DefaultScope();
        var context = new BusinessContext<SourceLocationEntity>(null);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldRun_WhenInsert()
    {
        // arrange
        var scope = new DefaultScope();
        var targetEntity = scope.ValidSourceLocationEntity;
        targetEntity.GeneratorId = Guid.NewGuid();
        targetEntity.ContractOperatorId = Guid.NewGuid();
        targetEntity.GeneratorProductionAccountContactId = Guid.NewGuid();
        targetEntity.ContractOperatorProductionAccountContactId = Guid.NewGuid();
        var context = new BusinessContext<SourceLocationEntity>(targetEntity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldRun_WhenGeneratorProductionAccountContactUpdated()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidSourceLocationEntity;
        var targetEntity = originalEntity.Clone();
        targetEntity.GeneratorId = Guid.NewGuid();
        targetEntity.GeneratorProductionAccountContactId = Guid.NewGuid();
        var context = new BusinessContext<SourceLocationEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldRun_WhenContractOperatorAccountContactUpdated()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidSourceLocationEntity;
        var targetEntity = originalEntity.Clone();
        targetEntity.ContractOperatorId = Guid.NewGuid();
        targetEntity.ContractOperatorProductionAccountContactId = Guid.NewGuid();
        var context = new BusinessContext<SourceLocationEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldNotRun_WhenNoContactUpdated()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidSourceLocationEntity;
        var targetEntity = originalEntity.Clone();
        var context = new BusinessContext<SourceLocationEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldNotRun_WhenNoContactSelected()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidSourceLocationEntity;
        originalEntity.ContractOperatorId = Guid.Empty;
        originalEntity.GeneratorId = Guid.Empty;
        originalEntity.GeneratorProductionAccountContactId = Guid.Empty;
        originalEntity.ContractOperatorProductionAccountContactId = Guid.Empty;
        var targetEntity = originalEntity.Clone();
        var context = new BusinessContext<SourceLocationEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldCreateNewIndex_WhenGeneratorProductionAccountContactIsSet()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = scope.ValidSourceLocationEntity.Clone();
        entity.GeneratorId = Guid.NewGuid();
        entity.GeneratorProductionAccountContactId = Guid.NewGuid();
        var context = new BusinessContext<SourceLocationEntity>(entity, scope.ValidSourceLocationEntity);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        scope.IndexProviderMock.Verify(p => p.Insert(It.Is<AccountContactReferenceIndexEntity>(index => IsGeneratorProductionAccountMatch(entity, index)), It.IsAny<bool>()));
    }

    [TestMethod]
    public async Task Task_ShouldCreateNewIndex_WhenContractOperatorProductionAccountContactIsSet()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = scope.ValidSourceLocationEntity.Clone();
        entity.ContractOperatorId = Guid.NewGuid();
        entity.ContractOperatorProductionAccountContactId = Guid.NewGuid();
        var context = new BusinessContext<SourceLocationEntity>(entity, scope.ValidSourceLocationEntity);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        scope.IndexProviderMock.Verify(p => p.Insert(It.Is<AccountContactReferenceIndexEntity>(index => IsContractOperatorAccountMatch(entity, index)), It.IsAny<bool>()));
    }

    private bool IsGeneratorProductionAccountMatch(SourceLocationEntity entity, AccountContactReferenceIndexEntity index)
    {
        return entity.Id == index.ReferenceEntityId &&
               entity.GeneratorId == index.AccountId &&
               entity.GeneratorProductionAccountContactId == index.AccountContactId;
    }

    private bool IsContractOperatorAccountMatch(SourceLocationEntity entity, AccountContactReferenceIndexEntity index)
    {
        return entity.Id == index.ReferenceEntityId &&
               entity.ContractOperatorId == index.AccountId &&
               entity.ContractOperatorProductionAccountContactId == index.AccountContactId;
    }

    public class DefaultScope : TestScope<SourceLocationGeneratorContactIndexer>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(IndexProviderMock.Object);
        }

        public Mock<IProvider<Guid, AccountContactReferenceIndexEntity>> IndexProviderMock { get; } = new();

        public SourceLocationEntity ValidSourceLocationEntity =>
            new()
            {
                SourceLocationName = "Test",
                Identifier = "990/99-90-999-09W9/99",
                GeneratorId = Guid.NewGuid(),
                GeneratorStartDate = DateTimeOffset.Now,
                ContractOperatorId = Guid.NewGuid(),
                SourceLocationType = new(),
                CountryCode = CountryCode.US,
                IsUnique = true,
                DownHoleType = DownHoleType.Pit,
                DeliveryMethod = DeliveryMethod.Trucked,
                ContractOperatorProductionAccountContactId = Guid.NewGuid(),
                GeneratorProductionAccountContactId = Guid.NewGuid(),
            };
    }
}
