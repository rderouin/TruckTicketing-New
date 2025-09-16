using System;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.Tests.TestUtilities;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Domain.Tests.TruckTickets;

[TestClass]
public class TruckTicketEffectiveDateSetterTaskTests
{
    [TestMethod]
    public async Task Task_ShouldRun_WhenTheOriginalIsNull()
    {
        // arrange
        var scope = new DefaultScope();

        var ticket = GenFu.GenFu.New<TruckTicketEntity>();
        ticket.LoadDate = DateTimeOffset.Now;
        ticket.TimeOut = DateTimeOffset.Now;
        var context = new BusinessContext<TruckTicketEntity>(ticket);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldNotRun_WhenTheOriginalLoadDateAndTimeOutAreTheSame()
    {
        // arrange
        var scope = new DefaultScope();

        var original = GenFu.GenFu.New<TruckTicketEntity>();
        original.LoadDate = DateTimeOffset.Now;
        original.TimeOut = DateTimeOffset.Now;
        var target = original.Clone();
        var context = new BusinessContext<TruckTicketEntity>(target, original);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldRun_WhenTheOriginalLoadDateDiffersFromTargetLoadDate()
    {
        // arrange
        var scope = new DefaultScope();

        var original = GenFu.GenFu.New<TruckTicketEntity>();
        original.LoadDate = DateTimeOffset.Now;
        original.TimeOut = DateTimeOffset.Now;
        var target = original.Clone();
        target.LoadDate = original.LoadDate.Value.AddDays(1);
        var context = new BusinessContext<TruckTicketEntity>(target, original);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldRun_WhenTheOriginalTimeOutDiffersFromTargetTimeOut()
    {
        // arrange
        var scope = new DefaultScope();

        var original = GenFu.GenFu.New<TruckTicketEntity>();
        original.LoadDate = DateTimeOffset.Now;
        original.TimeOut = DateTimeOffset.Now;
        var target = original.Clone();
        target.TimeOut = original.TimeOut.Value.AddHours(1);
        var context = new BusinessContext<TruckTicketEntity>(target, original);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldSetEffectiveDate_UsingCurrentDayStrategy_ForWorkTickets()
    {
        // arrange
        var scope = new DefaultScope();

        var ticket = GenFu.GenFu.New<TruckTicketEntity>();
        ticket.LoadDate = new(new(2023, 3, 3));
        ticket.TimeOut = DateTimeOffset.Now;
        ticket.TruckTicketType = TruckTicketType.WT;
        var context = new BusinessContext<TruckTicketEntity>(ticket);

        // act
        await scope.InstanceUnderTest.Run(context);

        // assert
        ticket.EffectiveDate.Should().NotBeNull();
        ticket.EffectiveDate.Should().HaveYear(2023);
        ticket.EffectiveDate.Should().HaveMonth(3);
        ticket.EffectiveDate.Should().HaveDay(3);
    }

    [TestMethod]
    public async Task Task_ShouldSetEffectiveDateToNull_UsingCurrentDayStrategy_ForWorkTicketsWithNoLoadDate()
    {
        // arrange
        var scope = new DefaultScope();

        var ticket = GenFu.GenFu.New<TruckTicketEntity>();
        ticket.LoadDate = null;
        ticket.TimeOut = DateTimeOffset.Now;
        ticket.TruckTicketType = TruckTicketType.WT;
        var context = new BusinessContext<TruckTicketEntity>(ticket);

        // act
        await scope.InstanceUnderTest.Run(context);

        // assert
        ticket.EffectiveDate.Should().BeNull();
    }

    [DataTestMethod]
    [DataRow(TruckTicketType.SP)]
    [DataRow(TruckTicketType.LF)]
    public async Task Task_ShouldSetEffectiveCurrentDate_UsingTMinusOneStrategy_ForNonWorkTickets(TruckTicketType truckTicketType)
    {
        // arrange
        var scope = new DefaultScope();
        var facility = GenFu.GenFu.New<FacilityEntity>();
        facility.OperatingDayCutOffTime = new(new(2023, 1, 1, 7, 0, 0, 0));
        scope.FacilityProviderMock.SetupEntities(new[] { facility });

        var ticket = GenFu.GenFu.New<TruckTicketEntity>();
        ticket.FacilityId = facility.Id;
        ticket.LoadDate = new(new(2023, 3, 3));
        ticket.TimeOut = ticket.LoadDate.Value.AddHours(8);
        ticket.TruckTicketType = truckTicketType;
        var context = new BusinessContext<TruckTicketEntity>(ticket);

        // act
        await scope.InstanceUnderTest.Run(context);

        // assert
        ticket.EffectiveDate.Should().NotBeNull();
        ticket.EffectiveDate.Should().HaveYear(2023);
        ticket.EffectiveDate.Should().HaveMonth(3);
        ticket.EffectiveDate.Should().HaveDay(3);
    }

    [DataTestMethod]
    [DataRow(TruckTicketType.SP)]
    [DataRow(TruckTicketType.LF)]
    public async Task Task_ShouldSetEffectiveCurrentDate_UsingTMinusOneStrategy_ForNonWorkTicketsWithNoTimeOut(TruckTicketType truckTicketType)
    {
        // arrange
        var scope = new DefaultScope();
        var facility = GenFu.GenFu.New<FacilityEntity>();
        facility.OperatingDayCutOffTime = new(new(2023, 1, 1, 7, 0, 0, 0));
        scope.FacilityProviderMock.SetupEntities(new[] { facility });

        var ticket = GenFu.GenFu.New<TruckTicketEntity>();
        ticket.FacilityId = facility.Id;
        ticket.LoadDate = new(new(2023, 3, 3));
        ticket.TimeOut = null;
        ticket.TruckTicketType = truckTicketType;
        var context = new BusinessContext<TruckTicketEntity>(ticket);

        // act
        await scope.InstanceUnderTest.Run(context);

        // assert
        ticket.EffectiveDate.Should().NotBeNull();
        ticket.EffectiveDate.Should().HaveYear(2023);
        ticket.EffectiveDate.Should().HaveMonth(3);
        ticket.EffectiveDate.Should().HaveDay(3);
    }

    [DataTestMethod]
    [DataRow(TruckTicketType.SP)]
    [DataRow(TruckTicketType.LF)]
    public async Task Task_ShouldSetEffectiveTMinusOneDate_UsingTMinusOneStrategy_ForNonWorkTickets(TruckTicketType truckTicketType)
    {
        // arrange
        var scope = new DefaultScope();
        var facility = GenFu.GenFu.New<FacilityEntity>();
        facility.OperatingDayCutOffTime = new(new(2023, 1, 1, 7, 0, 0, 0));
        scope.FacilityProviderMock.SetupEntities(new[] { facility });

        var ticket = GenFu.GenFu.New<TruckTicketEntity>();
        ticket.FacilityId = facility.Id;
        ticket.LoadDate = new(new(2023, 3, 3));
        ticket.TimeOut = ticket.LoadDate.Value.AddHours(6).AddMinutes(59);
        ticket.TruckTicketType = truckTicketType;
        var context = new BusinessContext<TruckTicketEntity>(ticket);

        // act
        await scope.InstanceUnderTest.Run(context);

        // assert
        ticket.EffectiveDate.Should().NotBeNull();
        ticket.EffectiveDate.Should().HaveYear(2023);
        ticket.EffectiveDate.Should().HaveMonth(3);
        ticket.EffectiveDate.Should().HaveDay(2);
    }

    [DataTestMethod]
    [DataRow(TruckTicketType.SP)]
    [DataRow(TruckTicketType.LF)]
    public async Task Task_ShouldSetEffectiveTMinusOneDate_UsingTMinusOneStrategy_ForNonWorkTicketsAt12(TruckTicketType truckTicketType)
    {
        // arrange
        var scope = new DefaultScope();
        var facility = GenFu.GenFu.New<FacilityEntity>();
        facility.OperatingDayCutOffTime = new(new(2023, 1, 1, 0, 0, 0, 0));
        scope.FacilityProviderMock.SetupEntities(new[] { facility });

        var ticket = GenFu.GenFu.New<TruckTicketEntity>();
        ticket.FacilityId = facility.Id;
        ticket.LoadDate = new(new(2023, 3, 3));
        ticket.TimeOut = ticket.LoadDate.Value.AddMinutes(30);
        ticket.TruckTicketType = truckTicketType;
        var context = new BusinessContext<TruckTicketEntity>(ticket);

        // act
        await scope.InstanceUnderTest.Run(context);

        // assert
        ticket.EffectiveDate.Should().NotBeNull();
        ticket.EffectiveDate.Should().HaveYear(2023);
        ticket.EffectiveDate.Should().HaveMonth(3);
        ticket.EffectiveDate.Should().HaveDay(3);
    }

    [DataTestMethod]
    [DataRow(TruckTicketType.SP)]
    [DataRow(TruckTicketType.LF)]
    public async Task Task_ShouldSetEffectiveCurrentDate_UsingTMinusOneStrategy_ForNonWorkTicketsWithNoOperatingDayCutOff(TruckTicketType truckTicketType)
    {
        // arrange
        var scope = new DefaultScope();
        var facility = GenFu.GenFu.New<FacilityEntity>();
        facility.OperatingDayCutOffTime = null;
        scope.FacilityProviderMock.SetupEntities(new[] { facility });

        var ticket = GenFu.GenFu.New<TruckTicketEntity>();
        ticket.FacilityId = facility.Id;
        ticket.LoadDate = new(new(2023, 3, 3));
        ticket.TimeOut = ticket.LoadDate.Value.AddHours(7).AddMinutes(1);
        ticket.TruckTicketType = truckTicketType;
        var context = new BusinessContext<TruckTicketEntity>(ticket);

        // act
        await scope.InstanceUnderTest.Run(context);

        // assert
        ticket.EffectiveDate.Should().NotBeNull();
        ticket.EffectiveDate.Should().HaveYear(2023);
        ticket.EffectiveDate.Should().HaveMonth(3);
        ticket.EffectiveDate.Should().HaveDay(3);
    }

    public class DefaultScope : TestScope<TruckTicketEffectiveDateSetterTask>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(FacilityProviderMock.Object);
        }

        public Mock<IProvider<Guid, FacilityEntity>> FacilityProviderMock { get; } = new();
    }
}
