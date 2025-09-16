using System;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.Tests.TestUtilities;
using SE.TruckTicketing.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Testing.TestScopes;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Tests.TruckTickets;

[TestClass]
public class TruckTicketWellClassificationIndexerTaskTests
{
    [TestMethod]
    public async Task Task_ShouldNotRun_WhenTargetIsNull()
    {
        // arrange
        var scope = new DefaultScope();
        var context = new BusinessContext<TruckTicketEntity>(null);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldNotRun_When_FacilityId_IsNotSet()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<TruckTicketEntity>();
        entity.FacilityId = default;
        var context = new BusinessContext<TruckTicketEntity>(entity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldNotRun_When_SourceLocationId_IsNotSet()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<TruckTicketEntity>();
        entity.SourceLocationId = default;
        var context = new BusinessContext<TruckTicketEntity>(entity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldNotRun_When_WellClassification_IsNotSet()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<TruckTicketEntity>();
        entity.WellClassification = default;
        var context = new BusinessContext<TruckTicketEntity>(entity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldNotRun_When_LoadDate_IsNotSet()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<TruckTicketEntity>();
        entity.LoadDate = default;
        var context = new BusinessContext<TruckTicketEntity>(entity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldRun_WhenFacilitySourceLocationAndWellClassificationAreSet()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<TruckTicketEntity>();
        entity.LoadDate = DateTimeOffset.Now;
        entity.WellClassification = WellClassifications.Completions;
        var context = new BusinessContext<TruckTicketEntity>(entity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeTrue();
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.PostValidation);
    }

    [TestMethod]
    public async Task Task_ShouldCreateNewIndex_WhenFacilitySourceLocationAndWellClassificationAreSet()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<TruckTicketEntity>();
        entity.WellClassification = WellClassifications.Completions;
        entity.LoadDate = DateTimeOffset.UtcNow;
        var context = new BusinessContext<TruckTicketEntity>(entity);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        scope.IndexProviderMock.Verify(p => p.Insert(It.Is<TruckTicketWellClassificationUsageEntity>(index => IsMatch(entity, index)), It.IsAny<bool>()));
    }

    [TestMethod]
    public async Task Task_ShouldUpdateIndex_WhenWellClassificationHasChanged()
    {
        // arrange
        var scope = new DefaultScope();

        var existingIndex = GenFu.GenFu.New<TruckTicketWellClassificationUsageEntity>();
        existingIndex.WellClassification = WellClassifications.Drilling;
        scope.IndexProviderMock.SetupEntities(new[] { existingIndex });

        var entity = GenFu.GenFu.New<TruckTicketEntity>();
        entity.LoadDate = DateTimeOffset.UtcNow;
        entity.SourceLocationId = existingIndex.SourceLocationId;
        entity.FacilityId = existingIndex.FacilityId;
        entity.WellClassification = WellClassifications.Completions;
        entity.FacilityName = existingIndex.FacilityName;
        entity.SourceLocationFormatted = existingIndex.SourceLocationIdentifier;
        entity.Id = existingIndex.TruckTicketId;
        entity.TicketNumber = existingIndex.TicketNumber;

        var context = new BusinessContext<TruckTicketEntity>(entity);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        scope.IndexProviderMock.Verify(p => p.Update(It.Is<TruckTicketWellClassificationUsageEntity>(index => IsMatch(entity, index)), It.IsAny<bool>()));
    }

    [TestMethod]
    public async Task Task_ShouldSkipUpdate_WhenWellClassificationHasNotChanged()
    {
        // arrange
        var scope = new DefaultScope();

        var existingIndex = GenFu.GenFu.New<TruckTicketWellClassificationUsageEntity>();
        existingIndex.WellClassification = WellClassifications.Drilling;
        scope.IndexProviderMock.SetupEntities(new[] { existingIndex });

        var entity = GenFu.GenFu.New<TruckTicketEntity>();
        entity.SourceLocationId = existingIndex.SourceLocationId;
        entity.FacilityId = existingIndex.FacilityId;
        entity.WellClassification = WellClassifications.Drilling;
        entity.LoadDate = DateTimeOffset.Now;

        var context = new BusinessContext<TruckTicketEntity>(entity);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        scope.IndexProviderMock.Verify(p => p.Insert(It.IsAny<TruckTicketWellClassificationUsageEntity>(), It.IsAny<bool>()), Times.Never);
        scope.IndexProviderMock.Verify(p => p.Update(It.IsAny<TruckTicketWellClassificationUsageEntity>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task Task_ShouldSkipUpdate_WhenLoadDateIsOlderThan2Days()
    {
        // arrange
        var scope = new DefaultScope();

        var entity = GenFu.GenFu.New<TruckTicketEntity>();
        entity.LoadDate = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(3));

        var context = new BusinessContext<TruckTicketEntity>(entity);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        scope.IndexProviderMock.Verify(p => p.Insert(It.IsAny<TruckTicketWellClassificationUsageEntity>(), It.IsAny<bool>()), Times.Never);
        scope.IndexProviderMock.Verify(p => p.Update(It.IsAny<TruckTicketWellClassificationUsageEntity>(), It.IsAny<bool>()), Times.Never);
    }

    private bool IsMatch(TruckTicketEntity entity, TruckTicketWellClassificationUsageEntity index)
    {
        return entity.FacilityId == index.FacilityId &&
               entity.FacilityName == index.FacilityName &&
               entity.SourceLocationId == index.SourceLocationId &&
               entity.SourceLocationFormatted == index.SourceLocationIdentifier &&
               entity.TicketNumber == index.TicketNumber &&
               entity.Id == index.TruckTicketId &&
               entity.WellClassification == index.WellClassification;
    }

    public class DefaultScope : TestScope<TruckTicketWellClassificationIndexerTask>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(IndexProviderMock.Object);
        }

        public Mock<IProvider<Guid, TruckTicketWellClassificationUsageEntity>> IndexProviderMock { get; } = new();
    }
}
