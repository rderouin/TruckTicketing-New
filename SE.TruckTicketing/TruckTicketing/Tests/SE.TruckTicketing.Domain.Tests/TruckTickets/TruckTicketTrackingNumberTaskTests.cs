using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.Sequences;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Domain.Tests.TruckTickets;

[TestClass]
public class TruckTicketTrackingNumberTaskTests
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
    public async Task Task_ShouldNotRun_When_TareWeight_IsNotSet()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<TruckTicketEntity>();
        entity.TareWeight = default;
        var context = new BusinessContext<TruckTicketEntity>(entity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldRun_When_LoadDate_TareWeight_FacilityType_Are_Set_Using_Expected_Values()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<TruckTicketEntity>();
        entity.TimeIn = DateTimeOffset.Now;
        entity.TruckTicketType = TruckTicketType.LF;
        entity.TrackingNumber = string.Empty;
        var context = new BusinessContext<TruckTicketEntity>(entity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldNotRun_When_TrackingNumber_Is_Set_Using_Expected_Values()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<TruckTicketEntity>();
        entity.TimeIn = DateTimeOffset.Now;
        entity.TruckTicketType = TruckTicketType.LF;
        var context = new BusinessContext<TruckTicketEntity>(entity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldUpdateTrackingNumberToExpectedValue()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<TruckTicketEntity>();
        var timeString = "2023-01-30T02:56:26.404-05:00";
        var targetTime = DateTimeOffset.Parse(timeString);
        entity.TimeIn = targetTime;
        entity.FacilityId = Guid.Parse("2ea69f09-ef03-4f97-b666-c3cfee1b3c55");
        entity.LoadDate = new DateTimeOffset(2023, 01, 30, 10, 10, 10, 10, new TimeSpan(-5, 0, 0));
        var context = new BusinessContext<TruckTicketEntity>(entity);
        context.Operation = Operation.Update;
        var expectedTrackingNumber = "1";
        scope.SetSequenceGeneratorManager();
        scope.SetFacilityProvider();

        // act
        var shouldRun = await scope.InstanceUnderTest.Run(context);

        // assert
        shouldRun.Should().BeTrue();
        context.Target.TrackingNumber.Should().Be(expectedTrackingNumber);
    }

    [TestMethod]
    public async Task Task_ShouldReturnDefaultTrackingDateIfTimeInIsNull()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<TruckTicketEntity>();
        var timeString = "2023-01-30T02:56:26.404-05:00";
        var targetTime = DateTimeOffset.Parse(timeString);
        entity.TimeIn = null;
        entity.FacilityId = Guid.Parse("2ea69f09-ef03-4f97-b666-c3cfee1b3c55");
        entity.LoadDate = new DateTimeOffset(2023, 01, 30, 10, 10, 10, 10, new TimeSpan(-5, 0, 0));
        var context = new BusinessContext<TruckTicketEntity>(entity);
        context.Operation = Operation.Update;
        var expectedTrackingNumber = "202301301";
        scope.SetSequenceGeneratorManager();
        scope.SetFacilityProvider();

        // act
        var shouldRun = await scope.InstanceUnderTest.Run(context);

        // assert
        shouldRun.Should().BeTrue();
        context.Target.TrackingNumber.Should().Be(expectedTrackingNumber);
    }

    public class DefaultScope : TestScope<TruckTicketTrackingNumberTask>
    {
        private Mock<ISequenceNumberGenerator> SequenceNumberGeneratorMock { get; } = new();

        private readonly Mock<IProvider<Guid, FacilityEntity>> _facilityProviderMock = new();

        public DefaultScope()
        {
            InstanceUnderTest = new TruckTicketTrackingNumberTask(SequenceNumberGeneratorMock.Object, _facilityProviderMock.Object);
        }

        public void SetFacilityProvider()
        {
            _facilityProviderMock.Setup(pr => pr.GetById(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                                 .ReturnsAsync(new FacilityEntity() { SiteId = "FACLF" });
        }

        public void SetSequenceGeneratorManager()
        {
            ConfigureSequenceGeneratorManagerMock(SequenceNumberGeneratorMock);
        }

        private void ConfigureSequenceGeneratorManagerMock(Mock<ISequenceNumberGenerator> mock)
        {
            mock.Setup(sequence => sequence.GenerateSequenceNumbers(It.IsAny<string>(),
                                                                    It.IsAny<string>(),
                                                                    It.IsAny<int>(),
                                                                    It.IsAny<string>(),
                                                                    It.IsAny<string>())).Returns(GenerateSequenceNumber());
        }

        private IAsyncEnumerable<string> GenerateSequenceNumber()
        {
            return new List<string>
            {
                "FACLF202301301",
            }.ToAsyncEnumerable();
        }
    }
}
